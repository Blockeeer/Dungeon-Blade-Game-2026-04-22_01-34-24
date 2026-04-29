using System.Collections.Generic;
using DungeonBlade.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonBlade.Boss
{
    public class BossAddSpawner : MonoBehaviour
    {
        [SerializeField] EnemyBase addPrefab;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] int addsPerWave = 2;
        [SerializeField] int maxAliveAdds = 4;
        [SerializeField] float overrideMaxHealth = 80f;
        [SerializeField] float overrideMoveSpeed = 0f;
        [SerializeField] float overrideAttackDamage = 0f;

        readonly List<EnemyBase> _live = new List<EnemyBase>();

        public int AliveCount
        {
            get
            {
                _live.RemoveAll(e => e == null || !e.IsAlive);
                return _live.Count;
            }
        }

        public void SpawnWave()
        {
            if (addPrefab == null)
            {
                Debug.LogWarning("[BossAddSpawner] No addPrefab assigned.");
                return;
            }

            int alive = AliveCount;
            int budget = Mathf.Max(0, maxAliveAdds - alive);
            int toSpawn = Mathf.Min(addsPerWave, budget);

            if (toSpawn == 0)
            {
                Debug.Log($"[BossAddSpawner] At cap ({alive}/{maxAliveAdds}) — skipping wave.");
                return;
            }

            for (int i = 0; i < toSpawn; i++)
            {
                Transform point = PickSpawnPoint(i);
                if (point == null) continue;

                Vector3 pos = point.position;
                if (NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas))
                    pos = hit.position;

                var add = Instantiate(addPrefab, pos, point.rotation);
                ApplyOverrides(add);
                _live.Add(add);
            }

            Debug.Log($"[BossAddSpawner] Spawned {toSpawn} add(s).");
        }

        Transform PickSpawnPoint(int i)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return transform;
            return spawnPoints[i % spawnPoints.Length];
        }

        void ApplyOverrides(EnemyBase add)
        {
            if (overrideMaxHealth > 0f) add.Stats.MaxHealth = overrideMaxHealth;
            if (overrideMoveSpeed > 0f) add.Stats.MoveSpeed = overrideMoveSpeed;
            if (overrideAttackDamage > 0f) add.Stats.AttackDamage = overrideAttackDamage;
            add.ReapplyStats();
        }

        void OnDrawGizmosSelected()
        {
            if (spawnPoints == null) return;
            Gizmos.color = Color.cyan;
            foreach (var p in spawnPoints)
            {
                if (p == null) continue;
                Gizmos.DrawWireSphere(p.position, 0.5f);
                Gizmos.DrawLine(p.position, p.position + Vector3.up * 1.5f);
            }
        }
    }
}
