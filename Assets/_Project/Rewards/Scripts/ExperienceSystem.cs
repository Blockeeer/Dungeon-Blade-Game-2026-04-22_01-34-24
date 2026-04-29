using System;
using DungeonBlade.Player;
using UnityEngine;

namespace DungeonBlade.Rewards
{
    public class ExperienceSystem : MonoBehaviour
    {
        public static ExperienceSystem Instance { get; private set; }

        public const int LevelCap = 10;
        public const int BaseExpPerLevel = 100;

        [SerializeField] PlayerStats playerStats;
        [SerializeField] int level = 1;
        [SerializeField] int experience = 0;

        [Header("Per-Level Bonus")]
        [SerializeField] float maxHpPerLevel = 10f;
        [SerializeField] float maxStaminaPerLevel = 5f;

        public int Level => level;
        public int Experience => experience;
        public int ExperienceForNextLevel => ExperienceRequired(level);

        public event Action<int> OnLevelUp;
        public event Action<int, int> OnExperienceChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static int ExperienceRequired(int currentLevel)
        {
            if (currentLevel >= LevelCap) return int.MaxValue;
            return BaseExpPerLevel * (int)Mathf.Pow(2, currentLevel - 1);
        }

        public void GrantExperience(int amount)
        {
            if (amount <= 0 || level >= LevelCap) return;

            experience += amount;
            Debug.Log($"[EXP] +{amount} EXP. ({experience}/{ExperienceForNextLevel})");

            while (level < LevelCap && experience >= ExperienceForNextLevel)
            {
                experience -= ExperienceForNextLevel;
                LevelUp();
            }

            if (level >= LevelCap) experience = 0;

            OnExperienceChanged?.Invoke(experience, ExperienceForNextLevel);
        }

        void LevelUp()
        {
            level++;
            Debug.Log($"[Level] LEVEL UP! Now level {level}.");

            if (playerStats != null)
            {
                var so = new SerializedStatBoost
                {
                    maxHpDelta = maxHpPerLevel,
                    maxStaminaDelta = maxStaminaPerLevel,
                };
                ApplyStatBoost(so);
            }

            OnLevelUp?.Invoke(level);
        }

        struct SerializedStatBoost
        {
            public float maxHpDelta;
            public float maxStaminaDelta;
        }

        void ApplyStatBoost(SerializedStatBoost boost)
        {
            if (playerStats == null) return;
            playerStats.AddMaxHealth(boost.maxHpDelta, healToFull: false);
            playerStats.AddMaxStamina(boost.maxStaminaDelta, refillToFull: false);
        }

        public void SetState(int newLevel, int newExperience)
        {
            level = Mathf.Clamp(newLevel, 1, LevelCap);
            experience = Mathf.Max(0, newExperience);
            OnExperienceChanged?.Invoke(experience, ExperienceForNextLevel);
        }
    }
}
