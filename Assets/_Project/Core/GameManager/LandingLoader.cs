using UnityEngine;

namespace DungeonBlade.Core
{
    public class LandingLoader : MonoBehaviour
    {
        [SerializeField] string nextScene = SceneLoader.MainMenu;
        [SerializeField] float minDisplaySeconds = 0.5f;

        float _startTime;

        void Start()
        {
            _startTime = Time.time;
            EnsureSystem<GameManager>("[GameManager]");
            EnsureSystem<InputManager>("[InputManager]");
        }

        void Update()
        {
            if (Time.time - _startTime >= minDisplaySeconds)
            {
                SceneLoader.Load(nextScene);
                enabled = false;
            }
        }

        static T EnsureSystem<T>(string objectName) where T : Component
        {
            var existing = FindObjectOfType<T>();
            if (existing != null) return existing;

            var go = new GameObject(objectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }
    }
}
