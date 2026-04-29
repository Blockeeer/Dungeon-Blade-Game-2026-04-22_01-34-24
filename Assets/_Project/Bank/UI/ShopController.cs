using DungeonBlade.Core;
using UnityEngine;

namespace DungeonBlade.Bank.UI
{
    public class ShopController : MonoBehaviour
    {
        public static ShopController Instance { get; private set; }

        [SerializeField] GameObject shopPanel;

        bool _isOpen;
        public bool IsOpen => _isOpen;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            if (shopPanel != null) shopPanel.SetActive(true);
            MenuState.Push();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (shopPanel != null) shopPanel.SetActive(false);
            if (ShopManager.Instance != null) ShopManager.Instance.CloseShop();
            MenuState.Pop();
            if (!MenuState.IsAnyOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
