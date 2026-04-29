using System.Collections;
using DungeonBlade.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonBlade.Enemies
{
    public enum EnemyState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected EnemyStats stats = new EnemyStats();

        [Header("Patrol")]
        [SerializeField] protected Vector2 patrolRadius = new Vector2(2f, 6f);
        [SerializeField] protected Vector2 patrolWaitTime = new Vector2(1f, 3f);

        [Header("Perception")]
        [SerializeField] protected LayerMask sightBlockers = ~0;
        [SerializeField] protected Transform eyeTransform;

        [Header("FX")]
        [SerializeField] protected Color flashColor = Color.red;
        [SerializeField] protected float flashDuration = 0.08f;
        [SerializeField] protected float deathFadeDelay = 2f;

        [Header("Rewards")]
        [SerializeField] protected Rewards.LootTable lootTable;

        public EnemyStats Stats => stats;
        public EnemyState State { get; protected set; } = EnemyState.Idle;
        public float Health { get; protected set; }
        public bool IsAlive => Health > 0f && State != EnemyState.Dead;
        public Transform Target { get; protected set; }

        public void ReapplyStats()
        {
            Health = stats.MaxHealth;
            if (Agent != null)
            {
                Agent.speed = stats.MoveSpeed;
                Agent.angularSpeed = stats.TurnSpeed;
                Agent.stoppingDistance = Mathf.Max(0.1f, stats.AttackRange * 0.8f);
            }
        }

        protected NavMeshAgent Agent;
        protected Vector3 SpawnPosition;

        Renderer[] _renderers;
        Color[] _baseColors;
        float _flashEndTime = -1f;
        float _lastSeenTime = -999f;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Agent.speed = stats.MoveSpeed;
            Agent.angularSpeed = stats.TurnSpeed;
            Agent.stoppingDistance = Mathf.Max(0.1f, stats.AttackRange * 0.8f);

            Health = stats.MaxHealth;
            SpawnPosition = transform.position;

            if (eyeTransform == null) eyeTransform = transform;

            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].material;
                if (mat.HasProperty("_BaseColor")) _baseColors[i] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) _baseColors[i] = mat.GetColor("_Color");
            }
        }

        protected virtual void Update()
        {
            if (_flashEndTime > 0f && Time.time >= _flashEndTime)
            {
                _flashEndTime = -1f;
                ApplyColor(false);
            }

            if (!IsAlive) return;

            UpdatePerception();
            UpdateState();
        }

        protected virtual void UpdatePerception()
        {
            if (Target == null)
            {
                var player = FindPlayer();
                if (player != null && CanSee(player)) AcquireTarget(player);
                return;
            }

            if (!Target.gameObject.activeInHierarchy)
            {
                LoseTarget();
                return;
            }

            if (CanSee(Target))
            {
                _lastSeenTime = Time.time;
            }
            else
            {
                float dist = Vector3.Distance(transform.position, Target.position);
                if (dist > stats.LoseAggroRange || Time.time - _lastSeenTime > stats.LoseAggroTime)
                {
                    LoseTarget();
                }
            }
        }

        protected virtual Transform FindPlayer()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            return go != null ? go.transform : null;
        }

        protected bool CanSee(Transform target)
        {
            Vector3 eye = eyeTransform.position;
            Vector3 to = target.position - eye;
            float dist = to.magnitude;
            if (dist > stats.AggroRange) return false;

            Vector3 dir = to / Mathf.Max(dist, 0.0001f);
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > stats.SightAngle * 0.5f) return false;

            if (Physics.Raycast(eye, dir, out var hit, dist, sightBlockers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != target && !hit.transform.IsChildOf(target)) return false;
            }
            return true;
        }

        protected virtual void AcquireTarget(Transform t)
        {
            Target = t;
            _lastSeenTime = Time.time;
            TransitionTo(EnemyState.Chase);
        }

        protected virtual void LoseTarget()
        {
            Target = null;
            TransitionTo(EnemyState.Idle);
        }

        protected abstract void UpdateState();

        protected virtual void TransitionTo(EnemyState next)
        {
            if (State == next || State == EnemyState.Dead) return;
            OnExitState(State);
            State = next;
            OnEnterState(next);
        }

        protected virtual void OnEnterState(EnemyState s) { }
        protected virtual void OnExitState(EnemyState s) { }

        public virtual void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive) return;

            float amount = info.Amount * (1f - Mathf.Clamp01(stats.DamageReduction));
            Health -= amount;

            ApplyColor(true);
            _flashEndTime = Time.time + flashDuration;

            if (info.Source != null && info.Source.CompareTag("Player"))
            {
                AcquireTarget(info.Source.transform);
            }

            Debug.Log($"[{name}] -{amount:F0} ({info.Type})  HP: {Mathf.Max(0f, Health):F0}/{stats.MaxHealth:F0}");

            if (Health <= 0f)
            {
                Die();
            }
            else
            {
                OnHurt(info);
            }
        }

        protected virtual void OnHurt(in DamageInfo info) { }

        protected virtual void Die()
        {
            Health = 0f;
            TransitionTo(EnemyState.Dead);
            if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = true;
            Debug.Log($"[{name}] died.");

            if (lootTable != null)
            {
                Rewards.DropSpawner.SpawnLoot(lootTable, transform.position + Vector3.up * 0.5f, scatterRadius: 1.5f);
            }

            StartCoroutine(FadeAndDestroy());
        }

        IEnumerator FadeAndDestroy()
        {
            yield return new WaitForSeconds(deathFadeDelay);
            Destroy(gameObject);
        }

        void ApplyColor(bool flash)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var mat = _renderers[i].material;
                Color c = flash ? flashColor : _baseColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }

        protected void FaceTarget(Vector3 worldPos)
        {
            Vector3 dir = worldPos - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, stats.TurnSpeed * Time.deltaTime);
        }

        protected bool PickPatrolPoint(out Vector3 point)
        {
            float r = Random.Range(patrolRadius.x, patrolRadius.y);
            Vector2 circle = Random.insideUnitCircle.normalized * r;
            Vector3 candidate = SpawnPosition + new Vector3(circle.x, 0f, circle.y);
            if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
            point = SpawnPosition;
            return false;
        }

        public float NextPatrolWait() => Random.Range(patrolWaitTime.x, patrolWaitTime.y);

        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 eye = eyeTransform != null ? eyeTransform.position : transform.position;
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(eye, stats != null ? stats.AggroRange : 10f);

            if (stats == null) return;
            Gizmos.color = Color.red;
            float half = stats.SightAngle * 0.5f * Mathf.Deg2Rad;
            Vector3 fwd = transform.forward * stats.AggroRange;
            Vector3 right = Quaternion.Euler(0, stats.SightAngle * 0.5f, 0) * transform.forward * stats.AggroRange;
            Vector3 left = Quaternion.Euler(0, -stats.SightAngle * 0.5f, 0) * transform.forward * stats.AggroRange;
            Gizmos.DrawLine(eye, eye + right);
            Gizmos.DrawLine(eye, eye + left);
        }
    }
}
