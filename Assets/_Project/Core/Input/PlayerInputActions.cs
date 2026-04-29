using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonBlade.Core
{
    public class PlayerInputActions
    {
        public readonly InputActionMap Map;

        public readonly InputAction Move;
        public readonly InputAction Look;
        public readonly InputAction Jump;
        public readonly InputAction Dash;
        public readonly InputAction Sprint;
        public readonly InputAction Crouch;
        public readonly InputAction Fire;
        public readonly InputAction AimOrBlock;
        public readonly InputAction Reload;
        public readonly InputAction SwitchWeapon;
        public readonly InputAction Interact;
        public readonly InputAction OpenInventory;
        public readonly InputAction Pause;
        public readonly InputAction Skill1;
        public readonly InputAction Skill2;
        public readonly InputAction Skill3;
        public readonly InputAction Hotbar1;
        public readonly InputAction Hotbar2;
        public readonly InputAction Hotbar3;
        public readonly InputAction Hotbar4;
        public readonly InputAction Hotbar5;
        public readonly InputAction Hotbar6;

        public PlayerInputActions()
        {
            Map = new InputActionMap("Player");

            Move = Map.AddAction("Move", InputActionType.Value);
            Move.expectedControlType = "Vector2";
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            Look = Map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            Look.expectedControlType = "Vector2";

            Jump = Map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Dash = Map.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            Sprint = Map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftCtrl");
            Crouch = Map.AddAction("Crouch", InputActionType.Button, "<Keyboard>/c");

            Fire = Map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            AimOrBlock = Map.AddAction("AimOrBlock", InputActionType.Button, "<Mouse>/rightButton");
            Reload = Map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r");
            SwitchWeapon = Map.AddAction("SwitchWeapon", InputActionType.Button, "<Keyboard>/q");

            Interact = Map.AddAction("Interact", InputActionType.Button, "<Keyboard>/f");
            OpenInventory = Map.AddAction("OpenInventory", InputActionType.Button, "<Keyboard>/tab");
            Pause = Map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");

            Skill1 = Map.AddAction("Skill1", InputActionType.Button, "<Keyboard>/e");
            Skill2 = Map.AddAction("Skill2", InputActionType.Button, "<Keyboard>/g");
            Skill3 = Map.AddAction("Skill3", InputActionType.Button, "<Keyboard>/v");

            Hotbar1 = Map.AddAction("Hotbar1", InputActionType.Button, "<Keyboard>/1");
            Hotbar2 = Map.AddAction("Hotbar2", InputActionType.Button, "<Keyboard>/2");
            Hotbar3 = Map.AddAction("Hotbar3", InputActionType.Button, "<Keyboard>/3");
            Hotbar4 = Map.AddAction("Hotbar4", InputActionType.Button, "<Keyboard>/4");
            Hotbar5 = Map.AddAction("Hotbar5", InputActionType.Button, "<Keyboard>/5");
            Hotbar6 = Map.AddAction("Hotbar6", InputActionType.Button, "<Keyboard>/6");
        }

        public void Enable() => Map.Enable();
        public void Disable() => Map.Disable();
    }
}
