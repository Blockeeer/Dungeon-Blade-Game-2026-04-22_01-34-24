using UnityEngine;
using UnityEngine.Events;
using DungeonBlade.Items;

namespace DungeonBlade.Dungeon
{
    /// <summary>World pickup: item, gold, or token that enters the player's inventory on touch.</summary>
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour
    {
        public enum PickupKind { Item, Gold, Token, HealthOrb, StaminaOrb }

        public PickupKind kind = PickupKind.Item;
        public ItemData item;
        public int quantity = 1;
        public int goldAmount = 0;

        [Header("FX")]
        public float bobHeight = 0.15f;
        public float bobSpeed = 2f;
        public float spin = 60f;
        public AudioClip pickupSfx;

        Vector3 startPos;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void Start() { startPos = transform.position; }

        void Update()
        {
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.Rotate(0, spin * Time.deltaTime, 0);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var inv = DungeonBlade.Core.GameServices.Inventory;
            var stats = other.GetComponentInParent<DungeonBlade.Player.PlayerStats>();

            switch (kind)
            {
                case PickupKind.Item:
                    if (item != null) inv?.TryAddItem(item, quantity);
                    break;
                case PickupKind.Gold:
                    inv?.AddGold(goldAmount);
                    break;
                case PickupKind.Token:
                    inv?.AddTokens(quantity);
                    break;
                case PickupKind.HealthOrb:
                    stats?.SendMessage("ApplyHeal", (float)goldAmount, SendMessageOptions.DontRequireReceiver);
                    break;
                case PickupKind.StaminaOrb:
                    stats?.SendMessage("ApplyStaminaRestore", (float)goldAmount, SendMessageOptions.DontRequireReceiver);
                    break;
            }

            if (pickupSfx != null) AudioSource.PlayClipAtPoint(pickupSfx, transform.position);
            Destroy(gameObject);
        }
    }

    /// <summary>Reward chest spawned after boss kill. Rolls its loot table on interact.</summary>
    public class RewardChest : MonoBehaviour
    {
        public DungeonBlade.Loot.LootTable lootTable;
        public GameObject coinPickupPrefab;
        public GameObject tokenPickupPrefab;
        public GameObject itemPickupPrefab;
        public Transform spawnPoint;
        public float interactRange = 2.5f;
        public UnityEvent OnOpened;

        bool opened;

        void Update()
        {
            if (opened) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            float d = Vector3.Distance(transform.position, player.transform.position);
            if (d <= interactRange && Input.GetKeyDown(KeyCode.F))
                Open();
        }

        public void Open()
        {
            if (opened || lootTable == null) return;
            opened = true;

            Vector3 anchor = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.5f;
            var drops = lootTable.Roll();

            foreach (var item in drops)
            {
                if (itemPickupPrefab != null)
                {
                    var p = Instantiate(itemPickupPrefab, anchor + Random.insideUnitSphere * 1f, Quaternion.identity);
                    if (p.TryGetComponent<Pickup>(out var pickup))
                    {
                        pickup.kind = Pickup.PickupKind.Item;
                        pickup.item = item;
                        pickup.quantity = 1;
                    }
                }
                else
                {
                    DungeonBlade.Core.GameServices.Inventory?.TryAddItem(item);
                }
            }

            int gold = lootTable.RollGold();
            if (gold > 0) DungeonBlade.Core.GameServices.Inventory?.AddGold(gold);

            int tokens = lootTable.RollTokens();
            if (tokens > 0) DungeonBlade.Core.GameServices.Inventory?.AddTokens(tokens);

            OnOpened?.Invoke();
        }
    }
}
