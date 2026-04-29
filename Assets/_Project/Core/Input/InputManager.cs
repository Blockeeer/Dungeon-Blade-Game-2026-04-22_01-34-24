using UnityEngine;

namespace DungeonBlade.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        public PlayerInputActions Actions { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Actions = new PlayerInputActions();
            Actions.Enable();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Actions?.Disable();
            }
        }
    }
}
