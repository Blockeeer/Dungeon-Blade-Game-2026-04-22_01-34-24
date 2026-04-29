using DungeonBlade.Inventory.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonBlade.Bank.UI
{
    [RequireComponent(typeof(Image))]
    public class ShopSellZone : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData e)
        {
            if (DragRouter.SourceKind != DragSourceKind.Inventory) return;
            if (ShopManager.Instance == null) return;

            ShopManager.Instance.TrySell(DragRouter.InventorySourceKind, DragRouter.SourceIndex);
        }
    }
}
