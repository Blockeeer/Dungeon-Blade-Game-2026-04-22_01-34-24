using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Dungeon
{
    public class ArrowWall : MonoBehaviour
    {
        [SerializeField] PressurePlate plate;
        [SerializeField] Transform[] arrowSpawnPoints;
        [SerializeField] float arrowDamage = 8f;
        [SerializeField] float arrowKnockback = 3f;
        [SerializeField] float maxRange = 30f;
        [SerializeField] float fireDelay = 0.2f;
        [SerializeField] float volleyCooldown = 1.5f;
        [SerializeField] LayerMask hitMask = ~0;

        float _lastFireTime = -999f;
        float _scheduledFireTime = -1f;

        void OnEnable() { if (plate != null) plate.OnPressed += SchedulePlateFire; }
        void OnDisable() { if (plate != null) plate.OnPressed -= SchedulePlateFire; }

        void SchedulePlateFire()
        {
            if (Time.time - _lastFireTime < volleyCooldown) return;
            _scheduledFireTime = Time.time + fireDelay;
            Debug.Log($"[ArrowWall] {name} volley scheduled");
        }

        void Update()
        {
            if (_scheduledFireTime > 0f && Time.time >= _scheduledFireTime)
            {
                _scheduledFireTime = -1f;
                _lastFireTime = Time.time;
                FireVolley();
            }
        }

        void FireVolley()
        {
            if (arrowSpawnPoints == null || arrowSpawnPoints.Length == 0)
            {
                Debug.LogWarning($"[ArrowWall] {name} has no spawn points.");
                return;
            }

            Debug.Log($"[ArrowWall] {name} firing {arrowSpawnPoints.Length} arrows");

            foreach (var sp in arrowSpawnPoints)
            {
                if (sp == null) continue;

                Vector3 origin = sp.position;
                Vector3 dir = sp.forward;

                SpawnArrowTrail(origin, origin + dir * maxRange);

                if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawLine(origin, hit.point, Color.red, 2f);
                    Debug.Log($"[ArrowWall] ray from {sp.name} hit {hit.collider.name} at {hit.point}");
                    var dmg = hit.collider.GetComponentInParent<IDamageable>();
                    if (dmg != null && dmg.IsAlive)
                    {
                        dmg.ApplyDamage(new DamageInfo
                        {
                            Amount = arrowDamage,
                            HitPoint = hit.point,
                            HitDirection = dir,
                            Knockback = arrowKnockback,
                            Source = gameObject,
                            Type = DamageType.Ranged,
                        });
                    }
                }
                else
                {
                    Debug.DrawRay(origin, dir * maxRange, Color.red, 2f);
                    Debug.Log($"[ArrowWall] ray from {sp.name} hit nothing");
                }
            }
        }

        void SpawnArrowTrail(Vector3 start, Vector3 end)
        {
            var go = new GameObject("ArrowTrail");
            go.transform.SetParent(transform, worldPositionStays: true);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.material.color = Color.red;
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            Destroy(go, 0.3f);
        }

        void OnDrawGizmosSelected()
        {
            if (arrowSpawnPoints == null) return;
            Gizmos.color = Color.red;
            foreach (var sp in arrowSpawnPoints)
            {
                if (sp == null) continue;
                Gizmos.DrawRay(sp.position, sp.forward * maxRange);
            }
        }
    }
}
