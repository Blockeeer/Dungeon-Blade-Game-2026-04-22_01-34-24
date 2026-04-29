using UnityEngine;
using DungeonBlade.Combat;

namespace DungeonBlade.Enemies
{
    /// <summary>Skeleton Soldier (GDD 4.1): HP 30, melee rush, 10 dmg sword slash.</summary>
    public class SkeletonSoldier : EnemyBase
    {
        [Header("Attack")]
        public float attackDamage = 10f;
        public float attackCooldown = 1.3f;
        public float windupTime = 0.3f;       // anim windup before hit applied
        public float hitRange = 2.2f;
        public float hitConeHalfAngle = 60f;

        protected override void Awake()
        {
            maxHealth = 30f;
            xpReward = 15;
            goldMin = 1; goldMax = 5;
            attackRange = 2.0f;
            preferredRange = 1.8f;
            moveSpeed = 4f;
            base.Awake();
        }

        protected override void AttackTick()
        {
            if (player == null) { TransitionTo(State.Patrol); return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (d > attackRange * 1.3f)
            {
                TransitionTo(State.Chase);
                return;
            }
            FacePlayer();
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                if (animator != null) animator.SetTrigger("Attack");
                StartCoroutine(DoAttackAfterWindup());
            }
        }

        System.Collections.IEnumerator DoAttackAfterWindup()
        {
            yield return new WaitForSeconds(windupTime);
            if (state == State.Dead || player == null) yield break;

            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 toPlayer = player.position - origin;
            if (toPlayer.magnitude > hitRange) yield break;
            if (Vector3.Angle(transform.forward, toPlayer) > hitConeHalfAngle) yield break;

            var dmg = player.GetComponentInParent<IDamageable>();
            if (dmg == null) yield break;

            // Parry check
            var combat = player.GetComponent<DungeonBlade.Player.PlayerCombat>();
            if (combat != null && combat.TryParry(dmg, gameObject)) yield break;

            dmg.TakeDamage(new DamageInfo {
                amount = attackDamage,
                source = DamageSource.EnemyMelee,
                knockback = transform.forward * 3f + Vector3.up * 1f,
                hitPoint = player.position,
                attacker = gameObject,
            });
        }
    }
}
