using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : MonoBehaviour
    {
        [SerializeField] float damage = 12f;
        [SerializeField] float lifetime = 4f;
        [SerializeField] LayerMask hitMask = ~0;

        Rigidbody _rb;
        GameObject _source;
        float _expireTime;
        bool _consumed;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Launch(Vector3 velocity, float damageAmount, float life, GameObject source, LayerMask mask)
        {
            damage = damageAmount;
            lifetime = life;
            _source = source;
            hitMask = mask;
            transform.rotation = Quaternion.LookRotation(velocity.sqrMagnitude > 0.0001f ? velocity : Vector3.forward);
            _rb.velocity = velocity;
            _expireTime = Time.time + lifetime;
        }

        void Update()
        {
            if (Time.time >= _expireTime) Destroy(gameObject);
            if (_rb.velocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(_rb.velocity);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            TryHit(other, fromTrigger: true);
        }

        void OnCollisionEnter(Collision collision)
        {
            TryHit(collision.collider, fromTrigger: false);
        }

        void TryHit(Collider other, bool fromTrigger)
        {
            if (_consumed) return;
            if (_source != null && other.transform.IsChildOf(_source.transform)) return;
            if (((1 << other.gameObject.layer) & hitMask) == 0) return;

            var dmg = other.GetComponentInParent<IDamageable>();

            if (fromTrigger && dmg == null) return;

            if (dmg != null && dmg.IsAlive)
            {
                var info = new DamageInfo
                {
                    Amount = damage,
                    HitPoint = transform.position,
                    HitDirection = transform.forward,
                    Source = _source,
                    Type = DamageType.Ranged,
                    IsParryable = false,
                };
                dmg.ApplyDamage(info);
            }

            _consumed = true;
            Destroy(gameObject);
        }
    }
}
