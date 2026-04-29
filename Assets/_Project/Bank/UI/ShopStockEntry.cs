using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonBlade.Bank.UI
{
    public class ShopStockEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text priceText;
        [SerializeField] Button buyButton;

        public ShopUI Owner { get; private set; }
        public int StockIndex { get; private set; } = -1;

        public void Bind(ShopUI owner, int stockIndex, ShopDefinition.Stock stock, int price)
        {
            Owner = owner;
            StockIndex = stockIndex;

            if (iconImage != null)
            {
                iconImage.enabled = stock.Item != null && stock.Item.Icon != null;
                if (stock.Item != null && stock.Item.Icon != null) iconImage.sprite = stock.Item.Icon;
            }
            if (nameText != null) nameText.text = stock.Item != null ? stock.Item.DisplayName : "(empty)";
            if (priceText != null) priceText.text = $"{price}g";
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        void OnBuyClicked()
        {
            if (ShopManager.Instance != null) ShopManager.Instance.TryBuy(StockIndex);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (Owner == null || ShopManager.Instance == null || ShopManager.Instance.CurrentShop == null) return;
            var shop = ShopManager.Instance.CurrentShop;
            if (StockIndex < 0 || StockIndex >= shop.StockList.Count) return;
            var item = shop.StockList[StockIndex].Item;
            if (item != null && Owner.Tooltip != null) Owner.Tooltip.Show(item, transform.position);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (Owner != null && Owner.Tooltip != null) Owner.Tooltip.Hide();
        }
    }
}
