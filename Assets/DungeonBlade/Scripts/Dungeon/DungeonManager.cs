using System;
using UnityEngine;
using UnityEngine.Events;

namespace DungeonBlade.Dungeon
{
    /// <summary>
    /// Per-run state: current checkpoint, respawns used, kill/combo stats.
    /// Handles player death → respawn flow, wave/zone progression,
    /// and dungeon-clear summary per GDD 11.2.
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        [Header("Refs")]
        public DungeonBlade.Player.PlayerStats playerStats;
        public DungeonBlade.Combat.ComboCounter comboCounter;

        [Header("Spawn")]
        public Transform defaultSpawnPoint;

        [Header("First Clear Bonus (GDD 9.2)")]
        public int firstClearBonusXp = 100;
        public int dungeonClearXp = 200;
        public string dungeonIdForFirstClear = "dungeon_1_forsaken_keep";

        [Header("Events")]
        public UnityEvent OnRunStarted;
        public UnityEvent OnPlayerDied;
        public UnityEvent OnPlayerRespawned;
        public UnityEvent OnDungeonClear;            // boss dead + exit reached
        public UnityEvent<DungeonClearSummary> OnDungeonClearSummary;

        [Serializable]
        public class DungeonClearSummary
        {
            public float runTimeSeconds;
            public int kills;
            public int maxCombo;
            public int xpGained;
            public int goldGained;
            public bool wasFirstClear;
        }

        Checkpoint lastCheckpoint;
        float runStartTime;
        int startingXp;
        int startingGold;
        int killsThisRun;
        bool bossDefeated;

        void Awake()
        {
            TryHookDeathListener();
        }

        void Start()
        {
            // Try again in Start — some scene builders wire playerStats AFTER Awake runs
            TryHookDeathListener();
        }

        void Update()
        {
            // Player GameObject may not exist yet at Start time (e.g. Player is spawned
            // by a separate bootstrap AFTER DungeonManager Awake/Start). Keep trying until
            // we find it.
            if (!hasHookedDeath) TryHookDeathListener();
        }

        void TryHookDeathListener()
        {
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<DungeonBlade.Player.PlayerStats>();
            }
            if (playerStats != null && !hasHookedDeath)
            {
                playerStats.OnDeath.AddListener(HandlePlayerDeath);
                hasHookedDeath = true;
                Debug.Log($"[DungeonManager] Hooked death listener to PlayerStats.");
            }
        }

        bool hasHookedDeath;

        public void StartNewRun()
        {
            runStartTime = Time.time;
            lastCheckpoint = null;
            killsThisRun = 0;
            bossDefeated = false;
            comboCounter?.ResetForNewRun();
            playerStats?.ResetForNewRun();

            var prog = DungeonBlade.Core.GameServices.Progression;
            var inv = DungeonBlade.Core.GameServices.Inventory;
            if (prog != null) startingXp = prog.currentXp;
            if (inv != null) startingGold = inv.gold;

            OnRunStarted?.Invoke();
        }

        public void RegisterCheckpoint(Checkpoint cp)
        {
            lastCheckpoint = cp;
        }

        public Vector3 GetLastCheckpointPosition()
        {
            if (lastCheckpoint != null) return lastCheckpoint.GetRespawnPosition();
            if (defaultSpawnPoint != null) return defaultSpawnPoint.position;
            return Vector3.zero;
        }

        public void RegisterKill() { killsThisRun++; }

        public void RegisterBossDefeated()
        {
            bossDefeated = true;
            CompleteDungeon();
        }

        void HandlePlayerDeath()
        {
            OnPlayerDied?.Invoke();
            if (playerStats != null && playerStats.respawnsRemaining > 0)
            {
                Invoke(nameof(RespawnAfterDelay), 2.5f);
            }
            else
            {
                // Game over path — UI takes it from here
            }
        }

        void RespawnAfterDelay()
        {
            if (playerStats == null) return;
            playerStats.Respawn(GetLastCheckpointPosition(), hpFraction: 0.5f);
            OnPlayerRespawned?.Invoke();
        }

        void CompleteDungeon()
        {
            var prog = DungeonBlade.Core.GameServices.Progression;
            var inv = DungeonBlade.Core.GameServices.Inventory;

            int comboBonus = comboCounter != null ? comboCounter.ConsumePendingBonusXp() : 0;
            int totalXp = dungeonClearXp + comboBonus;

            bool firstClear = !PlayerPrefs.HasKey("cleared_" + dungeonIdForFirstClear);
            if (firstClear)
            {
                PlayerPrefs.SetInt("cleared_" + dungeonIdForFirstClear, 1);
                totalXp += firstClearBonusXp;
            }

            prog?.AddExperience(totalXp);

            var summary = new DungeonClearSummary
            {
                runTimeSeconds = Time.time - runStartTime,
                kills = killsThisRun,
                maxCombo = comboCounter != null ? comboCounter.MaxComboThisRun : 0,
                xpGained = prog != null ? (prog.currentXp - startingXp) + totalXp : totalXp,
                goldGained = inv != null ? inv.gold - startingGold : 0,
                wasFirstClear = firstClear,
            };

            // Autosave on clear
            DungeonBlade.Core.GameServices.Save?.SaveAll();

            OnDungeonClear?.Invoke();
            OnDungeonClearSummary?.Invoke(summary);
        }
    }
}
