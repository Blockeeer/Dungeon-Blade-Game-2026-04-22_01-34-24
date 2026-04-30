using System.IO;
using DungeonBlade.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.UI.Menus
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] TMP_Text versionLabel;

        void Start()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

            if (continueButton != null) continueButton.interactable = SaveExists();
            if (versionLabel != null) versionLabel.text = $"v{Application.version}";
            if (settingsPanel != null) settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        bool SaveExists()
        {
            string path = Path.Combine(Application.persistentDataPath, "DungeonBlade", "profile.json");
            return File.Exists(path);
        }

        void OnNewGame()
        {
            DeleteSave();
            LoadScene(SceneLoader.Lobby);
        }

        void OnContinue()
        {
            LoadScene(SceneLoader.Lobby);
        }

        void OnSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void DeleteSave()
        {
            string root = Path.Combine(Application.persistentDataPath, "DungeonBlade");
            string profile = Path.Combine(root, "profile.json");
            string bank = Path.Combine(root, "bank.json");
            if (File.Exists(profile)) File.Delete(profile);
            if (File.Exists(bank)) File.Delete(bank);
            Debug.Log("[MainMenu] New game — old save deleted.");
        }

        void LoadScene(string sceneName)
        {
            if (FadeLoader.Instance != null) FadeLoader.Instance.LoadScene(sceneName);
            else SceneLoader.Load(sceneName);
        }
    }
}
