using DungeonBlade.Combat;
using DungeonBlade.Player;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    public class EquipmentBinder : MonoBehaviour
    {
        [SerializeField] PlayerCombat playerCombat;
        [SerializeField] Sword mainHandSword;
        [SerializeField] Gun offHandGun;

        void Start()
        {
            if (InventoryManager.Instance == null) return;
            InventoryManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
            ApplyAll();
        }

        void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
        }

        void OnEquipmentChanged(EquipmentSlot slot, Item prev, Item next)
        {
            ApplyAll();
        }

        void ApplyAll()
        {
            var inv = InventoryManager.Instance;
            if (inv == null) return;

            bool hasMain = inv.GetEquipped(EquipmentSlot.MainHand) != null;
            bool hasOff = inv.GetEquipped(EquipmentSlot.OffHand) != null;

            if (mainHandSword != null) mainHandSword.gameObject.SetActive(hasMain);
            if (offHandGun != null) offHandGun.gameObject.SetActive(hasOff);

            Debug.Log($"[Equipment] MainHand={(hasMain ? "yes" : "no")}, OffHand={(hasOff ? "yes" : "no")}");
        }
    }
}
