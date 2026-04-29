using System;
using System.Collections.Generic;
using DungeonBlade.Inventory;

namespace DungeonBlade.Core
{
    [Serializable]
    public class PlayerProfile
    {
        public string playerName = "Hero";
        public int level = 1;
        public int experience = 0;
        public int gold = 0;
        public List<string> ownedItemIds = new List<string>();
        public List<SerializedSlot> inventory = new List<SerializedSlot>();
    }
}
