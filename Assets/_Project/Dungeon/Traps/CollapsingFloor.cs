using UnityEngine;

namespace DungeonBlade.Dungeon
{
    public class StandDetectorRelay : MonoBehaviour
    {
        public CollapsingFloor Owner;

        void OnTriggerEnter(Collider other)
        {
            if (Owner == null) return;
            if (other.GetComponentInParent<Player.PlayerStats>() == null) return;
            Owner.OnPlayerStanding(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (Owner == null) return;
            if (other.GetComponentInParent<Player.PlayerStats>() == null) return;
            Owner.OnPlayerStanding(false);
        }
    }

    [RequireComponent(typeof(Collider))]
    public class CollapsingFloor : MonoBehaviour
    {
        [SerializeField] float secondsBeforeCollapse = 1.2f;
        [SerializeField] float fallDuration = 0.5f;
        [SerializeField] float fallDistance = 30f;
        [SerializeField] float resetDelay = 5f;
        [SerializeField] float warningShakeAmount = 0.05f;
        [Tooltip("Instant drop applied to visual on collapse to clear the player's feet.")]
        [SerializeField] float instantDropOnCollapse = 2f;

        [SerializeField] Transform visual;
        [Tooltip("Trigger volume that detects the player standing on the floor. Must be a child of this object and set to isTrigger.")]
        [SerializeField] Collider standDetector;

        Vector3 _initialVisualPos;
        Collider _solidCollider;
        float _standStartTime = -1f;
        float _collapseStartTime = -1f;
        float _resetTime = -1f;
        bool _isCollapsing;
        bool _hasCollapsed;
        StandDetectorRelay _relay;

        void Awake()
        {
            _solidCollider = GetComponent<Collider>();
            if (visual != null) _initialVisualPos = visual.localPosition;

            if (standDetector != null)
            {
                standDetector.isTrigger = true;
                _relay = standDetector.gameObject.GetComponent<StandDetectorRelay>();
                if (_relay == null) _relay = standDetector.gameObject.AddComponent<StandDetectorRelay>();
                _relay.Owner = this;
            }
        }

        internal void OnPlayerStanding(bool standing)
        {
            if (_hasCollapsed || _isCollapsing) return;
            if (standing)
            {
                if (_standStartTime < 0f) _standStartTime = Time.time;
            }
            else
            {
                _standStartTime = -1f;
                if (visual != null) visual.localPosition = _initialVisualPos;
            }
        }

        void StartCollapse()
        {
            _isCollapsing = true;
            _collapseStartTime = Time.time;
            _solidCollider.enabled = false;
            _hasCollapsed = true;
            _resetTime = Time.time + fallDuration + resetDelay;

            if (visual != null)
            {
                visual.localPosition = _initialVisualPos + Vector3.down * instantDropOnCollapse;
            }
        }

        void Update()
        {
            if (_standStartTime > 0f && !_isCollapsing && !_hasCollapsed)
            {
                float elapsed = Time.time - _standStartTime;
                if (visual != null && elapsed > 0.05f)
                {
                    visual.localPosition = _initialVisualPos + Random.insideUnitSphere * warningShakeAmount;
                }
                if (elapsed >= secondsBeforeCollapse) StartCollapse();
            }

            if (_isCollapsing && visual != null)
            {
                float t = Mathf.Clamp01((Time.time - _collapseStartTime) / fallDuration);
                visual.localPosition = _initialVisualPos + Vector3.down * (fallDistance * t);
                if (t >= 1f) _isCollapsing = false;
            }

            if (_hasCollapsed && _resetTime > 0f && Time.time >= _resetTime)
            {
                if (visual != null) visual.localPosition = _initialVisualPos;
                _solidCollider.enabled = true;
                _hasCollapsed = false;
                _isCollapsing = false;
                _standStartTime = -1f;
                _collapseStartTime = -1f;
                _resetTime = -1f;
            }
        }
    }
}
