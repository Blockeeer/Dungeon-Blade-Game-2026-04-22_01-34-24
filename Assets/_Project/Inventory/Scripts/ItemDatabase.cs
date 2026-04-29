using System.Collections.Generic;
using UnityEngine;

namespace DungeonBlade.Inventory
{
    [CreateAssetMenu(menuName = "DungeonBlade/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] List<Item> items = new List<Item>();

        Dictionary<string, Item> _lookup;

        public IReadOnlyList<Item> Items => items;

        public Item Find(string id)
        {
            EnsureLookup();
            return _lookup.TryGetValue(id, out var item) ? item : null;
        }

        void EnsureLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, Item>(items.Count);
            foreach (var it in items)
            {
                if (it == null || string.IsNullOrEmpty(it.ItemId)) continue;
                if (!_lookup.ContainsKey(it.ItemId)) _lookup.Add(it.ItemId, it);
                else Debug.LogWarning($"[ItemDatabase] Duplicate id '{it.ItemId}' on {it.name}");
            }
        }

        public void RebuildLookup() => _lookup = null;
    }
}
