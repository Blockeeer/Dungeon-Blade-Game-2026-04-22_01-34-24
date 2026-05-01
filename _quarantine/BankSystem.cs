using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DungeonBlade.Items;
using DungeonBlade.Inventory;

namespace DungeonBlade.Bank
{
    [System.Serializable]
    public class ShopEntry
    {
        public ItemData item;
        public int priceOverride;   // 0 = use item.baseValue
        public int stock = -1;      // -1 = infinite
    }

    /// <summary>
    /// Bank + shop per GDD Section 8.
    /// - 120-slot vault (6x20)
    /// - Sell items at 30% of base value
    /// - Buy consumables/common/uncommon gear
    /// - Spend Dungeon Clear Tokens for exclusive items
    /// - Gold stored here is lobby-safe (separate from adventuring purse? Phase 1: shared)
    /// </summary>
    public class BankSystem : MonoBehaviour
    {
        public const int VaultSize = 120;

        [Header("Storage")]
        public InventorySlot[] vault;

        [Header("Shop — Gold")]
        public List<ShopEntry> shopItems = new List<ShopEntry>();

        [Header("Shop — Token Exchange")]
        public List<ShopEntry> tokenShopItems = new List<ShopEntry>();

        [Header("Links")]
        public InventorySystem playerInventory;

        [Header("Events")]
        public UnityEvent OnVaultChanged;
        public UnityEvent<ItemData, int> OnItemSold;      // (item, gold)
        public UnityEvent<ItemData, int> OnItemBought;

        void Awake()
        {
            if (vault == null || vault.Length != VaultSize)
            {
                vault = new InventorySlot[VaultSize];
                for (int i = 0; i < VaultSize; i++) vault[i] = new InventorySlot();
            }
        }

        // ─────── Vault ───────

        public bool DepositFromInventory(ItemData item, int quantity = 1)
        {
            if (playerInventory == null || item == null) return false;
            if (playerInventory.CountItem(item) < quantity) return false;

            int remaining = quantity;
            if (item.stackable)
            {
                foreach (var s in vault)
                {
                    if (s.CanStack(item))
                    {
                        int move = Mathf.Min(item.maxStack - s.quantity, remaining);
                        s.quantity += move;
                        remaining -= move;
                        if (remaining <= 0) break;
                    }
                }
            }
            while (remaining > 0)
            {
                var empty = FirstEmpty();
                if (empty == null) return false;
                empty.item = item;
                empty.quantity = item.stackable ? Mathf.Min(item.maxStack, remaining) : 1;
                remaining -= empty.quantity;
            }

            playerInventory.RemoveItem(item, quantity);
            OnVaultChanged?.Invoke();
            return true;
        }

        public bool WithdrawToInventory(int vaultIndex, int quantity = 1)
        {
            if (vaultIndex < 0 || vaultIndex >= VaultSize) return false;
            var slot = vault[vaultIndex];
            if (slot.IsEmpty) return false;
            int take = Mathf.Min(slot.quantity, quantity);
            if (!playerInventory.TryAddItem(slot.item, take)) return false;
            slot.quantity -= take;
            if (slot.quantity <= 0) slot.Clear();
            OnVaultChanged?.Invoke();
            return true;
        }

        InventorySlot FirstEmpty()
        {
            foreach (var s in vault) if (s.IsEmpty) return s;
            return null;
        }

        // ─────── Shop ───────

        public bool Sell(ItemData item, int quantity = 1)
        {
            if (playerInventory == null || item == null) return false;
            if (playerInventory.CountItem(item) < quantity) return false;
            int total = item.SellPrice * quantity;
            playerInventory.RemoveItem(item, quantity);
            playerInventory.AddGold(total);
            OnItemSold?.Invoke(item, total);
            return true;
        }

        public bool Buy(ShopEntry entry, int quantity = 1)
        {
            if (entry == null || entry.item == null) return false;
            int unit = entry.priceOverride > 0 ? entry.priceOverride : entry.item.baseValue;
            int total = unit * quantity;
            if (entry.stock >= 0 && entry.stock < quantity) return false;
            if (!playerInventory.SpendGold(total)) return false;
            if (!playerInventory.TryAddItem(entry.item, quantity))
            {
                playerInventory.AddGold(total);  // refund
                return false;
            }
            if (entry.stock >= 0) entry.stock -= quantity;
            OnItemBought?.Invoke(entry.item, total);
            return true;
        }

        public bool RedeemToken(ShopEntry entry, int quantity = 1)
        {
            if (entry == null || entry.item == null) return false;
            int tokens = entry.priceOverride > 0 ? entry.priceOverride : 1;
            int total = tokens * quantity;
            if (entry.stock >= 0 && entry.stock < quantity) return false;
            if (!playerInventory.SpendTokens(total)) return false;
            if (!playerInventory.TryAddItem(entry.item, quantity))
            {
                playerInventory.AddTokens(total);
                return false;
            }
            if (entry.stock >= 0) entry.stock -= quantity;
            OnItemBought?.Invoke(entry.item, total);
            return true;
        }
    }
}
