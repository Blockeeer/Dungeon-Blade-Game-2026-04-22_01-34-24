using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DungeonBlade.Combat;

namespace DungeonBlade.Enemies
{
    /// <summary>
    /// Boss: The Undead Warlord (GDD 4.2).
    /// - HP 400 across 3 phases
    /// - Phase 1: sword combo (3 hits), stomp AoE
    /// - Phase 2: + summon 2 Skeleton Soldiers, bone-throw (15 dmg), faster combos
    /// - Phase 3: enrage (glow red, +30% attack speed, ground shockwaves)
    /// - Cinematic death then spawn reward chest
    /// </summary>
    public class UndeadWarlord : EnemyBase
    {
        public enum Phase { One, Two, Three, Dying }

        [Header("Phase Thresholds")]
        [Range(0, 1)] public float phase2Threshold = 0.66f;
        [Range(0, 1)] public float phase3Threshold = 0.33f;

        [Header("Sword Combo")]
        public float comboDamage = 30f;
        public float comboRange = 3.5f;
        public float comboConeHalf = 60f;
        public float combo1Windup = 0.55f;
        public float comboGap = 0.35f;
        public float comboCooldown = 3.5f;

        [Header("Stomp AoE")]
        public float stompDamage = 20f;
        public float stompRadius = 5f;
        public float stompWindup = 0.9f;
        public float stompCooldown = 7f;
        public GameObject stompVfxPrefab;

        [Header("Phase 2: Summons")]
        public GameObject skeletonSoldierPrefab;
        public int summonCount = 2;
        public float summonCooldown = 18f;
        public Vector3[] summonOffsets = { new Vector3(4, 0, 4), new Vector3(-4, 0, 4) };

        [Header("Phase 2: Bone Throw")]
        public float boneThrowDamage = 15f;
        public float boneThrowSpeed = 20f;
        public float boneThrowCooldown = 5f;
        public GameObject boneProjectilePrefab;
        public Transform projectileSpawnPoint;

        [Header("Phase 3: Enrage")]
        public float enrageAttackSpeedMult = 1.3f;
        public Material enrageMaterial;
        public Renderer[] renderersToTint;
        public float shockwaveDamage = 25f;
        public float shockwaveRadius = 7f;
        public float shockwaveCooldown = 6f;
        public GameObject shockwaveVfxPrefab;

        [Header("Death")]
        public float deathCinematicSeconds = 2.5f;
        public GameObject rewardChestPrefab;
        public Transform chestSpawnPoint;
        public UnityEvent OnBossDefeated;
        public UnityEvent<Phase> OnPhaseChanged;

        public Phase CurrentPhase { get; private set; } = Phase.One;

        float lastComboTime = -99f;
        float lastStompTime = -99f;
        float lastSummonTime = -99f;
        float lastBoneTime = -99f;
        float lastShockTime = -99f;

        protected override void Awake()
        {
            maxHealth = 400f;
            xpReward = 300;
            goldMin = 150; goldMax = 300;
            attackRange = 4.0f;
            preferredRange = 3.0f;
            moveSpeed = 3.2f;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (state != State.Dead) UpdatePhase();
        }

        void UpdatePhase()
        {
            float frac = currentHealth / maxHealth;
            Phase target = CurrentPhase;
            if (frac <= phase3Threshold) target = Phase.Three;
            else if (frac <= phase2Threshold) target = Phase.Two;
            else target = Phase.One;

            if (target != CurrentPhase)
            {
                CurrentPhase = target;
                if (animator != null) animator.SetTrigger("PhaseTransition");
                OnPhaseChanged?.Invoke(target);
                if (target == Phase.Three) EnterEnrage();
            }
        }

