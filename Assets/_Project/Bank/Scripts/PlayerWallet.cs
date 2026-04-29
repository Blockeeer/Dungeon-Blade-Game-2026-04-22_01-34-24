using System;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [SerializeField] int startingGold = 100;

        int _gold;

        public int Gold => _gold;
        public event Action<int> OnGoldChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _gold = Mathf.Max(0, startingGold);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (_gold < amount) return false;
            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
        }

        public void SetGold(int amount)
        {
            _gold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(_gold);
        }
    }
}
