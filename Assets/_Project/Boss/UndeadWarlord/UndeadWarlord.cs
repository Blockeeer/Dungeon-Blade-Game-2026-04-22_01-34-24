using System.Collections.Generic;
using DungeonBlade.Combat;
using DungeonBlade.Enemies;
using UnityEngine;

namespace DungeonBlade.Boss
{
    public class UndeadWarlord : BossBase
    {
        [Header("Phase 1 / 2 — Cleave")]
        [SerializeField] float cleaveDamage = 30f;
        [SerializeField] float cleaveRange = 3.2f;
        [SerializeField] float cleaveRadius = 1.8f;
        [SerializeField] float cleaveWindup = 0.6f;
        [SerializeField] float cleaveRecovery = 0.6f;
        [SerializeField] float cleaveCooldown = 2.0f;

        [Header("Phase 1 / 2 — Heavy Slam")]
        [SerializeField] float slamDamage = 50f;
        [SerializeField] float slamRadius = 4.5f;
        [SerializeField] float slamWindup = 1.2f;
        [SerializeField] float slamRecovery = 1.0f;
        [SerializeField] float slamCooldown = 8.0f;
        [SerializeField] Color slamTelegraphColor = new Color(1f, 0.3f, 0f);

        [Header("Phase 2 — Bone Spear")]
        [SerializeField] BoneSpear boneSpearPrefab;
        [SerializeField] Transform spearOrigin;
        [SerializeField] float spearDamage = 18f;
        [SerializeField] float spearSpeed = 22f;
        [SerializeField] float spearLifetime = 5f;
        [SerializeField] int spearVolleyCount = 3;
        [SerializeField] float spearVolleyInterval = 0.25f;
        [SerializeField] float spearCooldown = 10f;
        [SerializeField] LayerMask spearHitMask = ~0;

        [Header("Phase 2 — Adds")]
        [SerializeField] BossAddSpawner addSpawner;
        [SerializeField] float addSpawnCooldown = 12f;

        [Header("Phase 3 — Enrage")]
        [SerializeField] float enrageSpeedMul = 1.5f;
        [SerializeField] float enrageDamageMul = 1.3f;
        [SerializeField] float spinDamage = 25f;
        [SerializeField] float spinRadius = 5f;
        [SerializeField] float spinWindup = 0.5f;
        [SerializeField] float spinActive = 1.8f;
        [SerializeField] float spinRecovery = 1.0f;
        [SerializeField] float spinCooldown = 6f;
        [SerializeField] Color enrageTint = new Color(1f, 0.2f, 0.2f);

        [Header("Hit Detection")]
        [SerializeField] LayerMask meleeHitMask = ~0;

        enum AttackType { None, Cleave, Slam, Spear, Spin }
        enum AttackPhase { None, Windup, Active, Recovery }

        AttackType _currentAttack = AttackType.None;
        AttackPhase _attackPhase = AttackPhase.None;
        float _phaseEndTime;

        float _nextCleaveTime;
        float _nextSlamTime;
        float _nextSpearTime;
        float _nextAddTime;
        float _nextSpinTime;

        int _spearsRemaining;
        float _nextSpearSubshotTime;

        Renderer[] _bodyRenderers;
        Color[] _bodyBaseColors;
        readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();
        bool _enraged;

