using UnityEngine;
using UnityEngine.Events;

namespace DungeonBlade.Dungeon
{
    /// <summary>
    /// Checkpoint trigger per GDD 3.3.
    /// On entry: refresh stamina, +20 HP, save respawn point, optionally autosave bank/profile.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Identity")]
        public string checkpointId = "zone_1_end";
        public int zoneIndex = 1;
        public bool autosaveOnActivate = true;

        [Header("Respawn")]
        public Transform respawnPoint;

        [Header("Events")]
        public UnityEvent OnCheckpointActivated;

        bool activated;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (activated) return;
            if (!other.CompareTag("Player")) return;

            activated = true;
            var stats = other.GetComponentInParent<DungeonBlade.Player.PlayerStats>();
            stats?.CheckpointRefresh();

            var dungeon = DungeonBlade.Core.GameServices.Dungeon;
            dungeon?.RegisterCheckpoint(this);

            if (autosaveOnActivate)
                DungeonBlade.Core.GameServices.Save?.SaveAll();

            OnCheckpointActivated?.Invoke();
        }

        public Vector3 GetRespawnPosition() => respawnPoint != null ? respawnPoint.position : transform.position;
    }
}
