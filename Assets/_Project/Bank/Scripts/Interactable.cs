using UnityEngine;

namespace DungeonBlade.Bank
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] string promptText = "Press [F] to interact";

        public string PromptText => promptText;

        public abstract void OnInteract(GameObject player);
    }
}
