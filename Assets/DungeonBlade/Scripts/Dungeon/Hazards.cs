using UnityEngine;
using DungeonBlade.Combat;

namespace DungeonBlade.Dungeon
{
    /// <summary>Spike trap: floor tile that extends spikes on a timer and damages anyone standing on it.</summary>
    [RequireComponent(typeof(Collider))]
    public class SpikeTrap : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        public float safeDuration = 1.5f;
        public float warnDuration = 0.4f;
        public float activeDuration = 0.8f;

        [Header("Damage")]
        public float damage = 15f;
        public Vector3 knockback = new Vector3(0, 4, 0);

        [Header("Visuals")]
        public Transform spikes;
        public float spikesDownY = 0f;
        public float spikesUpY   = 0.5f;
        public Renderer warnRenderer;
        public Color safeColor = Color.white;
        public Color warnColor = new Color(1f, 0.4f, 0.1f);

        bool active;
        bool insideHit;
        Collider playerInside;

        void Update()
        {
            float cycle = safeDuration + warnDuration + activeDuration;
            float t = Time.time % cycle;

            if (t < safeDuration)
            {
                SetSpikes(false);
                if (warnRenderer != null) warnRenderer.material.color = safeColor;
                active = false;
                insideHit = false;
            }
            else if (t < safeDuration + warnDuration)
            {
                if (warnRenderer != null) warnRenderer.material.color = warnColor;
                active = false;
            }
            else
            {
                if (!active)
                {
                    active = true;
                    insideHit = false;
                }
                SetSpikes(true);
                if (active && !insideHit && playerInside != null)
                {
                    ApplyDamage(playerInside);
                    insideHit = true;
                }
            }
        }

        void SetSpikes(bool up)
        {
            if (spikes == null) return;
            var p = spikes.localPosition;
            p.y = up ? spikesUpY : spikesDownY;
            spikes.localPosition = p;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInside = other;
            if (active && !insideHit) { ApplyDamage(other); insideHit = true; }
        }
        void OnTriggerExit(Collider other)
        {
            if (other == playerInside) playerInside = null;
        }

        void ApplyDamage(Collider c)
        {
            var dmg = c.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg.IsDead) return;
            dmg.TakeDamage(new DamageInfo {
                amount = damage,
                source = DamageSource.Environment,
                knockback = knockback,
                hitPoint = c.transform.position,
                attacker = gameObject,
            });
        }
    }

    /// <summary>Collapsing floor: crumbles after player stands on it for X seconds.</summary>
    [RequireComponent(typeof(Collider))]
    public class CollapsingFloor : MonoBehaviour
    {
        public float delayBeforeCollapse = 1.5f;
        public float shakeAmplitude = 0.05f;
        public float fallAcceleration = 20f;
        public float resetAfter = 5f;
        public Rigidbody floorBody;   // kinematic starting; turned dynamic when triggered

        bool triggered;
        bool collapsing;
        float triggerTime;
        Vector3 originalPos;
        Quaternion originalRot;

        void Awake()
        {
            originalPos = transform.position;
            originalRot = transform.rotation;
            if (floorBody == null) floorBody = GetComponent<Rigidbody>();
            if (floorBody != null) floorBody.isKinematic = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (triggered) return;
            triggered = true;
            triggerTime = Time.time;
            Invoke(nameof(Collapse), delayBeforeCollapse);
            Invoke(nameof(Reset), resetAfter);
        }

        void Update()
        {
            if (triggered && !collapsing)
            {
                float t = Time.time - triggerTime;
                float amp = Mathf.Lerp(0f, shakeAmplitude, Mathf.Clamp01(t / delayBeforeCollapse));
                transform.position = originalPos + new Vector3(
                    (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * amp,
                    (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * amp,
                    0);
            }
        }

        void Collapse()
        {
            collapsing = true;
            if (floorBody != null)
            {
                floorBody.isKinematic = false;
                floorBody.useGravity = true;
            }
        }

        void Reset()
        {
            triggered = false;
            collapsing = false;
            if (floorBody != null)
            {
                floorBody.velocity = Vector3.zero;
                floorBody.angularVelocity = Vector3.zero;
                floorBody.isKinematic = true;
            }
            transform.position = originalPos;
            transform.rotation = originalRot;
        }
    }

    /// <summary>Arrow wall: pressure plate trigger fires arrows from wall slots.</summary>
    public class ArrowWall : MonoBehaviour
    {
        public Collider pressurePlate;
        public Transform[] arrowSpawnPoints;
        public GameObject arrowProjectilePrefab;
        public float arrowSpeed = 25f;
        public float arrowDamage = 15f;
        public float cooldown = 2.5f;
        public int volleyCount = 1;
        public float volleyGap = 0.15f;

        float nextFireTime;

        void OnEnable()
        {
            if (pressurePlate != null)
            {
                var trigger = pressurePlate.gameObject.AddComponent<PressurePlateTrigger>();
                trigger.wall = this;
            }
        }

        public void Fire()
        {
            if (Time.time < nextFireTime) return;
            nextFireTime = Time.time + cooldown;
            StartCoroutine(FireVolley());
        }

        System.Collections.IEnumerator FireVolley()
        {
            for (int v = 0; v < volleyCount; v++)
            {
                foreach (var p in arrowSpawnPoints)
                {
                    if (p == null) continue;
                    if (arrowProjectilePrefab == null) continue;
                    var obj = Instantiate(arrowProjectilePrefab, p.position, p.rotation);
                    var proj = obj.GetComponent<Projectile>() ?? obj.AddComponent<Projectile>();
                    proj.Launch(p.forward, arrowSpeed, arrowDamage, gameObject, DamageSource.Environment, p.forward * 3f);
                }
                yield return new WaitForSeconds(volleyGap);
            }
        }
    }

    /// <summary>Helper trigger for ArrowWall pressure plate.</summary>
    public class PressurePlateTrigger : MonoBehaviour
    {
        public ArrowWall wall;
        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            wall?.Fire();
        }
    }

    /// <summary>Fall pit: deals fall damage and teleports player to respawn per GDD 3.4.</summary>
    [RequireComponent(typeof(Collider))]
    public class FallPit : MonoBehaviour
    {
        public float fallDamage = 20f;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var stats = other.GetComponentInParent<DungeonBlade.Player.PlayerStats>();
            var pc = other.GetComponentInParent<DungeonBlade.Player.PlayerController>();
            if (stats != null)
            {
                stats.TakeDamage(new DamageInfo {
                    amount = fallDamage, source = DamageSource.Environment, attacker = gameObject
                });
            }
            var dungeon = DungeonBlade.Core.GameServices.Dungeon;
            if (dungeon != null && pc != null) pc.Teleport(dungeon.GetLastCheckpointPosition());
        }
    }
}
