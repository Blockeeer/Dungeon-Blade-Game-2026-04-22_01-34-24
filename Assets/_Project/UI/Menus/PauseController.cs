using DungeonBlade.Core;
using DungeonBlade.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.UI.Menus
{
    public class PauseController : MonoBehaviour
    {
        public static PauseController Instance { get; private set; }

        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button returnLobbyButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] Button quitButton;
        [SerializeField] InventoryPersistence persistence;

        PlayerInputActions _input;
        bool _isOpen;

        public bool IsOpen => _isOpen;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        void Start()
        {
            _input = InputManager.Instance != null ? InputManager.Instance.Actions : new PlayerInputActions();
            if (InputManager.Instance == null) _input.Enable();

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (resumeButton != null) resumeButton.onClick.AddListener(Close);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (returnLobbyButton != null) returnLobbyButton.onClick.AddListener(() => SaveAndLoad(SceneLoader.Lobby));
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => SaveAndLoad(SceneLoader.MainMenu));
            if (quitButton != null) quitButton.onClick.AddListener(QuitToDesktop);
        }

        void Update()
        {
            if (_input == null) return;
            if (_input.Pause.WasPressedThisFrame())
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    settingsPanel.SetActive(false);
                    return;
                }
                Toggle();
            }
        }

        public void Toggle() { if (_isOpen) Close(); else Open(); }

        public void Open()
        {
            if (_isOpen) return;
            if (MenuState.IsAnyOpen) return;
            _isOpen = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            MenuState.Push();
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            MenuState.Pop();
            Time.timeScale = 1f;
            if (!MenuState.IsAnyOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        void SaveAndLoad(string sceneName)
        {
            Time.timeScale = 1f;
            if (persistence != null) persistence.SaveNow();
            if (FadeLoader.Instance != null) FadeLoader.Instance.LoadScene(sceneName);
            else SceneLoader.Load(sceneName);
        }

        void QuitToDesktop()
        {
            Time.timeScale = 1f;
            if (persistence != null) persistence.SaveNow();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
