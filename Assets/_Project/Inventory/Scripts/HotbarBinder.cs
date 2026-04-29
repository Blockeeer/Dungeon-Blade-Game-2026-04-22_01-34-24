using DungeonBlade.Core;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    public class HotbarBinder : MonoBehaviour
    {
        [SerializeField] GameObject playerRef;

        PlayerInputActions _input;

        void Start()
        {
            _input = InputManager.Instance != null ? InputManager.Instance.Actions : new PlayerInputActions();
            if (InputManager.Instance == null) _input.Enable();
        }

        void Update()
        {
            if (InventoryManager.Instance == null || playerRef == null) return;
            if (Core.MenuState.IsAnyOpen) return;

            if (_input.Hotbar1.WasPressedThisFrame()) UseHotbar(0);
            if (_input.Hotbar2.WasPressedThisFrame()) UseHotbar(1);
            if (_input.Hotbar3.WasPressedThisFrame()) UseHotbar(2);
            if (_input.Hotbar4.WasPressedThisFrame()) UseHotbar(3);
            if (_input.Hotbar5.WasPressedThisFrame()) UseHotbar(4);
            if (_input.Hotbar6.WasPressedThisFrame()) UseHotbar(5);
        }

        void UseHotbar(int idx)
        {
            InventoryManager.Instance.UseSlot(SlotKind.Hotbar, idx, playerRef);
        }
    }
}
