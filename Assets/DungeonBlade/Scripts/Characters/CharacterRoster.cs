using System.Collections.Generic;
using UnityEngine;

namespace DungeonBlade.Characters
{
    /// <summary>
    /// Persistent list of all selectable characters plus the currently-selected
    /// one. Survives scene loads via DontDestroyOnLoad. GameServices auto-grabs
    /// this on Awake; access via GameServices.Roster.
    ///
    /// Selection is persisted to PlayerPrefs, so the user's choice survives
    /// app restarts until they pick a different character.
    /// </summary>
    public class CharacterRoster : MonoBehaviour
    {
        const string PREF_KEY = "DungeonBlade.SelectedCharacterId";

        [Tooltip("All characters the player can choose from. Populated by DBCharacterBuilder or manually.")]
        public List<CharacterData> characters = new List<CharacterData>();

        [Tooltip("Default character used when no selection has been made yet. Falls back to characters[0].")]
        public CharacterData defaultCharacter;

        CharacterData currentSelection;

        void Awake()
        {
            // Survive scene loads
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            LoadSelection();
        }

        /// <summary>Get the currently selected character, or the default if none set.</summary>
        public CharacterData Current
        {
            get
            {
                if (currentSelection != null) return currentSelection;
                if (defaultCharacter != null) return defaultCharacter;
                if (characters.Count > 0) return characters[0];
                return null;
            }
        }

        /// <summary>Change the active character. Triggers OnSelectionChanged and persists to PlayerPrefs.</summary>
        public void Select(CharacterData data)
        {
            if (data == null) return;
            currentSelection = data;
            PlayerPrefs.SetString(PREF_KEY, data.characterId);
            PlayerPrefs.Save();
            OnSelectionChanged?.Invoke(data);
        }

        public void SelectById(string id)
        {
            var match = characters.Find(c => c != null && c.characterId == id);
            if (match != null) Select(match);
        }

        public event System.Action<CharacterData> OnSelectionChanged;

        void LoadSelection()
        {
            if (!PlayerPrefs.HasKey(PREF_KEY)) return;
            var id = PlayerPrefs.GetString(PREF_KEY);
            var match = characters.Find(c => c != null && c.characterId == id);
            if (match != null) currentSelection = match;
        }
    }
}
