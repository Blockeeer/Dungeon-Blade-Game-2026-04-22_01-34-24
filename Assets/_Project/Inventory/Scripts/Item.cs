using UnityEngine;

namespace DungeonBlade.Inventory
{
    public enum ItemType { Misc, Weapon, Consumable, Material, KeyItem }
    public enum EquipmentSlot { None, Head, Body, MainHand, OffHand }

    [CreateAssetMenu(menuName = "DungeonBlade/Item/Misc Item", fileName = "NewItem")]
    public class Item : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string itemId = "item_id";
        [SerializeField] string displayName = "New Item";
        [TextArea(2, 5)]
        [SerializeField] string description = "";
        [SerializeField] Sprite icon;

        [Header("Type")]
        [SerializeField] ItemType type = ItemType.Misc;
        [SerializeField] EquipmentSlot equipSlot = EquipmentSlot.None;

        [Header("Stack")]
        [SerializeField] bool stackable = true;
        [SerializeField] int maxStack = 99;

        [Header("Economy")]
        [SerializeField] int sellValue = 1;
        [SerializeField] int buyValue = 5;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => type;
        public EquipmentSlot EquipSlot => equipSlot;
        public bool Stackable => stackable;
        public int MaxStack => stackable ? Mathf.Max(1, maxStack) : 1;
        public int SellValue => sellValue;
        public int BuyValue => buyValue;

        public virtual void OnUse(GameObject user) { }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId)) itemId = name.ToLowerInvariant().Replace(' ', '_');
        }
#endif
    }
}