        protected override void AttackTick()
        {
            if (player == null) { TransitionTo(State.Patrol); return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (d > attackRange * 2.0f) { TransitionTo(State.Chase); return; }

            FacePlayer();
            float speedMult = (CurrentPhase == Phase.Three) ? enrageAttackSpeedMult : 1f;

            // Phase 3: shockwave is top priority
            if (CurrentPhase == Phase.Three && Time.time - lastShockTime >= shockwaveCooldown / speedMult)
            {
                lastShockTime = Time.time;
                StartCoroutine(Shockwave());
                return;
            }

            // Phase 2+: summons when ready
            if (CurrentPhase >= Phase.Two && Time.time - lastSummonTime >= summonCooldown && skeletonSoldierPrefab != null)
            {
                lastSummonTime = Time.time;
                SummonSkeletons();
                return;
            }

            // Phase 2+: bone throw when out of melee
            if (CurrentPhase >= Phase.Two && d > comboRange && Time.time - lastBoneTime >= boneThrowCooldown / speedMult)
            {
                lastBoneTime = Time.time;
                StartCoroutine(ThrowBone());
                return;
            }

            // Stomp AoE if player hugging boss
            if (d < stompRadius * 0.8f && Time.time - lastStompTime >= stompCooldown / speedMult)
            {
                lastStompTime = Time.time;
                StartCoroutine(StompAoe());
                return;
            }

            // 3-hit combo
            if (d < comboRange && Time.time - lastComboTime >= comboCooldown / speedMult)
            {
                lastComboTime = Time.time;
                StartCoroutine(SwordCombo(speedMult));
                return;
            }

            // Otherwise just close
            if (d > attackRange && agent.isOnNavMesh) agent.SetDestination(player.position);
        }

        IEnumerator SwordCombo(float speedMult)
        {
            for (int i = 0; i < 3; i++)
            {
                if (state == State.Dead || player == null) yield break;
                if (animator != null) animator.SetInteger("ComboStep", i + 1);
                if (animator != null) animator.SetTrigger("Combo");
                yield return new WaitForSeconds(combo1Windup / speedMult);
                DoConeHit(comboDamage, comboRange, comboConeHalf, isHeavy: i == 2);
                yield return new WaitForSeconds(comboGap / speedMult);
            }
        }

        IEnumerator StompAoe()
        {
            if (animator != null) animator.SetTrigger("Stomp");
            yield return new WaitForSeconds(stompWindup);
            if (state == State.Dead) yield break;
            if (stompVfxPrefab != null) Instantiate(stompVfxPrefab, transform.position, Quaternion.identity);
            DoRadiusHit(stompDamage, stompRadius, knockbackUp:5f);
        }

        IEnumerator Shockwave()
        {
            if (animator != null) animator.SetTrigger("Shockwave");
            yield return new WaitForSeconds(0.7f);
            if (state == State.Dead) yield break;
            if (shockwaveVfxPrefab != null) Instantiate(shockwaveVfxPrefab, transform.position, Quaternion.identity);
            DoRadiusHit(shockwaveDamage, shockwaveRadius, knockbackUp:6f, source: DamageSource.EnemyMelee);
        }

        void SummonSkeletons()
        {
            if (animator != null) animator.SetTrigger("Summon");
            foreach (var offset in summonOffsets)
            {
                Vector3 pos = transform.position + offset;
                Instantiate(skeletonSoldierPrefab, pos, Quaternion.identity);
            }
        }

        IEnumerator ThrowBone()
        {
            if (animator != null) animator.SetTrigger("BoneThrow");
            yield return new WaitForSeconds(0.4f);
            if (state == State.Dead || player == null) yield break;
            Vector3 spawn = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position + Vector3.up * 1.6f + transform.forward;
            Vector3 target = player.position + Vector3.up * 1.0f;
            Vector3 dir = (target - spawn).normalized;

            if (boneProjectilePrefab != null)
            {
                var bone = Instantiate(boneProjectilePrefab, spawn, Quaternion.LookRotation(dir));
                var proj = bone.GetComponent<Projectile>() ?? bone.AddComponent<Projectile>();
                proj.Launch(dir, boneThrowSpeed, boneThrowDamage, gameObject, DamageSource.EnemyRanged, dir * 4f);
            }
        }

        void EnterEnrage()
        {
            if (enrageMaterial != null && renderersToTint != null)
            {
                foreach (var r in renderersToTint) if (r != null) r.material = enrageMaterial;
            }
            if (animator != null) animator.SetBool("Enraged", true);
        }

        void DoConeHit(float damage, float range, float coneHalf, bool isHeavy = false)
        {
            if (player == null) return;
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 toPlayer = player.position - origin;
            if (toPlayer.magnitude > range) return;
            if (Vector3.Angle(transform.forward, toPlayer) > coneHalf) return;

            var dmg = player.GetComponentInParent<IDamageable>();
            if (dmg == null) return;
            var combat = player.GetComponent<DungeonBlade.Player.PlayerCombat>();
            if (combat != null && combat.TryParry(dmg, gameObject)) return;

            dmg.TakeDamage(new DamageInfo {
                amount = damage,
                source = DamageSource.EnemyMelee,
                knockback = transform.forward * 5f + Vector3.up * 2f,
                hitPoint = player.position,
                attacker = gameObject,
                isHeavy = isHeavy,
            });
        }

        void DoRadiusHit(float damage, float radius, float knockbackUp = 3f, DamageSource source = DamageSource.EnemyMelee)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var c in hits)
            {
                if (c.transform.root == transform) continue;
                var dmg = c.GetComponentInParent<IDamageable>();
                if (dmg == null || dmg.IsDead) continue;
                Vector3 away = (c.transform.position - transform.position); away.y = 0;
                dmg.TakeDamage(new DamageInfo {
                    amount = damage,
                    source = source,
                    knockback = away.normalized * 4f + Vector3.up * knockbackUp,
                    hitPoint = c.ClosestPoint(transform.position),
                    attacker = gameObject,
                    isHeavy = true,
                });
            }
        }

        protected override void Die(DamageInfo finalBlow)
        {
            // Skip base.Die because boss has cinematic + chest
            TransitionTo(State.Dead);
            if (agent != null) agent.enabled = false;
            if (animator != null) animator.SetTrigger("Death");
            StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            // Pause input optional — keep player moving but boss dying
            yield return new WaitForSeconds(deathCinematicSeconds);
            DungeonBlade.Core.GameServices.Progression?.AddExperience(xpReward);
            int gold = Random.Range(goldMin, goldMax + 1);
            DungeonBlade.Core.GameServices.Inventory?.AddGold(gold);

            if (rewardChestPrefab != null)
            {
                Vector3 pos = chestSpawnPoint != null ? chestSpawnPoint.position : transform.position;
                Instantiate(rewardChestPrefab, pos, Quaternion.identity);
            }

            OnBossDefeated?.Invoke();
            OnDied?.Invoke();

            // Boss body lingers a bit then fades
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Destroy(gameObject, 10f);
        }
    }
}
