using System.Collections.Generic;
using UnityEngine;
using DungeonBlade.Items;

namespace DungeonBlade.Loot
{
    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0f, 1f)] public float chance = 0.5f;
        public bool guaranteed = false;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    [System.Serializable]
    public class WeightedLootPool
    {
        public LootEntry entry;
        public float weight = 1f;
    }

    /// <summary>
    /// Loot table per GDD 6.2.
    /// Supports: guaranteed drops, independent-chance rolls, weighted-pool rolls (pick 1 of N).
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonBlade/Loot Table", fileName = "NewLootTable")]
    public class LootTable : ScriptableObject
    {
        [Header("Always Drop")]
        public List<LootEntry> guaranteedDrops = new List<LootEntry>();

        [Header("Independent Rolls (each rolled separately)")]
        public List<LootEntry> independentRolls = new List<LootEntry>();

        [Header("Pick-One Weighted Pool (optional)")]
        public List<WeightedLootPool> weightedPool = new List<WeightedLootPool>();
        public bool rollWeightedPool = false;

        [Header("Gold Bonus")]
        public int guaranteedGoldMin = 0;
        public int guaranteedGoldMax = 0;

        [Header("Dungeon Clear Token")]
        public int dungeonTokenAmount = 0;

        /// <summary>Rolls all drop categories and returns the list of items to award.</summary>
        public List<ItemData> Roll()
        {
            var result = new List<ItemData>();

            foreach (var e in guaranteedDrops)
            {
                if (e?.item == null) continue;
                int q = Random.Range(e.minQuantity, e.maxQuantity + 1);
                for (int i = 0; i < q; i++) result.Add(e.item);
            }

            foreach (var e in independentRolls)
            {
                if (e?.item == null) continue;
                if (Random.value <= e.chance)
                {
                    int q = Random.Range(e.minQuantity, e.maxQuantity + 1);
                    for (int i = 0; i < q; i++) result.Add(e.item);
                }
            }

            if (rollWeightedPool && weightedPool.Count > 0)
            {
                float total = 0f;
                foreach (var w in weightedPool) total += Mathf.Max(0f, w.weight);
                if (total > 0f)
                {
                    float r = Random.value * total;
                    float acc = 0f;
                    foreach (var w in weightedPool)
                    {
                        acc += Mathf.Max(0f, w.weight);
                        if (r <= acc)
                        {
                            if (w.entry?.item != null)
                            {
                                int q = Random.Range(w.entry.minQuantity, w.entry.maxQuantity + 1);
                                for (int i = 0; i < q; i++) result.Add(w.entry.item);
                            }
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public int RollGold() => guaranteedGoldMax > 0 ? Random.Range(guaranteedGoldMin, guaranteedGoldMax + 1) : 0;
        public int RollTokens() => dungeonTokenAmount;
    }
}
