using DungeonBlade.Core;
using TMPro;
using UnityEngine;

namespace DungeonBlade.Bank
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float interactRange = 2.5f;
        [SerializeField] LayerMask interactMask = ~0;
        [SerializeField] TMP_Text promptLabel;
        [SerializeField] Camera lookCamera;

        PlayerInputActions _input;
        Interactable _current;

        void Start()
        {
            _input = InputManager.Instance != null ? InputManager.Instance.Actions : new PlayerInputActions();
            if (InputManager.Instance == null) _input.Enable();
            if (lookCamera == null) lookCamera = Camera.main;
            if (promptLabel != null) promptLabel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (IsAnyMenuOpen()) { ClearPrompt(); return; }

            UpdateNearestInteractable();

            if (_current != null && _input.Interact.WasPressedThisFrame())
            {
                _current.OnInteract(gameObject);
            }
        }

        bool IsAnyMenuOpen()
        {
            if (Inventory.InventoryController.Instance != null && Inventory.InventoryController.Instance.IsOpen) return true;
            if (UI.BankController.Instance != null && UI.BankController.Instance.IsOpen) return true;
            if (UI.ShopController.Instance != null && UI.ShopController.Instance.IsOpen) return true;
            return false;
        }

        void UpdateNearestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactMask, QueryTriggerInteraction.Collide);

            Interactable best = null;
            float bestDot = -1f;
            Vector3 fwd = lookCamera != null ? lookCamera.transform.forward : transform.forward;
            Vector3 origin = lookCamera != null ? lookCamera.transform.position : transform.position;

            foreach (var h in hits)
            {
                var inter = h.GetComponentInParent<Interactable>();
                if (inter == null) continue;
                Vector3 to = (inter.transform.position - origin).normalized;
                float dot = Vector3.Dot(fwd, to);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = inter;
                }
            }

            if (best != _current)
            {
                _current = best;
                ShowPrompt(_current?.PromptText);
            }
            else if (_current != null)
            {
                ShowPrompt(_current.PromptText);
            }
        }

        void ShowPrompt(string text)
        {
            if (promptLabel == null) return;
            if (string.IsNullOrEmpty(text))
            {
                promptLabel.gameObject.SetActive(false);
                return;
            }
            promptLabel.gameObject.SetActive(true);
            promptLabel.text = text;
        }

        void ClearPrompt() => ShowPrompt(null);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
