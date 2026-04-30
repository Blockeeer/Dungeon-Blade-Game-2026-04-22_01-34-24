using UnityEngine;
using UnityEngine.Events;

namespace DungeonBlade.Progression
{
    /// <summary>
    /// XP and leveling per GDD 9.1.
    /// - Level cap 20 (Phase 1)
    /// - Each level: +5 max HP, +2 max stamina, +1 skill point
    /// </summary>
    public class PlayerProgression : MonoBehaviour
    {
        [Header("Level")]
        public int level = 1;
        public int levelCap = 20;
        public int currentXp;
        public int skillPoints;

        [Header("Per-Level Gains")]
        public float hpPerLevel = 5f;
        public float staminaPerLevel = 2f;

        [Header("Links")]
        public DungeonBlade.Player.PlayerStats playerStats;

        [Header("Events")]
        public UnityEvent<int> OnXpChanged;            // currentXp
        public UnityEvent<int, int> OnLevelChanged;    // (newLevel, skillPoints)
        public UnityEvent<int> OnXpToNextChanged;      // xp needed for next level

        // Simple curve: nextXp = round( 100 * level^1.35 )
        public int XpForLevel(int lvl) => Mathf.RoundToInt(100f * Mathf.Pow(lvl, 1.35f));
        public int XpToNext => level >= levelCap ? 0 : XpForLevel(level);

        void Start()
        {
            OnXpChanged?.Invoke(currentXp);
            OnLevelChanged?.Invoke(level, skillPoints);
            OnXpToNextChanged?.Invoke(XpToNext);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            currentXp += amount;
            OnXpChanged?.Invoke(currentXp);

            while (level < levelCap && currentXp >= XpForLevel(level))
            {
                currentXp -= XpForLevel(level);
                LevelUp();
            }
            OnXpToNextChanged?.Invoke(XpToNext);
        }

        void LevelUp()
        {
            level++;
            skillPoints++;
            if (playerStats != null)
            {
                playerStats.maxHealth  += hpPerLevel;
                playerStats.maxStamina += staminaPerLevel;
            }
            OnLevelChanged?.Invoke(level, skillPoints);
        }

        public void LoadFromSave(int lvl, int xp, int sp)
        {
            level = Mathf.Clamp(lvl, 1, levelCap);
            currentXp = Mathf.Max(0, xp);
            skillPoints = Mathf.Max(0, sp);
            OnXpChanged?.Invoke(currentXp);
            OnLevelChanged?.Invoke(level, skillPoints);
            OnXpToNextChanged?.Invoke(XpToNext);
        }
    }
}