        protected override void Awake()
        {
            base.Awake();
            if (spearOrigin == null) spearOrigin = transform;

            _bodyRenderers = GetComponentsInChildren<Renderer>();
            _bodyBaseColors = new Color[_bodyRenderers.Length];
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                var mat = _bodyRenderers[i].material;
                if (mat.HasProperty("_BaseColor")) _bodyBaseColors[i] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) _bodyBaseColors[i] = mat.GetColor("_Color");
            }
        }

        protected override void UpdateBossPhase()
        {
            if (Target == null)
            {
                var player = FindPlayer();
                if (player != null) ForceAcquireTarget(player);
                return;
            }

            if (_attackPhase != AttackPhase.None)
            {
                TickActiveAttack();
                return;
            }

            float dist = Vector3.Distance(transform.position, Target.position);
            FaceTarget(Target.position);

            bool canSlam = Time.time >= _nextSlamTime;
            bool canCleave = Time.time >= _nextCleaveTime;
            bool canSpear = Phase == BossPhase.Phase2 && Time.time >= _nextSpearTime;
            bool canAdd = Phase == BossPhase.Phase2 && Time.time >= _nextAddTime && addSpawner != null;
            bool canSpin = Phase == BossPhase.Phase3 && Time.time >= _nextSpinTime;

            if (canAdd)
            {
                addSpawner.SpawnWave();
                _nextAddTime = Time.time + addSpawnCooldown;
                return;
            }

            if (canSpin && dist <= spinRadius + 1f)
            {
                StartAttack(AttackType.Spin);
                return;
            }

            if (canSpear && dist > cleaveRange)
            {
                StartAttack(AttackType.Spear);
                return;
            }

            if (dist <= cleaveRange + 0.5f)
            {
                if (canSlam) StartAttack(AttackType.Slam);
                else if (canCleave) StartAttack(AttackType.Cleave);
                else ChaseTick();
                return;
            }

            ChaseTick();
        }

        void ChaseTick()
        {
            Agent.isStopped = false;
            float speedBase = (Phase == BossPhase.Phase1) ? Stats.MoveSpeed : Stats.ChaseSpeed;
            Agent.speed = _enraged ? speedBase * enrageSpeedMul : speedBase;
            Agent.SetDestination(Target.position);
        }

        void StartAttack(AttackType t)
        {
            _currentAttack = t;
            _attackPhase = AttackPhase.Windup;
            _hitThisSwing.Clear();
            Agent.isStopped = true;

            switch (t)
            {
                case AttackType.Cleave:
                    _phaseEndTime = Time.time + cleaveWindup;
                    break;
                case AttackType.Slam:
                    _phaseEndTime = Time.time + slamWindup;
                    TintBody(slamTelegraphColor);
                    break;
                case AttackType.Spear:
                    _phaseEndTime = Time.time + 0.4f;
                    _spearsRemaining = spearVolleyCount;
                    _nextSpearSubshotTime = _phaseEndTime;
                    break;
                case AttackType.Spin:
                    _phaseEndTime = Time.time + spinWindup;
                    TintBody(enrageTint);
                    break;
            }
        }

        void TickActiveAttack()
        {
            if (_currentAttack == AttackType.Cleave || _currentAttack == AttackType.Slam || _currentAttack == AttackType.Spin)
            {
                if (_attackPhase == AttackPhase.Windup && Time.time >= _phaseEndTime) EnterActive();
                else if (_attackPhase == AttackPhase.Active) TickActivePhase();
                else if (_attackPhase == AttackPhase.Recovery && Time.time >= _phaseEndTime) FinishAttack();
            }
            else if (_currentAttack == AttackType.Spear)
            {
                if (_spearsRemaining > 0 && Time.time >= _nextSpearSubshotTime)
                {
                    FireOneSpear();
                    _spearsRemaining--;
                    _nextSpearSubshotTime = Time.time + spearVolleyInterval;
                }
                else if (_spearsRemaining == 0 && Time.time >= _nextSpearSubshotTime + 0.3f)
                {
                    FinishAttack();
                }
            }

            if (_attackPhase == AttackPhase.Windup && Target != null)
            {
                FaceTarget(Target.position);
            }
        }

        void EnterActive()
        {
            _attackPhase = AttackPhase.Active;
            switch (_currentAttack)
            {
                case AttackType.Cleave:
                    _phaseEndTime = Time.time + 0.2f;
                    break;
                case AttackType.Slam:
                    _phaseEndTime = Time.time + 0.2f;
                    DoSlamHit();
                    break;
                case AttackType.Spin:
                    _phaseEndTime = Time.time + spinActive;
                    break;
            }
        }

        void TickActivePhase()
        {
            if (_currentAttack == AttackType.Cleave) DoCleaveTick();
            else if (_currentAttack == AttackType.Spin) DoSpinTick();

            if (Time.time >= _phaseEndTime)
            {
                _attackPhase = AttackPhase.Recovery;
                switch (_currentAttack)
                {
                    case AttackType.Cleave: _phaseEndTime = Time.time + cleaveRecovery; break;
                    case AttackType.Slam:   _phaseEndTime = Time.time + slamRecovery; ClearBodyTint(); break;
                    case AttackType.Spin:   _phaseEndTime = Time.time + spinRecovery; ClearBodyTint(); break;
                }
            }
        }

        void FinishAttack()
        {
            switch (_currentAttack)
            {
                case AttackType.Cleave: _nextCleaveTime = Time.time + cleaveCooldown; break;
                case AttackType.Slam:   _nextSlamTime = Time.time + slamCooldown; break;
                case AttackType.Spear:  _nextSpearTime = Time.time + spearCooldown; break;
                case AttackType.Spin:   _nextSpinTime = Time.time + spinCooldown; break;
            }
            _currentAttack = AttackType.None;
            _attackPhase = AttackPhase.None;
            ClearBodyTint();
        }

        void DoCleaveTick()
        {
            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * cleaveRange * 0.5f;
            float dmg = cleaveDamage * (_enraged ? enrageDamageMul : 1f);
            var template = new DamageInfo
            {
                Amount = dmg,
                Knockback = 4f,
                Source = gameObject,
                Type = DamageType.Melee,
                IsParryable = true,
            };
            MeleeHitbox.SphereSweep(origin, cleaveRadius, meleeHitMask, _hitThisSwing, template);
        }

        void DoSlamHit()
        {
            Vector3 origin = transform.position + Vector3.up * 0.3f;
            float dmg = slamDamage * (_enraged ? enrageDamageMul : 1f);
            var template = new DamageInfo
            {
                Amount = dmg,
                Knockback = 10f,
                Source = gameObject,
                Type = DamageType.Melee,
                IsParryable = false,
            };
            MeleeHitbox.SphereSweep(origin, slamRadius, meleeHitMask, _hitThisSwing, template);
        }

        void DoSpinTick()
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            float dmg = spinDamage * Time.deltaTime * 3f;
            var template = new DamageInfo
            {
                Amount = dmg,
                Knockback = 2f,
                Source = gameObject,
                Type = DamageType.Melee,
                IsParryable = false,
            };
            _hitThisSwing.Clear();
            MeleeHitbox.SphereSweep(origin, spinRadius, meleeHitMask, _hitThisSwing, template);
            transform.Rotate(Vector3.up, 720f * Time.deltaTime, Space.World);
        }

        void FireOneSpear()
        {
            if (boneSpearPrefab == null || Target == null) return;
            var spear = Instantiate(boneSpearPrefab, spearOrigin.position, Quaternion.identity);

            var spearCol = spear.GetComponent<Collider>();
            if (spearCol != null)
                foreach (var myCol in GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(spearCol, myCol, true);

            Vector3 aim = (Target.position + Vector3.up * 1.0f) - spearOrigin.position;
            aim = aim.normalized * spearSpeed;
            spear.Launch(aim, spearDamage, spearLifetime, gameObject, spearHitMask);
        }

        protected override void OnEnterPhase(BossPhase p)
        {
            if (p == BossPhase.Phase2)
            {
                _nextAddTime = Time.time + 3f;
                _nextSpearTime = Time.time + 2f;
                Debug.Log($"[Boss] {name} entered Phase 2 — adds + bone spears.");
            }
            else if (p == BossPhase.Phase3)
            {
                _enraged = true;
                TintBody(enrageTint);
                _nextSpinTime = Time.time + 2f;
                Debug.Log($"[Boss] {name} ENRAGED — Phase 3 active.");
            }
        }

        protected override void OnEnterTransition(BossPhase nextPhase)
        {
            Agent.isStopped = true;
            _currentAttack = AttackType.None;
            _attackPhase = AttackPhase.None;
            _hitThisSwing.Clear();
            TintBody(new Color(0.6f, 0.2f, 1f));
        }

        void TintBody(Color c)
        {
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                if (_bodyRenderers[i] == null) continue;
                var mat = _bodyRenderers[i].material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }

        void ClearBodyTint()
        {
            if (_enraged) { TintBody(enrageTint); return; }
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                if (_bodyRenderers[i] == null) continue;
                var mat = _bodyRenderers[i].material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", _bodyBaseColors[i]);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", _bodyBaseColors[i]);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.red;
            Vector3 cleaveO = transform.position + Vector3.up * 1.2f + transform.forward * cleaveRange * 0.5f;
            Gizmos.DrawWireSphere(cleaveO, cleaveRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.3f, slamRadius);
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, spinRadius);
        }
    }
}
