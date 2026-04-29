using UnityEngine;

namespace DungeonBlade.Items
{
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum ItemCategory { Weapon, Armor, Accessory, Consumable, Material, Token }

    public enum EquipmentSlot {
        None,
        MainHand, OffHand,
        Head, Chest, Legs, Boots,
        Ring1, Ring2, Amulet
    }

    /// <summary>
    /// Base ScriptableObject for all items. Create via Assets → Create → DungeonBlade → Item.
    /// Subclass for Weapon/Armor/Consumable.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonBlade/Item (Base)", fileName = "NewItem")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;                    // stable ID for save system (e.g. "sword_common_01")
        public string displayName = "New Item";
        [TextArea] public string flavorText;
        public Sprite icon;

        [Header("Classification")]
        public ItemCategory category = ItemCategory.Material;
        public ItemRarity rarity = ItemRarity.Common;

        [Header("Economy")]
        public int baseValue = 10;               // gold
        public int levelRequirement = 1;
        public float weight = 1f;                // weight units

        [Header("Stacking")]
        public bool stackable = false;
        public int maxStack = 1;

        // Colors for tooltips/UI (matches GDD 6.1)
        public static Color RarityColor(ItemRarity r) => r switch {
            ItemRarity.Common     => new Color(0.80f, 0.80f, 0.80f),
            ItemRarity.Uncommon   => new Color(0.20f, 0.85f, 0.30f),
            ItemRarity.Rare       => new Color(0.20f, 0.55f, 0.95f),
            ItemRarity.Epic       => new Color(0.75f, 0.30f, 0.95f),
            ItemRarity.Legendary  => new Color(1.00f, 0.60f, 0.10f),
            _                     => Color.white,
        };

        public int SellPrice => Mathf.Max(1, Mathf.RoundToInt(baseValue * 0.30f));
    }
}
