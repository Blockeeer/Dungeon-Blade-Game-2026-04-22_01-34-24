using DungeonBlade.Bank;
using UnityEngine;

namespace DungeonBlade.Rewards
{
    public class Chest : Interactable
    {
        [SerializeField] LootTable lootTable;
        [SerializeField] Transform spawnPoint;
        [SerializeField] float scatterRadius = 1.0f;
        [SerializeField] GameObject openVisual;
        [SerializeField] GameObject closedVisual;

        bool _opened;

        void Awake()
        {
            UpdateVisuals();
        }

        public override void OnInteract(GameObject player)
        {
            if (_opened) return;
            _opened = true;

            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            if (lootTable != null) DropSpawner.SpawnLoot(lootTable, origin, scatterRadius);
            else Debug.LogWarning($"[Chest] {name} has no LootTable.");

            Debug.Log($"[Chest] {name} opened.");
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            if (closedVisual != null) closedVisual.SetActive(!_opened);
            if (openVisual != null) openVisual.SetActive(_opened);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
            Vector3 c = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            Gizmos.DrawWireSphere(c, scatterRadius);
        }
    }
}
