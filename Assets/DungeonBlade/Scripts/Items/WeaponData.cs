using UnityEngine;

namespace DungeonBlade.Items
{
    public enum WeaponType { Sword, Pistol, Rifle, Shotgun, GrenadeLauncher }

    [CreateAssetMenu(menuName = "DungeonBlade/Weapon", fileName = "NewWeapon")]
    public class WeaponData : ItemData
    {
        [Header("Weapon")]
        public WeaponType weaponType = WeaponType.Sword;

        [Header("Melee Stats (Sword)")]
        public float meleeDamage = 20f;
        public float attackSpeedMult = 1f;
        public float durability = 100f;

        [Header("Ranged Stats (Gun)")]
        public float rangedDamage = 25f;
        public float fireRate = 8f;
        public int magSize = 12;
        public float reloadTime = 1.4f;
        public float recoil = 1f;
        public float spreadHip = 2f;
        public float spreadAds = 0.4f;

        [Header("Special Property (Epic+)")]
        public bool hasSpecialProperty;
        public string specialPropertyDescription;
        public float specialPropertyValue;  // e.g. 0.1 for 10% lifesteal

        void OnEnable()
        {
            category = ItemCategory.Weapon;
        }
    }
}
