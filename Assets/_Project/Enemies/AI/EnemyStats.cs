using System;
using UnityEngine;

namespace DungeonBlade.Enemies
{
    [Serializable]
    public class EnemyStats
    {
        [Header("Health")]
        public float MaxHealth = 100f;

        [Header("Combat")]
        public float ContactDamage = 10f;
        public float AttackDamage = 15f;
        public float AttackCooldown = 1.5f;
        public float AttackWindup = 0.35f;
        public float AttackRecovery = 0.45f;

        [Header("Perception")]
        public float AggroRange = 10f;
        public float SightAngle = 90f;
        public float LoseAggroRange = 18f;
        public float LoseAggroTime = 4f;

        [Header("Movement")]
        public float MoveSpeed = 3.5f;
        public float ChaseSpeed = 5f;
        public float TurnSpeed = 540f;

        [Header("Ranged (Archer only)")]
        public float ProjectileSpeed = 20f;
        public float ProjectileLifetime = 4f;
        public float AttackRange = 2f;
        public float KeepDistance = 0f;

        [Header("Armor (Knight only)")]
        [Range(0f, 1f)] public float DamageReduction = 0f;
    }
}
