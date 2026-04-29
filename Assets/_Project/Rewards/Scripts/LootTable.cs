using System;
using System.Collections.Generic;
using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Rewards
{
    [CreateAssetMenu(menuName = "DungeonBlade/Loot Table", fileName = "LootTable")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public struct LootRoll
        {
            public Item Item;
            [Range(0f, 1f)] public float DropChance;
            [Min(1)] public int MinQuantity;
            [Min(1)] public int MaxQuantity;
        }

        [Header("Item Rolls")]
        [Tooltip("Each row is rolled independently against its DropChance.")]
        [SerializeField] List<LootRoll> rolls = new List<LootRoll>();

        [Header("Gold")]
        [SerializeField] int minGold = 0;
        [SerializeField] int maxGold = 0;

        [Header("Experience")]
        [Min(0)] [SerializeField] int experience = 0;

        public IReadOnlyList<LootRoll> Rolls => rolls;
        public int MinGold => minGold;
        public int MaxGold => maxGold;
        public int Experience => experience;

        public int RollGold()
        {
            return UnityEngine.Random.Range(minGold, maxGold + 1);
        }

        public List<(Item item, int qty)> RollItems()
        {
            var result = new List<(Item, int)>(rolls.Count);
            foreach (var r in rolls)
            {
                if (r.Item == null) continue;
                if (UnityEngine.Random.value > r.DropChance) continue;
                int qty = UnityEngine.Random.Range(Mathf.Max(1, r.MinQuantity), Mathf.Max(r.MinQuantity, r.MaxQuantity) + 1);
                result.Add((r.Item, qty));
            }
            return result;
        }
    }
}
