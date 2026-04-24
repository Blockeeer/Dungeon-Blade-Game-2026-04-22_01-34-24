using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using DungeonBlade.Combat;

namespace DungeonBlade.Enemies
{
    /// <summary>
    /// Base state machine for all enemy types.
    /// States: Idle/Patrol, Chase, Attack, Stagger, Dead.
    /// Specific enemies override AttackTick() to implement their behavior.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        public enum State { Patrol, Chase, Attack, Stagger, Dead }

        [Header("Stats")]
        public float maxHealth = 30f;
        public float currentHealth;
        public int xpReward = 15;
        public int goldMin = 1;
        public int goldMax = 5;

        [Header("Detection / Movement")]
        public float sightRange = 15f;
        public float loseSightRange = 22f;
        public float attackRange = 2f;
        public float preferredRange = 2f;        // ranged enemies override
        public float turnSpeed = 360f;
        public float moveSpeed = 3.5f;
        public LayerMask sightBlockers = ~0;
        public Transform eyePoint;

        [Header("Patrol")]
        public float patrolRadius = 6f;
        public float patrolWaitTime = 2f;

        [Header("Refs")]
        public Animator animator;
        public GameObject deathVfxPrefab;

        [Header("Loot Drop")]
        public DungeonBlade.Loot.LootTable lootTable;

        [Header("Events")]
        public UnityEvent OnAggro;
        public UnityEvent<DamageInfo> OnHit;
        public UnityEvent OnDied;

        // Runtime
        protected State state = State.Patrol;
        protected NavMeshAgent agent;
        protected Transform player;
        protected Vector3 homePosition;
        protected Vector3 patrolTarget;
        protected float patrolWaitTimer;
        protected float staggerEnd;
        protected float lastAttackTime = -99f;

        public bool IsDead => state == State.Dead;
        public State CurrentState => state;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.speed = moveSpeed;
            currentHealth = maxHealth;
            homePosition = transform.position;
            if (eyePoint == null) eyePoint = transform;
        }

        protected virtual void Start()
        {
            // Find player if not assigned
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
            }
            else
            {
                // Fallback: try finding by component (in case Tag is not set)
                var pc = FindObjectOfType<DungeonBlade.Player.PlayerController>();
                if (pc != null)
                {
                    player = pc.transform;
                    Debug.LogWarning($"[{GetType().Name}] Player tag missing — found player via PlayerController component. Set Player GameObject's Tag to 'Player' in the Inspector for better perf.");
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] Cannot find Player! No GameObject with tag 'Player' and no PlayerController in scene. Enemy will idle.");
                }
            }

            // Warn if NavMesh isn't baked — enemy can still aggro but won't chase smoothly
            if (agent != null && !agent.isOnNavMesh)
            {
                Debug.LogWarning($"[{GetType().Name}] NavMeshAgent is not on a NavMesh! Enemy won't chase. Bake NavMesh via Window → AI → Navigation or add a NavMeshSurface component.");
            }

            PickNewPatrolTarget();
        }

        protected virtual void Update()
        {
            if (state == State.Dead) return;

            if (state == State.Stagger)
            {
                if (Time.time >= staggerEnd) TransitionTo(State.Chase);
                return;
            }

            switch (state)
            {
                case State.Patrol: PatrolTick(); break;
                case State.Chase:  ChaseTick();  break;
                case State.Attack: AttackTick(); break;
            }
        }

        protected virtual void PatrolTick()
        {
            if (CanSeePlayer()) { TransitionTo(State.Chase); OnAggro?.Invoke(); return; }
            if (agent.isOnNavMesh)
            {
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    patrolWaitTimer -= Time.deltaTime;
                    if (patrolWaitTimer <= 0f)
                    {
                        PickNewPatrolTarget();
                        patrolWaitTimer = patrolWaitTime;
                    }
                }
            }
            if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        protected virtual void ChaseTick()
        {
            if (player == null) { TransitionTo(State.Patrol); return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (d > loseSightRange && !CanSeePlayer())
            {
                TransitionTo(State.Patrol);
                return;
            }
            if (d <= attackRange && HasLineOfSightToPlayer())
            {
                TransitionTo(State.Attack);
                return;
            }
            // Move toward player but maintain preferred range for ranged
            Vector3 toPlayer = player.position - transform.position;
            Vector3 target = player.position - toPlayer.normalized * preferredRange;
            if (agent.isOnNavMesh) agent.SetDestination(target);
            FacePlayer();
            if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        protected abstract void AttackTick();

        protected void FacePlayer()
        {
            if (player == null) return;
            Vector3 dir = player.position - transform.position; dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        protected bool CanSeePlayer()
        {
            if (player == null) return false;
            Vector3 dir = player.position - eyePoint.position;
            if (dir.magnitude > sightRange) return false;
            return HasLineOfSightToPlayer();
        }

        protected bool HasLineOfSightToPlayer()
        {
            if (player == null) return false;
            Vector3 from = eyePoint.position;
            Vector3 to = player.position + Vector3.up * 1.2f;
            if (Physics.Linecast(from, to, out RaycastHit hit, sightBlockers, QueryTriggerInteraction.Ignore))
            {
                return hit.transform.root == player.root;
            }
            return true;
        }

        protected void PickNewPatrolTarget()
        {
            Vector3 random = Random.insideUnitSphere * patrolRadius;
            random += homePosition; random.y = transform.position.y;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolTarget = hit.position;
                if (agent.isOnNavMesh) agent.SetDestination(patrolTarget);
            }
        }

        protected void TransitionTo(State s)
        {
            if (state == s) return;
            state = s;
            if (animator != null) animator.SetInteger("State", (int)s);
            if (s == State.Attack && agent.isOnNavMesh) agent.ResetPath();
        }

        public virtual void TakeDamage(DamageInfo info)
        {
            if (state == State.Dead) return;
            currentHealth -= info.amount;
            OnHit?.Invoke(info);

            // Aggro on hit even if out of sight
            if (state == State.Patrol) { TransitionTo(State.Chase); OnAggro?.Invoke(); }

            if (info.knockback.sqrMagnitude > 0.01f)
            {
                // Simple knockback: offset position & brief stagger
                if (agent.isOnNavMesh) agent.Move(info.knockback * 0.1f);
                Stagger(0.25f);
            }
            if (info.isHeavy) Stagger(0.5f);

            if (currentHealth <= 0f) Die(info);
        }

        public virtual void Stagger(float duration)
        {
            if (state == State.Dead) return;
            TransitionTo(State.Stagger);
            staggerEnd = Time.time + duration;
            if (animator != null) animator.SetTrigger("Stagger");
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        protected virtual void Die(DamageInfo finalBlow)
        {
            TransitionTo(State.Dead);
            if (agent != null) agent.enabled = false;
            if (animator != null) animator.SetTrigger("Death");
            if (deathVfxPrefab != null) Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            OnDied?.Invoke();

            // Award XP + loot
            var progression = DungeonBlade.Core.GameServices.Progression;
            progression?.AddExperience(xpReward);

            int gold = Random.Range(goldMin, goldMax + 1);
            DungeonBlade.Core.GameServices.Inventory?.AddGold(gold);

            if (lootTable != null)
            {
                var drops = lootTable.Roll();
                foreach (var d in drops)
                {
                    DungeonBlade.Core.GameServices.Inventory?.TryAddItem(d);
                }
            }

            // Disable colliders so corpse doesn't block player
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Destroy(gameObject, 8f);
        }
    }
}
