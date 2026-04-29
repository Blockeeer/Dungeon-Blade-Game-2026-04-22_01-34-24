using TMPro;
using UnityEngine;

namespace DungeonBlade.Inventory.UI
{
    public class ItemTooltip : MonoBehaviour
    {
        [SerializeField] RectTransform root;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text typeText;
        [SerializeField] Vector2 cursorOffset = new Vector2(20f, 20f);

        Canvas _canvas;

        void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (root != null) root.gameObject.SetActive(false);
        }

        public void Show(Item item, Vector3 worldAnchor)
        {
            if (item == null || root == null) return;
            root.gameObject.SetActive(true);
            if (nameText != null) nameText.text = item.DisplayName;
            if (descriptionText != null) descriptionText.text = item.Description;
            if (typeText != null) typeText.text = $"{item.Type}{(item.EquipSlot != EquipmentSlot.None ? $" • {item.EquipSlot}" : "")}";
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (root == null || !root.gameObject.activeSelf) return;
            Vector2 mouse = Input.mousePosition;
            root.position = mouse + cursorOffset;
        }
    }
}
