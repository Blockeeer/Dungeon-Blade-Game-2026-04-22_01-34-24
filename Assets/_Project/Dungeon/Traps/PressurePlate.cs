using System;
using UnityEngine;

namespace DungeonBlade.Dungeon
{
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] float repressDelay = 0.5f;
        [SerializeField] Transform plateVisual;
        [SerializeField] float pressedDownY = -0.05f;
        [SerializeField] float restY = 0f;

        public event Action OnPressed;

        float _lastPressTime = -999f;
        bool _occupied;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Player.PlayerStats>() == null) return;
            _occupied = true;
            UpdateVisual();

            if (Time.time - _lastPressTime < repressDelay) return;
            _lastPressTime = Time.time;
            Debug.Log($"[PressurePlate] {name} pressed");
            OnPressed?.Invoke();
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<Player.PlayerStats>() == null) return;
            _occupied = false;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            if (plateVisual == null) return;
            Vector3 p = plateVisual.localPosition;
            p.y = _occupied ? pressedDownY : restY;
            plateVisual.localPosition = p;
        }
    }
}
