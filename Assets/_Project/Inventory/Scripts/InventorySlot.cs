using System;

namespace DungeonBlade.Inventory
{
    [Serializable]
    public struct InventorySlot
    {
        public Item Item;
        public int Quantity;

        public bool IsEmpty => Item == null || Quantity <= 0;
        public int FreeSpace => Item == null ? 0 : Math.Max(0, Item.MaxStack - Quantity);

        public static InventorySlot Empty => new InventorySlot { Item = null, Quantity = 0 };

        public static InventorySlot Of(Item item, int qty)
        {
            return new InventorySlot { Item = item, Quantity = item == null ? 0 : qty };
        }
    }
}
