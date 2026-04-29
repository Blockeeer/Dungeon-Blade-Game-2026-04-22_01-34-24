using UnityEngine;

namespace DungeonBlade.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(in DamageInfo info);
    }

    public struct DamageInfo
    {
        public float Amount;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public float Knockback;
        public GameObject Source;
        public DamageType Type;
        public bool IsParryable;
    }

    public enum DamageType
    {
        Melee,
        Ranged,
        Explosive,
        Environmental,
    }
}
