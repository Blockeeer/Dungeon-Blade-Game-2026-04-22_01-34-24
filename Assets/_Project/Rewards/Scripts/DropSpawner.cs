using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Rewards
{
    public static class DropSpawner
    {
        public static GameObject ItemPickupPrefab;
        public static GameObject GoldPickupPrefab;

        public static void SpawnLoot(LootTable table, Vector3 origin, float scatterRadius = 1.5f)
        {
            if (table == null) return;

            var rolls = table.RollItems();
            foreach (var (item, qty) in rolls)
            {
                SpawnItem(item, qty, origin, scatterRadius);
            }

            int gold = table.RollGold();
            if (gold > 0) SpawnGold(gold, origin, scatterRadius);

            int exp = table.Experience;
            if (exp > 0 && ExperienceSystem.Instance != null)
            {
                ExperienceSystem.Instance.GrantExperience(exp);
            }
        }

        public static void SpawnItem(Item item, int quantity, Vector3 origin, float scatterRadius = 1.5f)
        {
            if (item == null || ItemPickupPrefab == null) return;
            Vector3 pos = origin + RandomScatter(scatterRadius);
            var go = Object.Instantiate(ItemPickupPrefab, pos, Quaternion.identity);
            var pickup = go.GetComponent<ItemPickup>();
            if (pickup != null) pickup.Initialize(item, quantity);
        }

        public static void SpawnGold(int amount, Vector3 origin, float scatterRadius = 1.5f)
        {
            if (amount <= 0 || GoldPickupPrefab == null) return;
            Vector3 pos = origin + RandomScatter(scatterRadius);
            var go = Object.Instantiate(GoldPickupPrefab, pos, Quaternion.identity);
            var pickup = go.GetComponent<GoldPickup>();
            if (pickup != null) pickup.Initialize(amount);
        }

        static Vector3 RandomScatter(float radius)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            return new Vector3(r.x, 0.6f, r.y);
        }
    }

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
