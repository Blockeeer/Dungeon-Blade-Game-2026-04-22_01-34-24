using System.Collections.Generic;
using DungeonBlade.Inventory.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.Bank.UI
{
    public class BankUI : MonoBehaviour
    {
        [Header("Slot prefab")]
        [SerializeField] BankSlotWidget slotPrefab;

        [Header("Containers")]
        [SerializeField] RectTransform gridParent;

        [Header("Drag ghost")]
        [SerializeField] Image dragGhost;

        [Header("Tooltip (shared with inventory)")]
        [SerializeField] ItemTooltip tooltip;

        [Header("Gold UI")]
        [SerializeField] TMP_Text pocketGoldText;
        [SerializeField] TMP_Text vaultGoldText;
        [SerializeField] TMP_InputField depositInput;
        [SerializeField] TMP_InputField withdrawInput;

        public ItemTooltip Tooltip => tooltip;
        public BankSlotWidget DraggingFromBank { get; private set; }

        readonly List<BankSlotWidget> _slots = new List<BankSlotWidget>();

        void OnEnable()
        {
            if (BankManager.Instance != null) BankManager.Instance.OnBankChanged += RefreshAll;
            if (BankManager.Instance != null) BankManager.Instance.OnGoldChanged += _ => RefreshGold();
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnGoldChanged += _ => RefreshGold();
            if (Inventory.InventoryManager.Instance != null) Inventory.InventoryManager.Instance.OnInventoryChanged += RefreshAll;
            EnsureBuilt();
            RefreshAll();
            RefreshGold();
        }

        void OnDisable()
        {
            if (BankManager.Instance != null) BankManager.Instance.OnBankChanged -= RefreshAll;
            if (BankManager.Instance != null) BankManager.Instance.OnGoldChanged -= _ => RefreshGold();
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnGoldChanged -= _ => RefreshGold();
            if (Inventory.InventoryManager.Instance != null) Inventory.InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
            CancelDrag();
        }

        void EnsureBuilt()
        {
            if (slotPrefab == null || gridParent == null) return;
            if (_slots.Count > 0) return;

            for (int i = 0; i < BankManager.BankSize; i++)
            {
                var w = Instantiate(slotPrefab, gridParent);
                w.name = $"BankSlot_{i}";
                w.Bind(this, i);
                _slots.Add(w);
            }
        }

        void RefreshAll()
        {
            foreach (var s in _slots) s.Refresh();
        }

        void RefreshGold()
        {
            int pocket = PlayerWallet.Instance != null ? PlayerWallet.Instance.Gold : 0;
            int vault = BankManager.Instance != null ? BankManager.Instance.StoredGold : 0;
            if (pocketGoldText != null) pocketGoldText.text = $"Pocket: {pocket}g";
            if (vaultGoldText != null) vaultGoldText.text = $"Vault: {vault}g";
        }

        public void OnDepositPressed()
        {
            if (BankManager.Instance == null) return;
            int amount = ParseAmount(depositInput);
            BankManager.Instance.DepositGold(amount);
        }

        public void OnWithdrawPressed()
        {
            if (BankManager.Instance == null) return;
            int amount = ParseAmount(withdrawInput);
            BankManager.Instance.WithdrawGold(amount);
        }

        public void OnClosePressed()
        {
            if (BankController.Instance != null) BankController.Instance.Close();
        }

        int ParseAmount(TMP_InputField field)
        {
            if (field == null) return 0;
            int.TryParse(field.text, out int v);
            return Mathf.Max(0, v);
        }

        public void BeginDrag(BankSlotWidget from)
        {
            DraggingFromBank = from;
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
            DraggingFromBank = null;
        }

        public void CancelDrag() => EndDrag();
    }
}
