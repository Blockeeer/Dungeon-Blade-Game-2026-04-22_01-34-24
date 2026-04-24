using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonBlade.Bank;
using DungeonBlade.Items;

namespace DungeonBlade.UI
{
    /// <summary>
    /// Bank / shop UI per GDD Section 8.
    /// Tabs: Vault (deposit/withdraw), Shop (buy), Sell, Token Exchange.
    /// Hook a BankNPC interact trigger to call Show(bankSystem).
    /// </summary>
    public class BankUI : MonoBehaviour
    {
        public enum Tab { Vault, Shop, Sell, Tokens }

        [Header("Refs")]
        public BankSystem bankSystem;
        public GameObject rootPanel;

        [Header("Tabs")]
        public Button vaultTab, shopTab, sellTab, tokenTab;
        public GameObject vaultPanel, shopPanel, sellPanel, tokenPanel;

        [Header("Row Prefab")]
        public GameObject shopRowPrefab;
        public Transform vaultParent;
        public Transform shopParent;
        public Transform sellParent;
        public Transform tokenParent;

        [Header("Status")]
        public TMP_Text goldText;
        public TMP_Text tokenText;
        public TMP_Text statusText;

        Tab currentTab = Tab.Shop;

        void Awake()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            if (vaultTab != null) vaultTab.onClick.AddListener(() => SwitchTab(Tab.Vault));
            if (shopTab != null)  shopTab.onClick.AddListener(()  => SwitchTab(Tab.Shop));
            if (sellTab != null)  sellTab.onClick.AddListener(()  => SwitchTab(Tab.Sell));
            if (tokenTab != null) tokenTab.onClick.AddListener(() => SwitchTab(Tab.Tokens));
        }

        public void Show(BankSystem bank)
        {
            bankSystem = bank;
            if (rootPanel != null) rootPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SwitchTab(Tab.Shop);
            UpdateStatus();
        }

        public void Hide()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (rootPanel != null && rootPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        void SwitchTab(Tab t)
        {
            currentTab = t;
            if (vaultPanel != null) vaultPanel.SetActive(t == Tab.Vault);
            if (shopPanel != null) shopPanel.SetActive(t == Tab.Shop);
            if (sellPanel != null) sellPanel.SetActive(t == Tab.Sell);
            if (tokenPanel != null) tokenPanel.SetActive(t == Tab.Tokens);

            switch (t)
            {
                case Tab.Vault:  BuildVault();  break;
                case Tab.Shop:   BuildShop();   break;
                case Tab.Sell:   BuildSell();   break;
                case Tab.Tokens: BuildTokens(); break;
            }
        }

        void BuildVault()
        {
            if (vaultParent == null || shopRowPrefab == null || bankSystem == null) return;
            ClearChildren(vaultParent);
            for (int i = 0; i < bankSystem.vault.Length; i++)
            {
                var slot = bankSystem.vault[i];
                if (slot.IsEmpty) continue;
                int idx = i;  // capture
                var row = Instantiate(shopRowPrefab, vaultParent);
                var ui = row.GetComponent<BankRowUI>() ?? row.AddComponent<BankRowUI>();
                ui.Setup(slot.item, slot.quantity, "Withdraw", () =>
                {
                    bankSystem.WithdrawToInventory(idx, 1);
                    BuildVault();
                    UpdateStatus();
                });
            }
        }

        void BuildShop()
        {
            if (shopParent == null || shopRowPrefab == null || bankSystem == null) return;
            ClearChildren(shopParent);
            foreach (var entry in bankSystem.shopItems)
            {
                if (entry?.item == null) continue;
                var row = Instantiate(shopRowPrefab, shopParent);
                var ui = row.GetComponent<BankRowUI>() ?? row.AddComponent<BankRowUI>();
                int unit = entry.priceOverride > 0 ? entry.priceOverride : entry.item.baseValue;
                ui.Setup(entry.item, 1, $"Buy ({unit}g)", () =>
                {
                    if (bankSystem.Buy(entry, 1)) { SetStatus($"Bought {entry.item.displayName}"); UpdateStatus(); }
                    else SetStatus("Not enough gold.");
                });
            }
        }

        void BuildSell()
        {
            if (sellParent == null || shopRowPrefab == null || bankSystem == null) return;
            ClearChildren(sellParent);
            var inv = bankSystem.playerInventory;
            if (inv == null) return;
            for (int i = 0; i < inv.mainGrid.Length; i++)
            {
                var slot = inv.mainGrid[i];
                if (slot.IsEmpty) continue;
                var item = slot.item;
                int qty = slot.quantity;
                var row = Instantiate(shopRowPrefab, sellParent);
                var ui = row.GetComponent<BankRowUI>() ?? row.AddComponent<BankRowUI>();
                ui.Setup(item, qty, $"Sell ({item.SellPrice}g)", () =>
                {
                    if (bankSystem.Sell(item, 1)) { SetStatus($"Sold {item.displayName}"); UpdateStatus(); BuildSell(); }
                });
            }
        }

        void BuildTokens()
        {
            if (tokenParent == null || shopRowPrefab == null || bankSystem == null) return;
            ClearChildren(tokenParent);
            foreach (var entry in bankSystem.tokenShopItems)
            {
                if (entry?.item == null) continue;
                var row = Instantiate(shopRowPrefab, tokenParent);
                var ui = row.GetComponent<BankRowUI>() ?? row.AddComponent<BankRowUI>();
                int cost = entry.priceOverride > 0 ? entry.priceOverride : 1;
                ui.Setup(entry.item, 1, $"Redeem ({cost}t)", () =>
                {
                    if (bankSystem.RedeemToken(entry, 1)) { SetStatus($"Redeemed {entry.item.displayName}"); UpdateStatus(); }
                    else SetStatus("Not enough tokens.");
                });
            }
        }

        void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        void UpdateStatus()
        {
            var inv = bankSystem?.playerInventory;
            if (inv == null) return;
            if (goldText != null) goldText.text = $"Gold: {inv.gold:N0}";
            if (tokenText != null) tokenText.text = $"Tokens: {inv.dungeonClearTokens}";
        }

        void SetStatus(string s) { if (statusText != null) statusText.text = s; }
    }

