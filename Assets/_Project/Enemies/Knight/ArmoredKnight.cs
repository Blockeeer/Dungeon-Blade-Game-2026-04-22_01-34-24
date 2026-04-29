using System.Collections.Generic;
using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Enemies
{
    public class ArmoredKnight : EnemyBase
    {
        [Header("Heavy Swing")]
        [SerializeField] float hitRadius = 1.6f;
        [SerializeField] float hitForwardOffset = 1.6f;
        [SerializeField] float knockback = 8f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Telegraph")]
        [SerializeField] Color telegraphColor = new Color(1f, 0.4f, 0f);

        enum AttackPhase { None, Telegraph, Active, Recovery }
        AttackPhase _phase = AttackPhase.None;
        float _phaseEndTime;
        float _nextAttackTime;
        float _patrolNextMoveTime;

        Renderer[] _telegraphRenderers;
        Color[] _telegraphBaseColors;
        bool _telegraphApplied;
        readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        protected override void Awake()
        {
            base.Awake();
            if (stats.AttackRange <= 0f) stats.AttackRange = 2.4f;
            if (stats.DamageReduction <= 0f) stats.DamageReduction = 0.35f;
            if (stats.AttackWindup < 0.6f) stats.AttackWindup = 0.75f;
            if (stats.AttackRecovery < 0.5f) stats.AttackRecovery = 0.7f;

            _telegraphRenderers = GetComponentsInChildren<Renderer>();
            _telegraphBaseColors = new Color[_telegraphRenderers.Length];
            for (int i = 0; i < _telegraphRenderers.Length; i++)
            {
                var mat = _telegraphRenderers[i].material;
                if (mat.HasProperty("_BaseColor")) _telegraphBaseColors[i] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) _telegraphBaseColors[i] = mat.GetColor("_Color");
            }
        }

        protected override void UpdateState()
        {
            switch (State)
            {
                case EnemyState.Idle:   TickIdle(); break;
                case EnemyState.Patrol: TickPatrol(); break;
                case EnemyState.Chase:  TickChase(); break;
                case EnemyState.Attack: TickAttack(); break;
            }
        }

        void TickIdle()
        {
            if (Time.time >= _patrolNextMoveTime) TransitionTo(EnemyState.Patrol);
        }

        void TickPatrol()
        {
            if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.1f)
            {
                _patrolNextMoveTime = Time.time + NextPatrolWait();
                TransitionTo(EnemyState.Idle);
            }
        }

        void TickChase()
        {
            if (Target == null) { TransitionTo(EnemyState.Idle); return; }

            Agent.isStopped = false;
            Agent.speed = stats.ChaseSpeed;
            Agent.SetDestination(Target.position);

            float dist = Vector3.Distance(transform.position, Target.position);
            if (dist <= stats.AttackRange && Time.time >= _nextAttackTime)
            {
                TransitionTo(EnemyState.Attack);
            }
        }

        void TickAttack()
        {
            if (Target == null) { TransitionTo(EnemyState.Idle); return; }

            if (_phase == AttackPhase.Telegraph)
            {
                FaceTarget(Target.position);
                RepaintTelegraph();
            }

            switch (_phase)
            {
                case AttackPhase.Telegraph:
                    if (Time.time >= _phaseEndTime) EnterActive();
                    break;
                case AttackPhase.Active:
                    DoActiveTick();
                    if (Time.time >= _phaseEndTime) EnterRecovery();
                    break;
                case AttackPhase.Recovery:
                    if (Time.time >= _phaseEndTime) FinishAttack();
                    break;
            }
        }

        protected override void OnEnterState(EnemyState s)
        {
            if (s == EnemyState.Patrol)
            {
                if (PickPatrolPoint(out var p))
                {
                    Agent.isStopped = false;
                    Agent.speed = stats.MoveSpeed;
                    Agent.SetDestination(p);
                }
                else TransitionTo(EnemyState.Idle);
            }
            else if (s == EnemyState.Attack)
            {
                Agent.isStopped = true;
                _phase = AttackPhase.Telegraph;
                _phaseEndTime = Time.time + stats.AttackWindup;
                _hitThisSwing.Clear();
                SetTelegraph(true);
            }
        }

        protected override void OnExitState(EnemyState s)
        {
            if (s == EnemyState.Attack) SetTelegraph(false);
        }

        void EnterActive()
        {
            SetTelegraph(false);
            _phase = AttackPhase.Active;
            _phaseEndTime = Time.time + 0.2f;
        }

        void EnterRecovery()
        {
            _phase = AttackPhase.Recovery;
            _phaseEndTime = Time.time + stats.AttackRecovery;
        }

        void FinishAttack()
        {
            _phase = AttackPhase.None;
            _nextAttackTime = Time.time + stats.AttackCooldown;
            TransitionTo(EnemyState.Chase);
        }

        void DoActiveTick()
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f + transform.forward * hitForwardOffset;
            var template = new DamageInfo
            {
                Amount = stats.AttackDamage,
                Knockback = knockback,
                Source = gameObject,
                Type = DamageType.Melee,
                IsParryable = true,
            };
            MeleeHitbox.SphereSweep(origin, hitRadius, hitMask, _hitThisSwing, template);
        }

        void SetTelegraph(bool on)
        {
            _telegraphApplied = on;
            for (int i = 0; i < _telegraphRenderers.Length; i++)
            {
                if (_telegraphRenderers[i] == null) continue;
                var mat = _telegraphRenderers[i].material;
                Color c = on ? telegraphColor : _telegraphBaseColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }

        void RepaintTelegraph()
        {
            if (!_telegraphApplied) return;
            for (int i = 0; i < _telegraphRenderers.Length; i++)
            {
                if (_telegraphRenderers[i] == null) continue;
                var mat = _telegraphRenderers[i].material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", telegraphColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", telegraphColor);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.red;
            Vector3 origin = transform.position + Vector3.up * 1.0f + transform.forward * hitForwardOffset;
            Gizmos.DrawWireSphere(origin, hitRadius);
        }
    }
}
