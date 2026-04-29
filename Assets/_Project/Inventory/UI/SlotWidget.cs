using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonBlade.Inventory.UI
{
    public class SlotWidget : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] Image background;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text quantityText;
        [SerializeField] Color emptyColor = new Color(0.15f, 0.15f, 0.18f, 0.85f);
        [SerializeField] Color filledColor = new Color(0.25f, 0.25f, 0.32f, 0.95f);
        [SerializeField] Color hoverColor = new Color(0.4f, 0.4f, 0.55f, 1f);

        public SlotKind Kind { get; private set; }
        public int Index { get; private set; }
        public InventoryUI Owner { get; private set; }
        public InventorySlot Data { get; private set; } = InventorySlot.Empty;

        public void Bind(InventoryUI owner, SlotKind kind, int index)
        {
            Owner = owner;
            Kind = kind;
            Index = index;
            Refresh();
        }

        public void Refresh()
        {
            if (Owner == null || InventoryManager.Instance == null) return;
            Data = InventoryManager.Instance.GetSlot(Kind, Index);

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

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right && !Data.IsEmpty)
            {
                if (Data.Item.Type == ItemType.Consumable)
                {
                    InventoryManager.Instance.UseSlot(Kind, Index, Owner.PlayerRef);
                }
            }
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (Data.IsEmpty) return;
            Owner?.BeginDrag(this);
            DragRouter.BeginInventoryDrag(Kind, Index, Data.Item);
        }

        public void OnDrag(PointerEventData e)
        {
            Owner?.UpdateDrag(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            Owner?.EndDrag();
            DragRouter.End();
        }

        public void OnDrop(PointerEventData e)
        {
            if (Owner == null) return;

            if (DragRouter.SourceKind == DragSourceKind.Inventory)
            {
                if (DragRouter.SourceIndex == Index && DragRouter.InventorySourceKind == Kind) return;
                InventoryManager.Instance.MoveOrSwap(DragRouter.InventorySourceKind, DragRouter.SourceIndex, Kind, Index);
                return;
            }

            if (DragRouter.SourceKind == DragSourceKind.Bank)
            {
                if (DungeonBlade.Bank.BankManager.Instance == null) return;
                DungeonBlade.Bank.BankManager.Instance.MoveToInventory(DragRouter.SourceIndex);
                return;
            }
        }
    }
}
