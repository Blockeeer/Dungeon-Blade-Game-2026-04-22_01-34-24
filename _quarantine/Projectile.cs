using UnityEngine;

namespace DungeonBlade.Combat
{
    /// <summary>Kinematic projectile used by archers, boss bone-throws, etc.</summary>
    public class Projectile : MonoBehaviour
    {
        public float lifetime = 6f;
        public LayerMask hitMask = ~0;
        public TrailRenderer trail;

        Vector3 velocity;
        float damage;
        GameObject owner;
        DamageSource source;
        Vector3 knockback;
        bool launched;
        float spawnTime;

        public void Launch(Vector3 direction, float speed, float damage, GameObject owner, DamageSource source, Vector3 knockback = default)
        {
            velocity = direction.normalized * speed;
            this.damage = damage;
            this.owner = owner;
            this.source = source;
            this.knockback = knockback;
            launched = true;
            spawnTime = Time.time;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        void Update()
        {
            if (!launched) return;
            if (Time.time - spawnTime > lifetime) { Destroy(gameObject); return; }

            float step = velocity.magnitude * Time.deltaTime;
            if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (owner != null && hit.transform.root == owner.transform.root)
                {
                    // Skip owner, continue flight
                    transform.position = hit.point + velocity.normalized * 0.02f;
                }
                else
                {
                    OnHit(hit);
                    return;
                }
            }
            else
            {
                transform.position += velocity * Time.deltaTime;
            }
        }

        void OnHit(RaycastHit hit)
        {
            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg != null && !dmg.IsDead)
            {
                dmg.TakeDamage(new DamageInfo {
                    amount = damage,
                    source = source,
                    knockback = knockback,
                    hitPoint = hit.point,
                    attacker = owner,
                });
            }
            Destroy(gameObject);
        }
    }
}
