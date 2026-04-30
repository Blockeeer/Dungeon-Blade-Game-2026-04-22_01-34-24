using UnityEngine;

namespace DungeonBlade.Rewards
{
    public class DropPrefabBindings : MonoBehaviour
    {
        [SerializeField] GameObject itemPickupPrefab;
        [SerializeField] GameObject goldPickupPrefab;

        void Awake()
        {
            DropSpawner.ItemPickupPrefab = itemPickupPrefab;
            DropSpawner.GoldPickupPrefab = goldPickupPrefab;
        }
    }
}
