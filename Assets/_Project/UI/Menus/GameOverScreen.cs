using System.Collections;
using DungeonBlade.Core;
using DungeonBlade.Dungeon;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.UI.Menus
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] RespawnManager respawnManager;
        [SerializeField] Button retryButton;
        [SerializeField] Button lobbyButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] float showDelay = 1.0f;

        bool _shown;

        void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (respawnManager != null) respawnManager.OnRunFailed += OnRunFailed;

            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (lobbyButton != null) lobbyButton.onClick.AddListener(OnReturnLobby);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        void OnDestroy()
        {
            if (respawnManager != null) respawnManager.OnRunFailed -= OnRunFailed;
            Time.timeScale = 1f;
        }

        void OnRunFailed()
        {
            if (_shown) return;
            _shown = true;
            StartCoroutine(ShowAfterDelay());
        }

        IEnumerator ShowAfterDelay()
        {
            float t = 0f;
            while (t < showDelay) { t += Time.unscaledDeltaTime; yield return null; }
            Show();
        }

        void Show()
        {
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnRetry()
        {
            Time.timeScale = 1f;
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (FadeLoader.Instance != null) FadeLoader.Instance.LoadScene(current);
            else SceneLoader.Load(current);
        }

        void OnReturnLobby()
        {
            Time.timeScale = 1f;
            if (FadeLoader.Instance != null) FadeLoader.Instance.LoadScene(SceneLoader.Lobby);
            else SceneLoader.Load(SceneLoader.Lobby);
        }

        void OnMainMenu()
        {
            Time.timeScale = 1f;
            if (FadeLoader.Instance != null) FadeLoader.Instance.LoadScene(SceneLoader.MainMenu);
            else SceneLoader.Load(SceneLoader.MainMenu);
        }
    }
}
