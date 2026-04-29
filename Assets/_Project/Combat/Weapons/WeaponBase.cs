using UnityEngine;

namespace DungeonBlade.Combat
{
    public enum WeaponKind { Melee, Ranged }

    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] string displayName = "Weapon";
        [SerializeField] WeaponKind kind = WeaponKind.Melee;

        public string DisplayName => displayName;
        public WeaponKind Kind => kind;
        public bool IsEquipped { get; private set; }

        public virtual void OnEquip()
        {
            IsEquipped = true;
            gameObject.SetActive(true);
        }

        public virtual void OnUnequip()
        {
            IsEquipped = false;
            gameObject.SetActive(false);
        }

        public abstract void OnPrimaryPressed();
        public abstract void OnPrimaryReleased();
        public abstract void OnSecondaryPressed();
        public abstract void OnSecondaryReleased();
        public virtual void OnReloadPressed() { }
        public virtual void TickWeapon(float dt) { }

        public virtual bool IsBusy => false;
        public virtual bool CanBeCancelled => true;
    }
}
