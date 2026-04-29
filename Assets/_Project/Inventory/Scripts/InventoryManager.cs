using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    public enum SlotKind { Grid, Equipment, Hotbar }

    public class InventoryManager : MonoBehaviour
    {
        public const int GridWidth = 6;
        public const int GridHeight = 8;
        public const int GridSize = GridWidth * GridHeight;
        public const int HotbarSize = 6;
        public const int EquipmentSize = 4;

        public static InventoryManager Instance { get; private set; }

        [Serializable]
        public struct StartingItem
        {
            public Item Item;
            public int Quantity;
        }

        [SerializeField] List<StartingItem> startingItems = new List<StartingItem>();

        InventorySlot[] _grid = new InventorySlot[GridSize];
        InventorySlot[] _hotbar = new InventorySlot[HotbarSize];
        InventorySlot[] _equipment = new InventorySlot[EquipmentSize];

        public IReadOnlyList<InventorySlot> Grid => _grid;
        public IReadOnlyList<InventorySlot> Hotbar => _hotbar;
        public IReadOnlyList<InventorySlot> Equipment => _equipment;

        public event Action OnInventoryChanged;
        public event Action<EquipmentSlot, Item, Item> OnEquipmentChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            for (int i = 0; i < startingItems.Count; i++)
            {
                var entry = startingItems[i];
                if (entry.Item == null) continue;
                AddItem(entry.Item, Mathf.Max(1, entry.Quantity));
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public InventorySlot GetSlot(SlotKind kind, int index)
        {
            var arr = ArrayFor(kind);
            if (index < 0 || index >= arr.Length) return InventorySlot.Empty;
            return arr[index];
        }

        public bool SetSlot(SlotKind kind, int index, InventorySlot slot)
        {
            var arr = ArrayFor(kind);
            if (index < 0 || index >= arr.Length) return false;
            arr[index] = slot;
            OnInventoryChanged?.Invoke();
            return true;
        }

        InventorySlot[] ArrayFor(SlotKind k) =>
            k == SlotKind.Grid ? _grid : k == SlotKind.Hotbar ? _hotbar : _equipment;

        public int AddItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0) return quantity;

            int remaining = quantity;

            if (item.Stackable)
            {
                for (int i = 0; i < _grid.Length && remaining > 0; i++)
                {
                    if (_grid[i].Item == item && _grid[i].FreeSpace > 0)
                    {
                        int put = Mathf.Min(remaining, _grid[i].FreeSpace);
                        _grid[i].Quantity += put;
                        remaining -= put;
                    }
                }
            }

            for (int i = 0; i < _grid.Length && remaining > 0; i++)
            {
                if (_grid[i].IsEmpty)
                {
                    int put = Mathf.Min(remaining, item.MaxStack);
                    _grid[i] = InventorySlot.Of(item, put);
                    remaining -= put;
                }
            }

            if (remaining < quantity) OnInventoryChanged?.Invoke();
            if (remaining > 0) Debug.LogWarning($"[Inventory] Inventory full — dropped {remaining}× {item.DisplayName}.");
            return remaining;
        }

        public bool RemoveItemAt(SlotKind kind, int index, int quantity)
        {
            var arr = ArrayFor(kind);
            if (index < 0 || index >= arr.Length) return false;
            if (arr[index].IsEmpty) return false;
            if (arr[index].Quantity < quantity) return false;

            arr[index].Quantity -= quantity;
            if (arr[index].Quantity <= 0) arr[index] = InventorySlot.Empty;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void MoveOrSwap(SlotKind fromKind, int fromIdx, SlotKind toKind, int toIdx)
        {
            var fromArr = ArrayFor(fromKind);
            var toArr = ArrayFor(toKind);
            if (fromIdx < 0 || fromIdx >= fromArr.Length) return;
            if (toIdx < 0 || toIdx >= toArr.Length) return;

            var src = fromArr[fromIdx];
            var dst = toArr[toIdx];
            if (src.IsEmpty) return;

            if (toKind == SlotKind.Equipment)
            {
                if (!(src.Item is WeaponItem) || !IsEquipSlotMatch(src.Item, (EquipmentSlot)(toIdx + 1)))
                {
                    Debug.Log($"[Inventory] Cannot equip {src.Item.DisplayName} to slot {(EquipmentSlot)(toIdx + 1)}.");
                    return;
                }
            }
            if (fromKind == SlotKind.Equipment && !dst.IsEmpty)
            {
                if (!IsEquipSlotMatch(dst.Item, (EquipmentSlot)(fromIdx + 1)))
                {
                    Debug.Log($"[Inventory] Cannot move {dst.Item.DisplayName} into equipment slot {(EquipmentSlot)(fromIdx + 1)}.");
                    return;
                }
            }

            if (!dst.IsEmpty && dst.Item == src.Item && src.Item.Stackable)
            {
                int put = Mathf.Min(src.Quantity, dst.FreeSpace);
                if (put > 0)
                {
                    dst.Quantity += put;
                    src.Quantity -= put;
                    toArr[toIdx] = dst;
                    fromArr[fromIdx] = src.Quantity > 0 ? src : InventorySlot.Empty;
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }

            fromArr[fromIdx] = dst;
            toArr[toIdx] = src;

            if (fromKind == SlotKind.Equipment) RaiseEquipmentChanged((EquipmentSlot)(fromIdx + 1), src.Item, dst.Item);
            if (toKind == SlotKind.Equipment) RaiseEquipmentChanged((EquipmentSlot)(toIdx + 1), dst.Item, src.Item);

            OnInventoryChanged?.Invoke();
        }

        bool IsEquipSlotMatch(Item it, EquipmentSlot slot)
        {
            if (it == null) return true;
            return it.EquipSlot == slot;
        }

        void RaiseEquipmentChanged(EquipmentSlot slot, Item prev, Item next)
        {
            OnEquipmentChanged?.Invoke(slot, prev, next);
        }

        public bool UseSlot(SlotKind kind, int index, GameObject user)
        {
            var slot = GetSlot(kind, index);
            if (slot.IsEmpty) return false;
            if (slot.Item.Type != ItemType.Consumable) return false;

            slot.Item.OnUse(user);
            return RemoveItemAt(kind, index, 1);
        }

        public Item GetEquipped(EquipmentSlot slot)
        {
            int idx = (int)slot - 1;
            if (idx < 0 || idx >= _equipment.Length) return null;
            return _equipment[idx].Item;
        }

        public List<SerializedSlot> SerializeAll()
        {
            var list = new List<SerializedSlot>(GridSize + HotbarSize + EquipmentSize);
            for (int i = 0; i < _grid.Length; i++) list.Add(SerializedSlot.From(SlotKind.Grid, i, _grid[i]));
            for (int i = 0; i < _hotbar.Length; i++) list.Add(SerializedSlot.From(SlotKind.Hotbar, i, _hotbar[i]));
            for (int i = 0; i < _equipment.Length; i++) list.Add(SerializedSlot.From(SlotKind.Equipment, i, _equipment[i]));
            return list;
        }

        public void DeserializeAll(List<SerializedSlot> slots, Func<string, Item> resolver)
        {
            if (slots == null || resolver == null) return;
            for (int i = 0; i < _grid.Length; i++) _grid[i] = InventorySlot.Empty;
            for (int i = 0; i < _hotbar.Length; i++) _hotbar[i] = InventorySlot.Empty;
            for (int i = 0; i < _equipment.Length; i++) _equipment[i] = InventorySlot.Empty;

            foreach (var s in slots)
            {
                if (string.IsNullOrEmpty(s.itemId) || s.quantity <= 0) continue;
                var item = resolver(s.itemId);
                if (item == null) continue;
                var arr = ArrayFor(s.kind);
                if (s.index < 0 || s.index >= arr.Length) continue;
                arr[s.index] = InventorySlot.Of(item, s.quantity);
            }
            OnInventoryChanged?.Invoke();
        }
    }

    [Serializable]
    public struct SerializedSlot
    {
        public SlotKind kind;
        public int index;
        public string itemId;
        public int quantity;

        public static SerializedSlot From(SlotKind k, int i, InventorySlot s)
        {
            return new SerializedSlot
            {
                kind = k, index = i,
                itemId = s.IsEmpty ? "" : s.Item.ItemId,
                quantity = s.IsEmpty ? 0 : s.Quantity,
            };
        }
    }
}
