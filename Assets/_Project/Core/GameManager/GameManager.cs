using UnityEngine;

namespace DungeonBlade.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SaveSystem SaveSystem { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SaveSystem = new SaveSystem();
            SaveSystem.Load();
        }

        void OnApplicationQuit()
        {
            SaveSystem?.Save();
        }
    }
}
