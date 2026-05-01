using UnityEngine;
using DungeonBlade.Inventory;
using DungeonBlade.Progression;
using DungeonBlade.Save;

namespace DungeonBlade.Core
{
    /// <summary>
    /// Service locator for global game systems.
    /// Attach one instance to a persistent scene object (e.g. "GameBootstrap").
    /// Other code accesses systems via GameServices.Inventory, GameServices.Progression, etc.
    /// </summary>
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        public static InventorySystem  Inventory   => Instance?.inventory;
        public static PlayerProgression Progression => Instance?.progression;
        public static SaveSystem       Save        => Instance?.save;
        public static Bank.BankSystem  Bank        => Instance?.bank;
        public static DungeonBlade.Dungeon.DungeonManager Dungeon => Instance?.dungeon;
        public static DungeonBlade.Characters.CharacterRoster Roster => Instance?.roster;

        [Header("System References")]
        public InventorySystem inventory;
        public PlayerProgression progression;
        public SaveSystem save;
        public Bank.BankSystem bank;
        public DungeonBlade.Dungeon.DungeonManager dungeon;
        public DungeonBlade.Characters.CharacterRoster roster;

        [Header("Player")]
        public GameObject playerRoot;   // assign in inspector; used for respawn/lookup

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-find if not assigned
            if (inventory   == null) inventory   = GetComponentInChildren<InventorySystem>();
            if (progression == null) progression = GetComponentInChildren<PlayerProgression>();
            if (save        == null) save        = GetComponentInChildren<SaveSystem>();
            if (bank        == null) bank        = GetComponentInChildren<Bank.BankSystem>();
            if (dungeon     == null) dungeon     = GetComponentInChildren<DungeonBlade.Dungeon.DungeonManager>();
            if (roster      == null) roster      = GetComponentInChildren<DungeonBlade.Characters.CharacterRoster>();
        }

        void OnApplicationQuit()
        {
            save?.SaveAll();
        }
    }
}
