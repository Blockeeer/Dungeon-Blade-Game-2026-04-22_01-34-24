using DungeonBlade.Combat;
using DungeonBlade.Core;
using UnityEngine;

namespace DungeonBlade.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] WeaponBase[] weapons;
        [SerializeField] int startingWeaponIndex = 0;
        [SerializeField] ComboSystem comboSystem;
        [SerializeField] GameObject crosshair;

        PlayerInputActions _input;
        WeaponBase _active;
        int _activeIndex = -1;

        public WeaponBase Active => _active;

        void Start()
        {
            _input = InputManager.Instance != null ? InputManager.Instance.Actions : new PlayerInputActions();
            if (InputManager.Instance == null) _input.Enable();

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null) weapons[i].gameObject.SetActive(false);
            }

            HookComboCallbacks();
            EquipIndex(Mathf.Clamp(startingWeaponIndex, 0, weapons.Length - 1));
        }

        void HookComboCallbacks()
        {
            if (comboSystem == null) return;
            foreach (var w in weapons)
            {
                if (w is Sword sword)
                {
                    sword.OnHit += (_, __) => comboSystem.RegisterHit();
                }
                else if (w is Gun gun)
                {
                    gun.OnHitDamageable += () => comboSystem.RegisterHit();
                }
            }
        }

        void Update()
        {
            if (_active == null) return;

            _active.TickWeapon(Time.deltaTime);

            if (DungeonBlade.Core.MenuState.IsAnyOpen) return;

            if (_input.SwitchWeapon.WasPressedThisFrame()) CycleWeapon();

            if (_input.Fire.WasPressedThisFrame())
            {
                if (_active is Sword s && _input.AimOrBlock.IsPressed())
                {
                    s.StartHeavyHold();
                }
                else
                {
                    TryGunZCancel();
                    _active.OnPrimaryPressed();
                }
            }

            if (_input.Fire.WasReleasedThisFrame())
            {
                if (_active is Sword s) s.TryReleaseHeavy();
                _active.OnPrimaryReleased();
            }

            if (_input.AimOrBlock.WasPressedThisFrame()) _active.OnSecondaryPressed();
            if (_input.AimOrBlock.WasReleasedThisFrame()) _active.OnSecondaryReleased();
            if (_input.Reload.WasPressedThisFrame()) _active.OnReloadPressed();
        }

        void TryGunZCancel()
        {
            if (_active is Gun) return;

            foreach (var w in weapons)
            {
                if (w is Sword s && s.CanBeCancelled)
                {
                    s.CancelForGunshot();
                }
            }
        }

        public void CycleWeapon()
        {
            if (weapons.Length == 0) return;
            int next = (_activeIndex + 1) % weapons.Length;
            EquipIndex(next);
        }

        public void EquipIndex(int index)
        {
            if (index < 0 || index >= weapons.Length || weapons[index] == null) return;
            if (_activeIndex == index) return;

            if (_active != null) _active.OnUnequip();
            _activeIndex = index;
            _active = weapons[index];
            _active.OnEquip();

            if (crosshair != null) crosshair.SetActive(true);
        }
    }
}
