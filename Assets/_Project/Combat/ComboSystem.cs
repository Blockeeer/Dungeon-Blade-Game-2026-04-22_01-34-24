using System;
using UnityEngine;

namespace DungeonBlade.Combat
{
    public class ComboSystem : MonoBehaviour
    {
        [SerializeField] float comboTimeoutSeconds = 3f;

        public int CurrentCombo { get; private set; }
        public int BestCombo { get; private set; }
        public float TimeSinceLastHit => Time.time - _lastHitTime;

        public event Action<int> OnComboChanged;
        public event Action<int> OnComboReset;

        float _lastHitTime = -999f;

        void Update()
        {
            if (CurrentCombo > 0 && TimeSinceLastHit > comboTimeoutSeconds)
            {
                ResetCombo();
            }
        }

        public void RegisterHit()
        {
            CurrentCombo++;
            if (CurrentCombo > BestCombo) BestCombo = CurrentCombo;
            _lastHitTime = Time.time;
            OnComboChanged?.Invoke(CurrentCombo);
        }

        public void ResetCombo()
        {
            int last = CurrentCombo;
            CurrentCombo = 0;
            if (last > 0) OnComboReset?.Invoke(last);
        }
    }
}
