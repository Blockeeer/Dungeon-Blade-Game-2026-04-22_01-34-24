using UnityEngine;
using UnityEngine.Events;
using DungeonBlade.Combat;

namespace DungeonBlade.Player
{
    /// <summary>
    /// Implements the 6 Phase-1 skills from GDD Section 5.
    /// Each skill has an index (0..5), a cooldown, and an Activate() method.
    /// Bind inputs in Inspector (defaults: E=BladeDash, G=ShotgunBurst, V=Smoke,
    /// F-1 =Counter auto, F-2 =BattleRoll, F-3 =IronSkin). Adapt to your input layout.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerCombat), typeof(PlayerStats))]
    public class PlayerSkills : MonoBehaviour
    {
        public enum SkillId { BladeDash, ShotgunBurst, SmokeGrenade, CounterStrike, BattleRoll, IronSkin }

        [System.Serializable]
        public class SkillEntry
        {
            public SkillId id;
            public KeyCode key;
            public float cooldown;
            [HideInInspector] public float readyAt;
        }

        public SkillEntry[] skills = new SkillEntry[]
        {
            new SkillEntry { id = SkillId.BladeDash,     key = KeyCode.E, cooldown = 6f  },
            new SkillEntry { id = SkillId.ShotgunBurst,  key = KeyCode.G, cooldown = 8f  },
            new SkillEntry { id = SkillId.SmokeGrenade,  key = KeyCode.V, cooldown = 15f },
            new SkillEntry { id = SkillId.BattleRoll,    key = KeyCode.C, cooldown = 4f  },
            new SkillEntry { id = SkillId.IronSkin,      key = KeyCode.F, cooldown = 20f },
            // CounterStrike is auto-triggered on successful parry (no hotkey, no cooldown)
            new SkillEntry { id = SkillId.CounterStrike, key = KeyCode.None, cooldown = 0f },
        };

        [Header("Blade Dash")]
        public float bladeDashDistance = 6f;
        public float bladeDashDamage = 40f;
        public float bladeDashRadius = 1.6f;

        [Header("Shotgun Burst")]
        public float shotgunBurstDamage = 200f;
        public float shotgunBurstRange = 10f;
        public float shotgunBurstCone = 45f;

        [Header("Smoke Grenade")]
        public GameObject smokeGrenadePrefab;
        public float smokeDuration = 3f;
        public float smokeThrowSpeed = 15f;

        [Header("Battle Roll")]
        public float rollDistance = 5f;
        public float rollIFrames = 0.4f;

        [Header("Iron Skin")]
        public float ironSkinDuration = 2f;
        public float ironSkinDamageReduction = 0.5f;

        [Header("Counter Strike")]
        public float counterStrikeDamagePerHit = 28f;
        public int counterStrikeHits = 2;
        public float counterStrikeDelay = 0.08f;

        [Header("Events")]
        public UnityEvent<SkillId> OnSkillUsed;
        public UnityEvent<SkillId, float> OnSkillCooldown;   // (id, remaining)

        PlayerController pc;
        PlayerCombat combat;
        PlayerStats stats;
        float ironSkinEndsAt;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            stats = GetComponent<PlayerStats>();
            combat.OnParrySuccess.AddListener(OnParry);
        }

        void Update()
        {
            foreach (var s in skills)
            {
                if (s.key == KeyCode.None) continue;
                if (Input.GetKeyDown(s.key)) TryActivate(s);
            }
        }

        public bool TryActivate(SkillEntry s)
        {
            if (Time.time < s.readyAt) return false;
            if (!RunSkill(s.id)) return false;
            s.readyAt = Time.time + s.cooldown;
            OnSkillUsed?.Invoke(s.id);
            return true;
        }

        bool RunSkill(SkillId id)
        {
            switch (id)
            {
                case SkillId.BladeDash:     return ExecuteBladeDash();
                case SkillId.ShotgunBurst:  return ExecuteShotgunBurst();
                case SkillId.SmokeGrenade:  return ExecuteSmokeGrenade();
                case SkillId.BattleRoll:    return ExecuteBattleRoll();
                case SkillId.IronSkin:      return ExecuteIronSkin();
                case SkillId.CounterStrike: return ExecuteCounterStrike();
            }
            return false;
        }

        bool ExecuteBladeDash()
        {
            if (!stats.TrySpendStamina(15f)) return false;
            Vector3 dir = transform.forward;
            Vector3 start = transform.position + Vector3.up * 1.0f;
            pc.ApplyKnockback(dir * bladeDashDistance * 2f);

            Collider[] hits = Physics.OverlapCapsule(start, start + dir * bladeDashDistance, bladeDashRadius);
            foreach (var c in hits)
            {
                if (c.transform.root == transform) continue;
                var t = c.GetComponentInParent<IDamageable>();
                if (t == null || t.IsDead) continue;
                t.TakeDamage(new DamageInfo {
                    amount = bladeDashDamage,
                    source = DamageSource.PlayerMelee,
                    knockback = dir * 3f + Vector3.up * 2f,
                    hitPoint = c.ClosestPoint(start),
                    attacker = gameObject,
                });
            }
            return true;
        }