    /// <summary>Single row in a bank tab — icon, name, qty/price, action button.</summary>
    public class BankRowUI : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text qtyText;
        public Button actionButton;
        public TMP_Text actionText;

        public void Setup(ItemData item, int qty, string actionLabel, System.Action onClick)
        {
            if (icon == null || nameText == null || actionButton == null)
            {
                // Auto-find
                foreach (var c in GetComponentsInChildren<Image>(true))
                    if (c.gameObject != gameObject && icon == null) icon = c;
                var texts = GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length > 0) nameText = texts[0];
                if (texts.Length > 1) qtyText = texts[1];
                actionButton = GetComponentInChildren<Button>(true);
                actionText = actionButton != null ? actionButton.GetComponentInChildren<TMP_Text>(true) : null;
            }

            if (icon != null)      { icon.sprite = item.icon; icon.enabled = item.icon != null; }
            if (nameText != null)  { nameText.text = item.displayName; nameText.color = ItemData.RarityColor(item.rarity); }
            if (qtyText != null)   qtyText.text = qty > 1 ? $"x{qty}" : "";
            if (actionText != null) actionText.text = actionLabel;
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }

    /// <summary>Attach to Bank NPC. Press F within range to open bank.</summary>
    public class BankNPC : MonoBehaviour
    {
        public BankUI bankUI;
        public BankSystem bankSystem;
        public float interactRange = 3f;
        public GameObject promptUI;   // "Press E to interact"
        public KeyCode interactKey = KeyCode.E;

        bool warnedMissingUI;

        void Update()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            float d = Vector3.Distance(transform.position, player.transform.position);
            bool inRange = d <= interactRange;
            if (promptUI != null) promptUI.SetActive(inRange);

            if (inRange && Input.GetKeyDown(interactKey))
            {
                if (bankUI != null)
                {
                    Debug.Log($"[BankNPC] Opening bank UI...");
                    bankUI.Show(bankSystem);
                }
                else if (!warnedMissingUI)
                {
                    Debug.LogError($"[BankNPC] interactKey pressed in range, but bankUI reference is null! Scene builder didn't wire it. Run DungeonBlade → Build Everything.");
                    warnedMissingUI = true;
                }
            }
        }
    }
}
