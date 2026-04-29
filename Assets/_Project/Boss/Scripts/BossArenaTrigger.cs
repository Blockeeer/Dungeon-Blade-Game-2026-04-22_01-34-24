using UnityEngine;

namespace DungeonBlade.Boss
{
    [RequireComponent(typeof(Collider))]
    public class BossArenaTrigger : MonoBehaviour
    {
        [SerializeField] BossBase boss;
        [SerializeField] GameObject arenaSealWall;
        [SerializeField] string playerTag = "Player";
        [SerializeField] float sealDelay = 2.5f;

        bool _triggered;
        float _sealAtTime = -1f;

        void Awake()
        {
            if (arenaSealWall != null) arenaSealWall.SetActive(false);
            if (boss != null) boss.OnBossDefeated += OnBossDefeated;
        }

        void Update()
        {
            if (_sealAtTime > 0f && Time.time >= _sealAtTime)
            {
                _sealAtTime = -1f;
                if (arenaSealWall != null) arenaSealWall.SetActive(true);
                Debug.Log("[Boss] Arena sealed — fight begins.");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag(playerTag)) return;

            _triggered = true;
            Debug.Log($"[Boss] Player entered arena — sealing in {sealDelay:F1}s.");

            _sealAtTime = Time.time + sealDelay;
            if (boss != null) boss.ActivateBoss();
        }

        void OnBossDefeated()
        {
            Debug.Log("[Boss] Defeated — arena unsealed.");
            if (arenaSealWall != null) arenaSealWall.SetActive(false);
        }

        void OnDestroy()
        {
            if (boss != null) boss.OnBossDefeated -= OnBossDefeated;
        }

        void OnDrawGizmosSelected()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;
            Gizmos.color = new Color(1f, 0f, 0.5f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box) Gizmos.DrawCube(box.center, box.size);
        }
    }
}
