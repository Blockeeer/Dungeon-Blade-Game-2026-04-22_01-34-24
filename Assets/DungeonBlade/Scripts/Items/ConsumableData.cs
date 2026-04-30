using UnityEngine;

namespace DungeonBlade.Items
{
    public enum ConsumableEffect { RestoreHp, RestoreStamina, ThrowGrenade, Buff }

    [CreateAssetMenu(menuName = "DungeonBlade/Consumable", fileName = "NewConsumable")]
    public class ConsumableData : ItemData
    {
        public ConsumableEffect effect = ConsumableEffect.RestoreHp;
        public float value = 50f;                   // hp/stamina to restore, or damage for grenade
        public float radius = 5f;                   // grenade radius
        public GameObject projectilePrefab;         // optional (grenades)

        void OnEnable() { category = ItemCategory.Consumable; stackable = true; maxStack = 99; }

        /// <summary>Apply effect. Returns true if used (consumed).</summary>
        public bool Use(DungeonBlade.Player.PlayerStats stats, Transform user)
        {
            switch (effect)
            {
                case ConsumableEffect.RestoreHp:
                    if (stats == null) return false;
                    stats.ApplyHeal(value);
                    return true;

                case ConsumableEffect.RestoreStamina:
                    if (stats == null) return false;
                    stats.ApplyStaminaRestore(value);
                    return true;

                case ConsumableEffect.ThrowGrenade:
                    if (projectilePrefab != null && user != null)
                    {
                        var proj = Instantiate(projectilePrefab, user.position + user.forward + Vector3.up, Quaternion.identity);
                        if (proj.TryGetComponent<Rigidbody>(out var rb))
                            rb.velocity = user.forward * 18f + Vector3.up * 4f;
                    }
                    return true;

                case ConsumableEffect.Buff:
                    // Extend via custom subclass as needed
                    return true;
            }
            return false;
        }
    }
}
