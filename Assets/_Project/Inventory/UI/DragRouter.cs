using UnityEngine;

namespace DungeonBlade.Inventory.UI
{
    public enum DragSourceKind { None, Inventory, Bank }

    public static class DragRouter
    {
        public static DragSourceKind SourceKind { get; private set; } = DragSourceKind.None;
        public static SlotKind InventorySourceKind { get; private set; }
        public static int SourceIndex { get; private set; } = -1;
        public static Item SourceItem { get; private set; }

        public static void BeginInventoryDrag(SlotKind kind, int index, Item item)
        {
            SourceKind = DragSourceKind.Inventory;
            InventorySourceKind = kind;
            SourceIndex = index;
            SourceItem = item;
        }

        public static void BeginBankDrag(int index, Item item)
        {
            SourceKind = DragSourceKind.Bank;
            SourceIndex = index;
            SourceItem = item;
        }

        public static void End()
        {
            SourceKind = DragSourceKind.None;
            SourceIndex = -1;
            SourceItem = null;
        }
    }
}
