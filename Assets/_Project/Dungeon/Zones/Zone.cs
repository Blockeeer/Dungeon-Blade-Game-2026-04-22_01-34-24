using UnityEngine;

namespace DungeonBlade.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class Zone : MonoBehaviour
    {
        [SerializeField] string zoneName = "Zone";
        [SerializeField] string zoneId = "zone_1";

        public string ZoneName => zoneName;
        public string ZoneId => zoneId;
        public bool HasBeenEntered { get; private set; }

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var manager = ZoneManager.Instance;
            if (manager == null) return;

            if (other.GetComponentInParent<Player.PlayerStats>() == null) return;

            HasBeenEntered = true;
            manager.NotifyZoneEntered(this);
        }

        void OnTriggerExit(Collider other)
        {
            var manager = ZoneManager.Instance;
            if (manager == null) return;
            if (other.GetComponentInParent<Player.PlayerStats>() == null) return;
            manager.NotifyZoneExited(this);
        }
    }
}
