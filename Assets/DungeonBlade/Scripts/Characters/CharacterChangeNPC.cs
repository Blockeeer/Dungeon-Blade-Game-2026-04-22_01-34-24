using UnityEngine;

namespace DungeonBlade.Characters
{
    /// <summary>
    /// Interactable NPC that reopens the CharacterSelectUI when the player
    /// steps close and presses Interact (E). Place in the Lobby so players can
    /// change their character any time they want.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CharacterChangeNPC : MonoBehaviour
    {
        public float interactRange = 3f;
        public KeyCode interactKey = KeyCode.E;
        public GameObject promptVisual;

        CharacterSelectUI selectUI;
        Transform player;
        bool inRange;

        void Start()
        {
            selectUI = FindObjectOfType<CharacterSelectUI>();
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            if (promptVisual != null) promptVisual.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            inRange = true;
            player = other.transform;
            if (promptVisual != null) promptVisual.SetActive(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            inRange = false;
            if (promptVisual != null) promptVisual.SetActive(false);
        }

        void Update()
        {
            if (!inRange || selectUI == null) return;
            if (Input.GetKeyDown(interactKey)) selectUI.Open();
        }
    }
}
