using UnityEngine;

namespace DungeonBlade.Combat
{
    /// <summary>Source of a damage event (for XP/combo credit, friendly fire checks).</summary>
    public enum DamageSource { PlayerMelee, PlayerRanged, EnemyMelee, EnemyRanged, Environment }

    /// <summary>All the data that travels with a damage event.</summary>
    public struct DamageInfo
    {
        public float amount;
        public DamageSource source;
        public Vector3 knockback;
        public Vector3 hitPoint;
        public GameObject attacker;
        public bool isHeavy;
        public bool isCritical;
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        bool IsDead { get; }
    }
}
