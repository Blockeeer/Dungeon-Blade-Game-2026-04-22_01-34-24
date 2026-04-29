using System;
using UnityEngine;

namespace DungeonBlade.Dungeon
{
    public class ZoneManager : MonoBehaviour
    {
        public static ZoneManager Instance { get; private set; }

        public Zone Current { get; private set; }
        public event Action<Zone> OnZoneEntered;
        public event Action<Zone> OnZoneExited;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void NotifyZoneEntered(Zone zone)
        {
            if (Current == zone) return;
            Current = zone;
            OnZoneEntered?.Invoke(zone);
            Debug.Log($"[Zone] Entered {zone.ZoneName} ({zone.ZoneId})");
        }

        public void NotifyZoneExited(Zone zone)
        {
            if (Current != zone) return;
            OnZoneExited?.Invoke(zone);
            Current = null;
        }
    }
}
