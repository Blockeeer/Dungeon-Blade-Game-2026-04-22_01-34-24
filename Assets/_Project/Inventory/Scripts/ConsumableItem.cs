using DungeonBlade.Player;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    [CreateAssetMenu(menuName = "DungeonBlade/Item/Consumable Item", fileName = "NewConsumable")]
    public class ConsumableItem : Item
    {
        [Header("Effect")]
        [SerializeField] float healAmount = 0f;
        [SerializeField] float staminaAmount = 0f;
        [SerializeField] float useCooldown = 0.5f;

        public float HealAmount => healAmount;
        public float StaminaAmount => staminaAmount;
        public float UseCooldown => useCooldown;

        public override void OnUse(GameObject user)
        {
            if (user == null) return;
            var stats = user.GetComponent<PlayerStats>();
            if (stats == null) return;

            if (healAmount > 0f) stats.Heal(healAmount);
            if (staminaAmount > 0f) stats.RestoreStamina(staminaAmount);
            Debug.Log($"[Consumable] {DisplayName} used → +{healAmount} HP, +{staminaAmount} stamina");
        }
    }
}
