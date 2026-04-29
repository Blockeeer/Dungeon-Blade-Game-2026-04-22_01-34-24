using DungeonBlade.Core;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        public static InventoryController Instance { get; private set; }

        [SerializeField] GameObject inventoryPanel;

        PlayerInputActions _input;
        bool _isOpen;

        public bool IsOpen => _isOpen;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            _input = InputManager.Instance != null ? InputManager.Instance.Actions : new PlayerInputActions();
            if (InputManager.Instance == null) _input.Enable();

            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }

        void Update()
        {
            if (_input.OpenInventory.WasPressedThisFrame()) Toggle();
            else if (_isOpen && _input.Pause.WasPressedThisFrame()) Close();
        }

        public void Toggle()
        {
            if (_isOpen) Close(); else Open();
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            Core.MenuState.Push();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            Core.MenuState.Pop();
            if (!Core.MenuState.IsAnyOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
