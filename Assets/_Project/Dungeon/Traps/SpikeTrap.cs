using System.Collections.Generic;
using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class SpikeTrap : MonoBehaviour
    {
        [SerializeField] float damagePerHit = 15f;
        [SerializeField] float activeDuration = 0.5f;
        [SerializeField] float restDuration = 1.5f;
        [SerializeField] float warningDuration = 0.3f;

        [SerializeField] Transform spikesVisual;
        [SerializeField] float spikesUpY = 0.5f;
        [SerializeField] float spikesDownY = -0.4f;

        [Header("Color Feedback (auto — uses spikesVisual's renderer)")]
        [SerializeField] Color restColor = Color.gray;
        [SerializeField] Color warningColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] Color activeColor = Color.red;
        Renderer _spikesRenderer;
        MaterialPropertyBlock _mpb;
        static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int s_ColorId = Shader.PropertyToID("_Color");

        enum State { Resting, Warning, Active }

        State _state = State.Resting;
        float _phaseEnd;
        readonly HashSet<IDamageable> _hitThisCycle = new HashSet<IDamageable>();
        readonly Collider[] _buffer = new Collider[8];
        Collider _trigger;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void Awake()
        {
            _trigger = GetComponent<Collider>();
            _phaseEnd = Time.time + restDuration;
            if (spikesVisual != null) _spikesRenderer = spikesVisual.GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            UpdateVisual();
        }

        void Update()
        {
            if (Time.time < _phaseEnd) return;

            switch (_state)
            {
                case State.Resting:
                    _state = State.Warning;
                    _phaseEnd = Time.time + warningDuration;
                    break;
                case State.Warning:
                    _state = State.Active;
                    _phaseEnd = Time.time + activeDuration;
                    _hitThisCycle.Clear();
                    DealDamageInTrigger();
                    break;
                case State.Active:
                    _state = State.Resting;
                    _phaseEnd = Time.time + restDuration;
                    break;
            }

            UpdateVisual();
        }

        void DealDamageInTrigger()
        {
            if (_trigger is BoxCollider box)
            {
                Vector3 worldCenter = transform.TransformPoint(box.center);
                Vector3 worldHalf = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
                int count = Physics.OverlapBoxNonAlloc(worldCenter, worldHalf, _buffer, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
                ApplyDamageToHits(count);
            }
            else
            {
                Bounds b = _trigger.bounds;
                int count = Physics.OverlapBoxNonAlloc(b.center, b.extents, _buffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                ApplyDamageToHits(count);
            }
        }

        void ApplyDamageToHits(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var col = _buffer[i];
                if (col == null) continue;
                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;
                if (!_hitThisCycle.Add(dmg)) continue;

                dmg.ApplyDamage(new DamageInfo
                {
                    Amount = damagePerHit,
                    HitPoint = col.ClosestPoint(transform.position),
                    HitDirection = Vector3.up,
                    Source = gameObject,
                    Type = DamageType.Environmental,
                });
            }
        }

        void UpdateVisual()
        {
            if (spikesVisual == null) return;

            Vector3 local = spikesVisual.localPosition;
            local.y = _state == State.Active ? spikesUpY : spikesDownY;
            spikesVisual.localPosition = local;

            if (_spikesRenderer == null) return;

            Color target = _state switch
            {
                State.Warning => warningColor,
                State.Active => activeColor,
                _ => restColor,
            };

            _spikesRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(s_BaseColorId, target);
            _mpb.SetColor(s_ColorId, target);
            _spikesRenderer.SetPropertyBlock(_mpb);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = _state switch
            {
                State.Active => Color.red,
                State.Warning => Color.yellow,
                _ => new Color(0.5f, 0.5f, 0.5f, 0.5f),
            };

            var col = GetComponent<Collider>();
            if (col != null) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
