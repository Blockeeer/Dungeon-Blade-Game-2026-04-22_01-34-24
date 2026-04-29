using UnityEngine;

namespace DungeonBlade.Enemies
{
    public class SkeletonArcher : EnemyBase
    {
        [Header("Ranged")]
        [SerializeField] Arrow arrowPrefab;
        [SerializeField] Transform shootOrigin;
        [SerializeField] LayerMask projectileHitMask = ~0;
        [SerializeField] float minRange = 5f;
        [SerializeField] float maxRange = 14f;

        enum AttackPhase { None, Windup, Fire, Recovery }
        AttackPhase _phase = AttackPhase.None;
        float _phaseEndTime;
        float _nextAttackTime;
        float _patrolNextMoveTime;

        protected override void Awake()
        {
            base.Awake();
            if (stats.AttackRange <= 0f) stats.AttackRange = maxRange;
            if (stats.KeepDistance <= 0f) stats.KeepDistance = minRange;
            if (shootOrigin == null) shootOrigin = transform;
        }

        protected override void UpdateState()
        {
            switch (State)
            {
                case EnemyState.Idle:    TickIdle(); break;
                case EnemyState.Patrol:  TickPatrol(); break;
                case EnemyState.Chase:   TickChase(); break;
                case EnemyState.Attack:  TickAttack(); break;
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

            float dist = Vector3.Distance(transform.position, Target.position);
            Agent.isStopped = false;
            Agent.speed = stats.MoveSpeed;

            if (dist < minRange)
            {
                Vector3 away = (transform.position - Target.position);
                away.y = 0f;
                if (away.sqrMagnitude < 0.001f) away = transform.right;
                Vector3 retreat = transform.position + away.normalized * 3f;
                if (UnityEngine.AI.NavMesh.SamplePosition(retreat, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                    Agent.SetDestination(hit.position);
            }
            else if (dist > maxRange)
            {
                Agent.SetDestination(Target.position);
            }
            else
            {
                Agent.ResetPath();
            }

            FaceTarget(Target.position);

            if (dist <= maxRange && dist >= minRange * 0.7f && Time.time >= _nextAttackTime)
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
                    if (Time.time >= _phaseEndTime) FireArrow();
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
                _phase = AttackPhase.Windup;
                _phaseEndTime = Time.time + stats.AttackWindup;
            }
        }

        void FireArrow()
        {
            if (arrowPrefab != null && Target != null)
            {
                var arrow = Instantiate(arrowPrefab, shootOrigin.position, Quaternion.identity);

                var arrowCol = arrow.GetComponent<Collider>();
                if (arrowCol != null)
                {
                    foreach (var myCol in GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(arrowCol, myCol, true);
                }

                Vector3 aim = (Target.position + Vector3.up * 1.0f) - shootOrigin.position;
                aim = aim.normalized * stats.ProjectileSpeed;
                arrow.Launch(aim, stats.AttackDamage, stats.ProjectileLifetime, gameObject, projectileHitMask);
            }
            _phase = AttackPhase.Recovery;
            _phaseEndTime = Time.time + stats.AttackRecovery;
        }

        void FinishAttack()
        {
            _phase = AttackPhase.None;
            _nextAttackTime = Time.time + stats.AttackCooldown;
            TransitionTo(EnemyState.Chase);
        }
    }
}
