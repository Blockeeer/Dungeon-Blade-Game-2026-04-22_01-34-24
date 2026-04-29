using System.Collections.Generic;
using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Enemies
{
    public class SkeletonSoldier : EnemyBase
    {
        [Header("Melee")]
        [SerializeField] float hitRadius = 1.1f;
        [SerializeField] float hitForwardOffset = 1.2f;
        [SerializeField] LayerMask hitMask = ~0;

        enum AttackPhase { None, Windup, Active, Recovery }
        AttackPhase _phase = AttackPhase.None;
        float _phaseEndTime;
        float _nextAttackTime;
        float _patrolNextMoveTime;
        readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        protected override void Awake()
        {
            base.Awake();
            if (stats.AttackRange <= 0f) stats.AttackRange = 1.8f;
        }

        protected override void UpdateState()
        {
            switch (State)
            {
                case EnemyState.Idle:
                    TickIdle();
                    break;
                case EnemyState.Patrol:
                    TickPatrol();
                    break;
                case EnemyState.Chase:
                    TickChase();
                    break;
                case EnemyState.Attack:
                    TickAttack();
                    break;
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

            FaceTarget(Target.position);

            switch (_phase)
            {
                case AttackPhase.Windup:
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
                else
                {
                    TransitionTo(EnemyState.Idle);
                }
            }
            else if (s == EnemyState.Attack)
            {
                Agent.isStopped = true;
                _phase = AttackPhase.Windup;
                _phaseEndTime = Time.time + stats.AttackWindup;
                _hitThisSwing.Clear();
            }
        }

        void EnterActive()
        {
            _phase = AttackPhase.Active;
            _phaseEndTime = Time.time + 0.15f;
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
                Knockback = 3f,
                Source = gameObject,
                Type = DamageType.Melee,
                IsParryable = true,
            };
            MeleeHitbox.SphereSweep(origin, hitRadius, hitMask, _hitThisSwing, template);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.magenta;
            Vector3 origin = transform.position + Vector3.up * 1.0f + transform.forward * hitForwardOffset;
            Gizmos.DrawWireSphere(origin, hitRadius);
        }
    }
}
