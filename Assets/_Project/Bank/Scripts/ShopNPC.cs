using DungeonBlade.Bank.UI;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class ShopNPC : Interactable
    {
        [SerializeField] ShopDefinition shop;

        public override void OnInteract(GameObject player)
        {
            if (shop == null)
            {
                Debug.LogWarning("[ShopNPC] No ShopDefinition assigned.");
                return;
            }
            if (ShopController.Instance == null || ShopManager.Instance == null)
            {
                Debug.LogWarning("[ShopNPC] No ShopController or ShopManager in scene.");
                return;
            }

            ShopManager.Instance.OpenShop(shop);
            ShopController.Instance.Open();
        }
    }
}
