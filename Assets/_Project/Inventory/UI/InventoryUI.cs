using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.Inventory.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Slot prefab")]
        [SerializeField] SlotWidget slotPrefab;

        [Header("Containers (assign in Inspector)")]
        [SerializeField] RectTransform gridParent;
        [SerializeField] RectTransform hotbarParent;
        [SerializeField] RectTransform equipmentParent;

        [Header("Drag ghost")]
        [SerializeField] Image dragGhost;

        [Header("Tooltip")]
        [SerializeField] ItemTooltip tooltip;

        [Header("Player reference (for consumable use)")]
        [SerializeField] GameObject playerRef;

        public ItemTooltip Tooltip => tooltip;
        public GameObject PlayerRef => playerRef;
        public SlotWidget DraggingFrom { get; private set; }

        readonly List<SlotWidget> _gridSlots = new List<SlotWidget>();
        readonly List<SlotWidget> _hotbarSlots = new List<SlotWidget>();
        readonly List<SlotWidget> _equipmentSlots = new List<SlotWidget>();

        void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += RefreshAll;
            }
            EnsureBuilt();
            RefreshAll();
        }

        void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
            }
            CancelDrag();
        }

        void EnsureBuilt()
        {
            if (slotPrefab == null) return;
            if (_gridSlots.Count == 0 && gridParent != null)
            {
                for (int i = 0; i < InventoryManager.GridSize; i++)
                {
                    var w = Instantiate(slotPrefab, gridParent);
                    w.name = $"GridSlot_{i}";
                    w.Bind(this, SlotKind.Grid, i);
                    _gridSlots.Add(w);
                }
            }
            if (_hotbarSlots.Count == 0 && hotbarParent != null)
            {
                for (int i = 0; i < InventoryManager.HotbarSize; i++)
                {
                    var w = Instantiate(slotPrefab, hotbarParent);
                    w.name = $"HotbarSlot_{i}";
                    w.Bind(this, SlotKind.Hotbar, i);
                    _hotbarSlots.Add(w);
                }
            }
            if (_equipmentSlots.Count == 0 && equipmentParent != null)
            {
                for (int i = 0; i < InventoryManager.EquipmentSize; i++)
                {
                    var w = Instantiate(slotPrefab, equipmentParent);
                    w.name = $"EquipSlot_{(EquipmentSlot)(i + 1)}";
                    w.Bind(this, SlotKind.Equipment, i);
                    _equipmentSlots.Add(w);
                }
            }
        }

        public void RefreshAll()
        {
            foreach (var s in _gridSlots) s.Refresh();
            foreach (var s in _hotbarSlots) s.Refresh();
            foreach (var s in _equipmentSlots) s.Refresh();
        }

        public void BeginDrag(SlotWidget from)
        {
            DraggingFrom = from;
            if (dragGhost != null && from != null && !from.Data.IsEmpty)
            {
                dragGhost.gameObject.SetActive(true);
                dragGhost.sprite = from.Data.Item.Icon;
                dragGhost.enabled = from.Data.Item.Icon != null;
            }
        }

        public void UpdateDrag(Vector2 screenPos)
        {
            if (dragGhost == null) return;
            dragGhost.rectTransform.position = screenPos;
        }

        public void EndDrag()
        {
            if (dragGhost != null) dragGhost.gameObject.SetActive(false);
            DraggingFrom = null;
        }

        public void CancelDrag()
        {
            EndDrag();
        }
    }
}
