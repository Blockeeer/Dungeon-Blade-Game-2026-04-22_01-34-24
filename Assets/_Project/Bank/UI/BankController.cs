using DungeonBlade.Core;
using UnityEngine;

namespace DungeonBlade.Bank.UI
{
    public class BankController : MonoBehaviour
    {
        public static BankController Instance { get; private set; }

        [SerializeField] GameObject bankPanel;

        bool _isOpen;
        public bool IsOpen => _isOpen;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            if (bankPanel != null) bankPanel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            if (bankPanel != null) bankPanel.SetActive(true);
            MenuState.Push();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (bankPanel != null) bankPanel.SetActive(false);
            MenuState.Pop();
            if (!MenuState.IsAnyOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
