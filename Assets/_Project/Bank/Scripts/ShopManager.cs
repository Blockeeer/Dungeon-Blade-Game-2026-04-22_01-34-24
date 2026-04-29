using System;
using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public ShopDefinition CurrentShop { get; private set; }
        public event Action OnShopChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OpenShop(ShopDefinition def)
        {
            CurrentShop = def;
            OnShopChanged?.Invoke();
        }

        public void CloseShop()
        {
            CurrentShop = null;
            OnShopChanged?.Invoke();
        }

        public bool TryBuy(int stockIndex)
        {
            if (CurrentShop == null) return false;
            if (stockIndex < 0 || stockIndex >= CurrentShop.StockList.Count) return false;
            if (PlayerWallet.Instance == null || InventoryManager.Instance == null) return false;

            var s = CurrentShop.StockList[stockIndex];
            if (s.Item == null) return false;

            int price = CurrentShop.BuyPriceFor(stockIndex);
            if (!PlayerWallet.Instance.TrySpend(price))
            {
                Debug.Log($"[Shop] Not enough gold ({PlayerWallet.Instance.Gold} < {price}).");
                return false;
            }

            int leftover = InventoryManager.Instance.AddItem(s.Item, 1);
            if (leftover > 0)
            {
                PlayerWallet.Instance.Add(price);
                Debug.Log($"[Shop] Inventory full — refunded {price}g.");
                return false;
            }

            Debug.Log($"[Shop] Bought {s.Item.DisplayName} for {price}g.");
            return true;
        }

        public bool TrySell(SlotKind kind, int invIdx)
        {
            if (CurrentShop == null) return false;
            if (PlayerWallet.Instance == null || InventoryManager.Instance == null) return false;

            var slot = InventoryManager.Instance.GetSlot(kind, invIdx);
            if (slot.IsEmpty) return false;

            int price = CurrentShop.SellPriceFor(slot.Item);
            if (!InventoryManager.Instance.RemoveItemAt(kind, invIdx, 1)) return false;
            PlayerWallet.Instance.Add(price);

            Debug.Log($"[Shop] Sold 1× {slot.Item.DisplayName} for {price}g.");
            return true;
        }
    }
}
