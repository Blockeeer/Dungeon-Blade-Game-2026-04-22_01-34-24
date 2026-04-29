using UnityEngine;

namespace DungeonBlade.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class KillVolume : MonoBehaviour
    {
        [SerializeField] float fallDamage = 20f;
        [SerializeField] bool instantKill = false;
        [SerializeField] float hitCooldown = 1.5f;

        float _lastHitTime = -999f;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastHitTime < hitCooldown) return;

            var respawn = other.GetComponentInParent<RespawnManager>();
            if (respawn != null)
            {
                _lastHitTime = Time.time;
                respawn.RecoverFromFall(instantKill ? float.MaxValue : fallDamage);
            }
        }
    }
}