        bool ExecuteShotgunBurst()
        {
            if (combat.CurrentWeapon != PlayerCombat.WeaponMode.Gun) return false;
            if (combat.CurrentMagAmmo <= 0) return false;
            Vector3 origin = transform.position + Vector3.up * 1.3f;
            Vector3 fwd = transform.forward;
            Collider[] hits = Physics.OverlapSphere(origin, shotgunBurstRange);
            int enemies = 0;
            foreach (var c in hits)
            {
                if (c.transform.root == transform) continue;
                Vector3 to = (c.transform.position - origin); to.y = 0;
                if (Vector3.Angle(fwd, to) > shotgunBurstCone) continue;
                var t = c.GetComponentInParent<IDamageable>();
                if (t == null || t.IsDead) continue;
                t.TakeDamage(new DamageInfo {
                    amount = shotgunBurstDamage,
                    source = DamageSource.PlayerRanged,
                    hitPoint = c.ClosestPoint(origin),
                    attacker = gameObject,
                    knockback = to.normalized * 5f,
                });
                enemies++;
            }
            // Empty the mag
            combat.SendMessage("FireGunOnce", SendMessageOptions.DontRequireReceiver); // fx hook
            return enemies > 0 || true;
        }

        bool ExecuteSmokeGrenade()
        {
            if (smokeGrenadePrefab == null) return false;
            Transform eye = combat.cameraTransform != null ? combat.cameraTransform : transform;
            GameObject smoke = Instantiate(smokeGrenadePrefab, eye.position + eye.forward * 0.5f, Quaternion.identity);
            if (smoke.TryGetComponent<Rigidbody>(out var rb))
                rb.velocity = eye.forward * smokeThrowSpeed + Vector3.up * 3f;
            Destroy(smoke, smokeDuration + 0.5f);
            return true;
        }

        bool ExecuteBattleRoll()
        {
            if (!stats.TrySpendStamina(10f)) return false;
            Vector3 dir = transform.forward;
            Vector2 inp = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (inp.sqrMagnitude > 0.01f)
                dir = (transform.forward * inp.y + transform.right * inp.x).normalized;
            pc.ApplyKnockback(dir * rollDistance * 2.5f + Vector3.up * 1.5f);
            pc.GrantIFrames(rollIFrames);
            return true;
        }

        bool ExecuteIronSkin()
        {
            ironSkinEndsAt = Time.time + ironSkinDuration;
            stats.percentArmor = Mathf.Max(stats.percentArmor, ironSkinDamageReduction);
            CancelInvoke(nameof(EndIronSkin));
            Invoke(nameof(EndIronSkin), ironSkinDuration);
            return true;
        }

        void EndIronSkin()
        {
            stats.percentArmor = 0f;
        }

        bool ExecuteCounterStrike()
        {
            // Chained via parry — see OnParry
            StartCoroutine(CounterRoutine());
            return true;
        }

        System.Collections.IEnumerator CounterRoutine()
        {
            for (int i = 0; i < counterStrikeHits; i++)
            {
                // Direct damage in cone
                Vector3 origin = transform.position + Vector3.up * 1.0f;
                Collider[] hits = Physics.OverlapSphere(origin, 2.5f);
                foreach (var c in hits)
                {
                    if (c.transform.root == transform) continue;
                    Vector3 to = c.transform.position - origin; to.y = 0f;
                    if (Vector3.Angle(transform.forward, to) > 60f) continue;
                    var t = c.GetComponentInParent<IDamageable>();
                    if (t == null || t.IsDead) continue;
                    t.TakeDamage(new DamageInfo {
                        amount = counterStrikeDamagePerHit,
                        source = DamageSource.PlayerMelee,
                        hitPoint = c.ClosestPoint(origin),
                        attacker = gameObject,
                        knockback = transform.forward * 4f + Vector3.up * 2f,
                    });
                }
                yield return new WaitForSeconds(counterStrikeDelay);
            }
        }

        void OnParry()
        {
            // Auto-trigger counter if skill is known and ready
            foreach (var s in skills)
            {
                if (s.id == SkillId.CounterStrike && Time.time >= s.readyAt)
                {
                    TryActivate(s);
                    break;
                }
            }
        }

        public float GetCooldownRemaining(SkillId id)
        {
            foreach (var s in skills)
                if (s.id == id) return Mathf.Max(0f, s.readyAt - Time.time);
            return 0f;
        }
    }
}
