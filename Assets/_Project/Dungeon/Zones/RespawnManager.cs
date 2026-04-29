using System;
using DungeonBlade.Player;
using UnityEngine;

namespace DungeonBlade.Dungeon
{
    [RequireComponent(typeof(PlayerStats))]
    public class RespawnManager : MonoBehaviour
    {
        [SerializeField] int maxRespawnsPerRun = 3;
        [SerializeField] float respawnHealthPercent = 0.5f;
        [SerializeField] float groundCheckRadius = 0.4f;
        [SerializeField] float groundCheckMaxDistance = 1.5f;
        [SerializeField] LayerMask groundLayers = ~0;

        public int RespawnsRemaining { get; private set; }
        public Checkpoint Current { get; private set; }
        public Vector3 LastStableGround { get; private set; }

        public event Action<int> OnRespawnsChanged;
        public event Action OnRunFailed;

        PlayerStats _stats;
        CharacterController _controller;
        Vector3 _initialSpawn;
        Quaternion _initialRotation;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _controller = GetComponent<CharacterController>();
            RespawnsRemaining = maxRespawnsPerRun;
            _initialSpawn = transform.position;
            _initialRotation = transform.rotation;
            LastStableGround = transform.position;
        }

        void OnEnable() => _stats.OnDeath += HandleDeath;
        void OnDisable() => _stats.OnDeath -= HandleDeath;

        void Update()
        {
            if (!_stats.IsAlive) return;

            if (Physics.SphereCast(transform.position + Vector3.up * 0.1f, groundCheckRadius, Vector3.down,
                    out RaycastHit hit, groundCheckMaxDistance, groundLayers, QueryTriggerInteraction.Ignore))
            {
                LastStableGround = hit.point + Vector3.up * 0.1f;
            }
        }

        public void RegisterCheckpoint(Checkpoint cp)
        {
            Current = cp;
            _stats.Heal(cp.HealAmount);
            Debug.Log($"[Respawn] Checkpoint reached: {cp.Id}");
        }

        public void RecoverFromFall(float damage)
        {
            _stats.TakeDamage(damage);
            Debug.Log($"[Respawn] Fell into pit. Took {damage:F0} damage. HP: {_stats.Health:F0}/{_stats.MaxHealth:F0}");
            if (!_stats.IsAlive) return;

            Vector3 target = Current != null ? Current.SpawnPosition : LastStableGround;
            Quaternion rot = Current != null ? Current.SpawnRotation : transform.rotation;
            TeleportTo(target, rot);
        }

        void HandleDeath()
        {
            if (RespawnsRemaining <= 0)
            {
                Debug.Log("[Respawn] Run failed — no respawns left.");
                OnRunFailed?.Invoke();
                return;
            }

            RespawnsRemaining--;
            OnRespawnsChanged?.Invoke(RespawnsRemaining);

            Vector3 pos = Current != null ? Current.SpawnPosition : _initialSpawn;
            Quaternion rot = Current != null ? Current.SpawnRotation : _initialRotation;
            TeleportTo(pos, rot);
            _stats.Revive(_stats.MaxHealth * respawnHealthPercent);
            Debug.Log($"[Respawn] Respawned. {RespawnsRemaining} remaining.");
        }

        void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (_controller != null) _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (_controller != null) _controller.enabled = true;
        }
    }
}
