using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonBlade.Core
{
    public static class SceneLoader
    {
        public const string Landing = "0_LandingScene";
        public const string MainMenu = "1_MainMenu";
        public const string Lobby = "2_Lobby";
        public const string Dungeon1 = "3_Dungeon1";

        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public static AsyncOperation LoadAsync(string sceneName)
        {
            return SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
