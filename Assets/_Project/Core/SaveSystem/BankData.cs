using System;
using System.Collections.Generic;
using DungeonBlade.Inventory;

namespace DungeonBlade.Core
{
    [Serializable]
    public class BankData
    {
        public int storedGold = 0;
        public int dungeonClearTokens = 0;
        public List<SerializedSlot> bankSlots = new List<SerializedSlot>();
    }
}
