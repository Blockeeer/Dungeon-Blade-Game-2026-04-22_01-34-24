using DungeonBlade.Inventory;
using DungeonBlade.Inventory.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonBlade.Bank.UI
{
    public class BankSlotWidget : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] Image background;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text quantityText;
        [SerializeField] Color emptyColor = new Color(0.15f, 0.18f, 0.15f, 0.85f);
        [SerializeField] Color filledColor = new Color(0.25f, 0.32f, 0.25f, 0.95f);
        [SerializeField] Color hoverColor = new Color(0.4f, 0.55f, 0.4f, 1f);

        public int Index { get; private set; }
        public BankUI Owner { get; private set; }
        public InventorySlot Data { get; private set; } = InventorySlot.Empty;

        public void Bind(BankUI owner, int index)
        {
            Owner = owner;
            Index = index;
            Refresh();
        }

        public void Refresh()
        {
            if (BankManager.Instance == null) return;
            Data = BankManager.Instance.GetSlot(Index);

            if (background != null) background.color = Data.IsEmpty ? emptyColor : filledColor;
            if (iconImage != null)
            {
                iconImage.enabled = !Data.IsEmpty && Data.Item.Icon != null;
                if (!Data.IsEmpty && Data.Item.Icon != null) iconImage.sprite = Data.Item.Icon;
            }
            if (quantityText != null)
            {
                bool show = !Data.IsEmpty && Data.Quantity > 1;
                quantityText.gameObject.SetActive(show);
                if (show) quantityText.text = Data.Quantity.ToString();
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (background != null && !Data.IsEmpty) background.color = hoverColor;
            if (!Data.IsEmpty) Owner?.Tooltip?.Show(Data.Item, transform.position);
        }

        public void OnPointerExit(PointerEventData e)
        {
            Refresh();
            Owner?.Tooltip?.Hide();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (Data.IsEmpty) return;
            Owner?.BeginDrag(this);
            DragRouter.BeginBankDrag(Index, Data.Item);
        }

        public void OnDrag(PointerEventData e) => Owner?.UpdateDrag(e.position);

        public void OnEndDrag(PointerEventData e)
        {
            Owner?.EndDrag();
            DragRouter.End();
        }

        public void OnDrop(PointerEventData e)
        {
            if (BankManager.Instance == null) return;

            if (DragRouter.SourceKind == DragSourceKind.Bank)
            {
                if (DragRouter.SourceIndex == Index) return;
                BankManager.Instance.MoveOrSwap(DragRouter.SourceIndex, Index);
                return;
            }

            if (DragRouter.SourceKind == DragSourceKind.Inventory)
            {
                BankManager.Instance.DepositFromInventory(DragRouter.InventorySourceKind, DragRouter.SourceIndex);
                return;
            }
        }
    }
}
