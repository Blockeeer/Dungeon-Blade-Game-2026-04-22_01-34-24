using UnityEngine;
using DungeonBlade.Combat;

namespace DungeonBlade.Enemies
{
    /// <summary>Skeleton Archer (GDD 4.1): HP 20, stationary-ish, ranged arrow with knockback.</summary>
    public class SkeletonArcher : EnemyBase
    {
        [Header("Attack")]
        public float arrowDamage = 8f;
        public float arrowSpeed = 25f;
        public float shootCooldown = 2.0f;
        public float windupTime = 0.6f;
        public Transform arrowSpawnPoint;
        public GameObject arrowProjectilePrefab;

        protected override void Awake()
        {
            maxHealth = 20f;
            xpReward = 15;
            goldMin = 1; goldMax = 3;
            attackRange = 16f;        // engagement distance
            preferredRange = 12f;     // stand off
            moveSpeed = 2.5f;
            base.Awake();
        }

        protected override void AttackTick()
        {
            if (player == null) { TransitionTo(State.Patrol); return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (d > attackRange * 1.2f) { TransitionTo(State.Chase); return; }
            if (!HasLineOfSightToPlayer()) { TransitionTo(State.Chase); return; }

            FacePlayer();

            // If player is too close, kite back
            if (d < preferredRange * 0.6f && agent.isOnNavMesh)
            {
                Vector3 away = (transform.position - player.position).normalized;
                agent.SetDestination(transform.position + away * 3f);
            }

            if (Time.time - lastAttackTime >= shootCooldown)
            {
                lastAttackTime = Time.time;
                if (animator != null) animator.SetTrigger("Attack");
                StartCoroutine(ShootArrow());
            }
        }

        System.Collections.IEnumerator ShootArrow()
        {
            yield return new WaitForSeconds(windupTime);
            if (state == State.Dead || player == null) yield break;

            Vector3 spawn = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
            Vector3 targetPoint = player.position + Vector3.up * 1.0f;
            Vector3 dir = (targetPoint - spawn).normalized;

            if (arrowProjectilePrefab != null)
            {
                var arrow = Instantiate(arrowProjectilePrefab, spawn, Quaternion.LookRotation(dir));
                var proj = arrow.GetComponent<Projectile>();
                if (proj == null) proj = arrow.AddComponent<Projectile>();
                proj.Launch(dir, arrowSpeed, arrowDamage, gameObject, DamageSource.EnemyRanged,
                           knockback: dir * 3f);
            }
            else
            {
                // Fallback: instant hitscan
                if (Physics.Raycast(spawn, dir, out RaycastHit hit, 40f))
                {
                    var dmg = hit.collider.GetComponentInParent<IDamageable>();
                    if (dmg != null && !dmg.IsDead)
                    {
                        dmg.TakeDamage(new DamageInfo {
                            amount = arrowDamage,
                            source = DamageSource.EnemyRanged,
                            knockback = dir * 3f,
                            hitPoint = hit.point,
                            attacker = gameObject,
                        });
                    }
                }
            }
        }
    }
}
