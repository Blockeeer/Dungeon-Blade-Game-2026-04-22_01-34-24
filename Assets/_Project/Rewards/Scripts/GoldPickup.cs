using DungeonBlade.Bank;
using UnityEngine;

namespace DungeonBlade.Rewards
{
    [RequireComponent(typeof(Collider))]
    public class GoldPickup : MonoBehaviour
    {
        [SerializeField] int amount = 1;
        [SerializeField] float magnetRange = 4f;
        [SerializeField] float magnetSpeed = 12f;
        [SerializeField] float pickupRange = 0.6f;
        [SerializeField] float spinSpeed = 180f;

        Transform _player;

        void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        public void Initialize(int goldAmount)
        {
            amount = Mathf.Max(1, goldAmount);
        }

        void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (_player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _player = go.transform;
                if (_player == null) return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= pickupRange)
            {
                Collect();
                return;
            }
            if (dist <= magnetRange)
            {
                transform.position = Vector3.MoveTowards(transform.position, _player.position + Vector3.up * 0.5f, magnetSpeed * Time.deltaTime);
            }
        }

        void Collect()
        {
            if (PlayerWallet.Instance != null)
            {
                PlayerWallet.Instance.Add(amount);
                Debug.Log($"[Gold] +{amount}g (Pocket: {PlayerWallet.Instance.Gold}g)");
            }
            Destroy(gameObject);
        }
    }
}
