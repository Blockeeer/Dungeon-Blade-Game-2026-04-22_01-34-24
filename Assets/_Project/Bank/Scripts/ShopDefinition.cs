using System;
using System.Collections.Generic;
using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Bank
{
    [CreateAssetMenu(menuName = "DungeonBlade/Shop Definition", fileName = "ShopDefinition")]
    public class ShopDefinition : ScriptableObject
    {
        [Serializable]
        public struct Stock
        {
            public Item Item;
            [Min(1)] public int Quantity;
            [Tooltip("Override the buy price. Leave 0 to use Item.BuyValue.")]
            public int PriceOverride;
        }

        [SerializeField] string shopName = "Shop";
        [SerializeField] [Range(0f, 1f)] float sellPriceMultiplier = 1f;
        [SerializeField] List<Stock> stockList = new List<Stock>();

        public string ShopName => shopName;
        public float SellPriceMultiplier => sellPriceMultiplier;
        public IReadOnlyList<Stock> StockList => stockList;

        public int BuyPriceFor(int stockIndex)
        {
            if (stockIndex < 0 || stockIndex >= stockList.Count) return 0;
            var s = stockList[stockIndex];
            if (s.Item == null) return 0;
            return s.PriceOverride > 0 ? s.PriceOverride : s.Item.BuyValue;
        }

        public int SellPriceFor(Item item)
        {
            if (item == null) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(item.SellValue * sellPriceMultiplier));
        }
    }
}
