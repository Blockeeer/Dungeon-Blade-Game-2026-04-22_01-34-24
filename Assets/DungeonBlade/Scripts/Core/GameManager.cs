using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace DungeonBlade.Core
{
    /// <summary>
    /// High-level game flow coordinator:
    /// Main Menu → Lobby/Hub → Dungeon → Dungeon Clear / Game Over.
    /// Attach to a persistent bootstrap GameObject alongside GameServices.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState { MainMenu, Lobby, Dungeon, DungeonClear, GameOver, Paused }

        public static GameManager Instance { get; private set; }

        [Header("Scenes")]
        public string mainMenuScene = "MainMenu";
        public string lobbyScene = "Lobby";
        public string dungeonScene = "Dungeon_ForsakenKeep";

        [Header("Refs")]
        public DungeonBlade.Dungeon.DungeonManager dungeonManager;

        [Header("Pause")]
        public GameObject pauseMenuUI;

        [Header("Events")]
        public UnityEvent<GameState> OnGameStateChanged;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        GameState previousState;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (CurrentState == GameState.Dungeon || CurrentState == GameState.Lobby)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            }
        }

        public void GoToMainMenu()
        {
            SetState(GameState.MainMenu);
            SceneManager.LoadScene(mainMenuScene);
        }

        public void GoToLobby()
        {
            SetState(GameState.Lobby);
            SceneManager.LoadScene(lobbyScene);
        }

        public void EnterDungeon()
        {
            SetState(GameState.Dungeon);
            SceneManager.LoadScene(dungeonScene);
            // Start run after scene loads
            SceneManager.sceneLoaded += OnDungeonLoaded;
        }

        void OnDungeonLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != dungeonScene) return;
            SceneManager.sceneLoaded -= OnDungeonLoaded;
            var dm = FindObjectOfType<DungeonBlade.Dungeon.DungeonManager>();
            if (dm != null) { dungeonManager = dm; dm.StartNewRun(); }
        }

        public void TogglePause()
        {
            if (CurrentState == GameState.Paused)
            {
                SetState(previousState);
                Time.timeScale = 1f;
                if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                previousState = CurrentState;
                SetState(GameState.Paused);
                Time.timeScale = 0f;
                if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void SetState(GameState s)
        {
            CurrentState = s;
            OnGameStateChanged?.Invoke(s);
        }

        public void QuitGame()
        {
            GameServices.Save?.SaveAll();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
