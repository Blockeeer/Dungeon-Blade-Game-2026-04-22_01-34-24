using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    [CreateAssetMenu(menuName = "DungeonBlade/Item/Weapon Item", fileName = "NewWeapon")]
    public class WeaponItem : Item
    {
        [Header("Weapon")]
        [SerializeField] WeaponBase weaponPrefab;
        [SerializeField] WeaponKind weaponKind = WeaponKind.Melee;
        [SerializeField] float damageBonus = 0f;

        public WeaponBase WeaponPrefab => weaponPrefab;
        public WeaponKind WeaponKind => weaponKind;
        public float DamageBonus => damageBonus;
    }
}
