using System;
using System.Collections.Generic;
using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class BankManager : MonoBehaviour
    {
        public const int BankSize = 48;

        public static BankManager Instance { get; private set; }

        InventorySlot[] _slots = new InventorySlot[BankSize];
        int _storedGold;

        public IReadOnlyList<InventorySlot> Slots => _slots;
        public int StoredGold => _storedGold;

        public event Action OnBankChanged;
        public event Action<int> OnGoldChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length) return InventorySlot.Empty;
            return _slots[index];
        }

        public int AddItem(Item item, int quantity)
        {
            if (item == null || quantity <= 0) return quantity;
            int remaining = quantity;

            if (item.Stackable)
            {
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    if (_slots[i].Item == item && _slots[i].FreeSpace > 0)
                    {
                        int put = Mathf.Min(remaining, _slots[i].FreeSpace);
                        _slots[i].Quantity += put;
                        remaining -= put;
                    }
                }
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int put = Mathf.Min(remaining, item.MaxStack);
                    _slots[i] = InventorySlot.Of(item, put);
                    remaining -= put;
                }
            }

            if (remaining < quantity) OnBankChanged?.Invoke();
            return remaining;
        }

        public bool MoveOrSwap(int fromIdx, int toIdx)
        {
            if (fromIdx < 0 || fromIdx >= _slots.Length) return false;
            if (toIdx < 0 || toIdx >= _slots.Length) return false;

            var src = _slots[fromIdx];
            var dst = _slots[toIdx];
            if (src.IsEmpty) return false;

            if (!dst.IsEmpty && dst.Item == src.Item && src.Item.Stackable)
            {
                int put = Mathf.Min(src.Quantity, dst.FreeSpace);
                if (put > 0)
                {
                    dst.Quantity += put;
                    src.Quantity -= put;
                    _slots[toIdx] = dst;
                    _slots[fromIdx] = src.Quantity > 0 ? src : InventorySlot.Empty;
                    OnBankChanged?.Invoke();
                    return true;
                }
            }

            _slots[fromIdx] = dst;
            _slots[toIdx] = src;
            OnBankChanged?.Invoke();
            return true;
        }

        public bool MoveToInventory(int bankIdx, int qty = -1)
        {
            if (bankIdx < 0 || bankIdx >= _slots.Length) return false;
            if (_slots[bankIdx].IsEmpty || InventoryManager.Instance == null) return false;

            int amount = qty <= 0 ? _slots[bankIdx].Quantity : Mathf.Min(qty, _slots[bankIdx].Quantity);
            int leftover = InventoryManager.Instance.AddItem(_slots[bankIdx].Item, amount);
            int moved = amount - leftover;
            if (moved <= 0) return false;

            _slots[bankIdx].Quantity -= moved;
            if (_slots[bankIdx].Quantity <= 0) _slots[bankIdx] = InventorySlot.Empty;
            OnBankChanged?.Invoke();
            return true;
        }

        public bool DepositFromInventory(SlotKind kind, int invIdx, int qty = -1)
        {
            if (InventoryManager.Instance == null) return false;
            var src = InventoryManager.Instance.GetSlot(kind, invIdx);
            if (src.IsEmpty) return false;

            int amount = qty <= 0 ? src.Quantity : Mathf.Min(qty, src.Quantity);
            int leftover = AddItem(src.Item, amount);
            int moved = amount - leftover;
            if (moved <= 0) return false;

            InventoryManager.Instance.RemoveItemAt(kind, invIdx, moved);
            return true;
        }

        public void DepositGold(int amount)
        {
            if (amount <= 0) return;
            if (PlayerWallet.Instance == null) return;
            if (!PlayerWallet.Instance.TrySpend(amount)) return;
            _storedGold += amount;
            OnGoldChanged?.Invoke(_storedGold);
            Debug.Log($"[Bank] Deposited {amount}g. Vault: {_storedGold}g.");
        }

        public void WithdrawGold(int amount)
        {
            if (amount <= 0) return;
            int actual = Mathf.Min(amount, _storedGold);
            if (actual <= 0) return;
            _storedGold -= actual;
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.Add(actual);
            OnGoldChanged?.Invoke(_storedGold);
            Debug.Log($"[Bank] Withdrew {actual}g. Vault: {_storedGold}g.");
        }

        public void SetStoredGold(int amount)
        {
            _storedGold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(_storedGold);
        }

        public List<SerializedSlot> SerializeAll()
        {
            var list = new List<SerializedSlot>(BankSize);
            for (int i = 0; i < _slots.Length; i++) list.Add(SerializedSlot.From(SlotKind.Grid, i, _slots[i]));
            return list;
        }

        public void DeserializeAll(List<SerializedSlot> slots, Func<string, Item> resolver)
        {
            if (resolver == null) return;
            for (int i = 0; i < _slots.Length; i++) _slots[i] = InventorySlot.Empty;
            if (slots == null) return;

            foreach (var s in slots)
            {
                if (string.IsNullOrEmpty(s.itemId) || s.quantity <= 0) continue;
                var item = resolver(s.itemId);
                if (item == null) continue;
                if (s.index < 0 || s.index >= _slots.Length) continue;
                _slots[s.index] = InventorySlot.Of(item, s.quantity);
            }
            OnBankChanged?.Invoke();
        }
    }
}
