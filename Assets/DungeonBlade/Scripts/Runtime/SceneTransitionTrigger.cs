using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonBlade.Runtime
{
    /// <summary>Trigger that loads a target scene when the player enters.</summary>
    [RequireComponent(typeof(Collider))]
    public class SceneTransitionTrigger : MonoBehaviour
    {
        public string targetScene = "Dungeon_ForsakenKeep";
        public float delayBeforeLoad = 0.3f;

        bool triggered;

        void OnTriggerEnter(Collider other)
        {
            if (triggered || !other.CompareTag("Player") || string.IsNullOrEmpty(targetScene)) return;
            triggered = true;
            Invoke(nameof(LoadTarget), delayBeforeLoad);
        }

        void LoadTarget()
        {
            DungeonBlade.Core.GameServices.Save?.SaveAll();
            SceneManager.LoadScene(targetScene);
        }
    }
}
