using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBlade.Characters
{
    /// <summary>
    /// Single portrait button in the character select row.
    /// Must live in its own .cs file (not inside CharacterSelectUI.cs) or
    /// Unity's prefab serialization can't resolve the script reference,
    /// leading to persistent "Missing Script" errors on CharacterPortraitButton.prefab.
    /// </summary>
    public class CharacterPortraitButton : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerClickHandler
    {
        public Image portraitImage;
        public TMP_Text nameLabel;
        public Image frameImage;

        CharacterData data;
        System.Action<CharacterData> onHover;
        System.Action<CharacterData> onClick;

        public void Setup(CharacterData d, System.Action<CharacterData> hov, System.Action<CharacterData> clk)
        {
            data = d; onHover = hov; onClick = clk;

            // Auto-resolve refs if builder didn't set them explicitly
            if (portraitImage == null) portraitImage = GetComponent<Image>();
            if (nameLabel == null) nameLabel = GetComponentInChildren<TMP_Text>();

            if (portraitImage != null)
            {
                if (d.portrait != null)
                {
                    portraitImage.sprite = d.portrait;
                    portraitImage.color = Color.white;
                }
                else
                {
                    // Fallback: use a unique color derived from the character ID
                    // so each portrait is visually distinct even without art.
                    portraitImage.color = ColorFromId(d.characterId ?? d.displayName ?? "default");
                }
            }
            if (nameLabel != null) nameLabel.text = d.displayName;
        }

        /// <summary>Deterministic color from any string — spreads through hue space.</summary>
        static Color ColorFromId(string id)
        {
            // Hash to a stable hue 0..1
            int h = 0;
            foreach (var c in id) h = (h * 31 + c) & 0x7FFFFFFF;
            float hue = (h % 1000) / 1000f;
            return Color.HSVToRGB(hue, 0.55f, 0.75f);
        }

        /// <summary>Public access so CharacterSelectUI can use the same color scheme for detail panel.</summary>
        public static Color ColorFromIdPublic(string id) => ColorFromId(id);

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _) => onHover?.Invoke(data);
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData _) => onClick?.Invoke(data);
    }
}
