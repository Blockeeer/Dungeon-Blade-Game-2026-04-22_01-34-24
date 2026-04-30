using UnityEngine;
using DungeonBlade.Combat;

namespace DungeonBlade.Enemies
{
    /// <summary>Armored Knight elite (GDD 4.1): HP 80, heavy slam (25 dmg), shield bash. Slow but powerful.</summary>
    public class ArmoredKnight : EnemyBase
    {
        public enum AttackKind { Slam, ShieldBash }

        [Header("Slam")]
        public float slamDamage = 25f;
        public float slamRange = 3.0f;
        public float slamConeHalfAngle = 80f;
        public float slamWindup = 0.8f;
        public float slamCooldown = 3.0f;

        [Header("Shield Bash")]
        public float bashDamage = 15f;
        public float bashRange = 2.2f;
        public float bashWindup = 0.35f;
        public float bashCooldown = 1.6f;

        [Header("Other")]
        public float flatDamageReduction = 6f;      // heavy armor

        float lastSlamTime = -99f;
        float lastBashTime = -99f;

        protected override void Awake()
        {
            maxHealth = 80f;
            xpReward = 60;
            goldMin = 10; goldMax = 20;
            attackRange = 2.8f;
            preferredRange = 2.2f;
            moveSpeed = 2.8f;
            base.Awake();
        }

        public override void TakeDamage(DamageInfo info)
        {
            info.amount = Mathf.Max(1f, info.amount - flatDamageReduction);
            base.TakeDamage(info);
        }

        protected override void AttackTick()
        {
            if (player == null) { TransitionTo(State.Patrol); return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (d > attackRange * 1.4f) { TransitionTo(State.Chase); return; }

            FacePlayer();

            bool slamReady = Time.time - lastSlamTime >= slamCooldown;
            bool bashReady = Time.time - lastBashTime >= bashCooldown;

            if (d < bashRange && bashReady)
            {
                lastBashTime = Time.time;
                if (animator != null) animator.SetTrigger("ShieldBash");
                StartCoroutine(DoAttack(AttackKind.ShieldBash));
            }
            else if (slamReady)
            {
                lastSlamTime = Time.time;
                if (animator != null) animator.SetTrigger("Slam");
                StartCoroutine(DoAttack(AttackKind.Slam));
            }
        }

        System.Collections.IEnumerator DoAttack(AttackKind kind)
        {
            float windup = kind == AttackKind.Slam ? slamWindup : bashWindup;
            yield return new WaitForSeconds(windup);
            if (state == State.Dead || player == null) yield break;

            float dmg = kind == AttackKind.Slam ? slamDamage : bashDamage;
            float range = kind == AttackKind.Slam ? slamRange : bashRange;
            float cone = kind == AttackKind.Slam ? slamConeHalfAngle : 40f;

            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 toPlayer = player.position - origin;
            if (toPlayer.magnitude > range) yield break;
            if (Vector3.Angle(transform.forward, toPlayer) > cone) yield break;

            var dmgTarget = player.GetComponentInParent<IDamageable>();
            if (dmgTarget == null) yield break;

            var combat = player.GetComponent<DungeonBlade.Player.PlayerCombat>();
            if (combat != null && combat.TryParry(dmgTarget, gameObject)) yield break;

            dmgTarget.TakeDamage(new DamageInfo {
                amount = dmg,
                source = DamageSource.EnemyMelee,
                knockback = transform.forward * (kind == AttackKind.ShieldBash ? 7f : 4f) + Vector3.up * 2f,
                hitPoint = player.position,
                attacker = gameObject,
                isHeavy = kind == AttackKind.Slam,
            });
        }
    }
}
