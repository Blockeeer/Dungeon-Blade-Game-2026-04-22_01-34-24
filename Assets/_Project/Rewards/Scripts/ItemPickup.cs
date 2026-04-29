using DungeonBlade.Bank;
using DungeonBlade.Inventory;
using UnityEngine;

namespace DungeonBlade.Rewards
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : Interactable
    {
        [SerializeField] Item item;
        [SerializeField] int quantity = 1;
        [SerializeField] float bobAmplitude = 0.15f;
        [SerializeField] float bobSpeed = 2f;
        [SerializeField] float spinSpeed = 60f;

        Vector3 _basePosition;

        public Item Item => item;
        public int Quantity => quantity;

        void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            _basePosition = transform.position;
        }

        public void Initialize(Item it, int qty)
        {
            item = it;
            quantity = Mathf.Max(1, qty);
            _basePosition = transform.position;
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = _basePosition + Vector3.up * bob;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        public override void OnInteract(GameObject player)
        {
            if (item == null || InventoryManager.Instance == null) return;
            int leftover = InventoryManager.Instance.AddItem(item, quantity);
            if (leftover >= quantity)
            {
                Debug.Log($"[Pickup] Inventory full — could not pick up {item.DisplayName}.");
                return;
            }

            int picked = quantity - leftover;
            Debug.Log($"[Pickup] Picked up {picked}× {item.DisplayName}.");
            quantity = leftover;
            if (quantity <= 0) Destroy(gameObject);
        }
    }
}
