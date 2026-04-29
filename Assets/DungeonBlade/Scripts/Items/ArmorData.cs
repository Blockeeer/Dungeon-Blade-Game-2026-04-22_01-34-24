using UnityEngine;

namespace DungeonBlade.Items
{
    [CreateAssetMenu(menuName = "DungeonBlade/Armor", fileName = "NewArmor")]
    public class ArmorData : ItemData
    {
        [Header("Armor")]
        public EquipmentSlot slot = EquipmentSlot.Chest;
        public float flatDefense = 2f;            // adds to flatArmor
        public float percentDefense = 0f;         // adds to percentArmor (0..0.85)
        public float bonusMaxHp = 0f;
        public float bonusMaxStamina = 0f;
        public float staminaRegenBonus = 0f;

        void OnEnable() { category = ItemCategory.Armor; }
    }
}
