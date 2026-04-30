using UnityEngine;
using UnityEngine.Events;

namespace DungeonBlade.Combat
{
    /// <summary>
    /// GunZ-style combo tracker. Counter rises as hits land, resets after timeout.
    /// Bonus XP awarded per 10-hit milestone per GDD 9.2.
    /// </summary>
    public class ComboCounter : MonoBehaviour
    {
        public float comboTimeout = 3f;       // seconds with no hit resets combo
        public int bonusXpEveryNHits = 10;
        public int bonusXpAmount = 25;

        public int CurrentCombo { get; private set; }
        public int MaxComboThisRun { get; private set; }

        public UnityEvent<int> OnComboChanged;
        public UnityEvent<int> OnComboMilestone;     // fired at each 10-hit step
        public UnityEvent<int> OnComboBroken;

        float lastHitTime = -99f;
        int pendingBonusMilestones;

        void Update()
        {
            if (CurrentCombo > 0 && Time.time - lastHitTime > comboTimeout)
                BreakCombo();
        }

        public void RegisterHit()
        {
            CurrentCombo++;
            lastHitTime = Time.time;
            if (CurrentCombo > MaxComboThisRun) MaxComboThisRun = CurrentCombo;
            OnComboChanged?.Invoke(CurrentCombo);

            if (CurrentCombo % bonusXpEveryNHits == 0)
            {
                pendingBonusMilestones++;
                OnComboMilestone?.Invoke(CurrentCombo);
            }
        }

        public void BreakCombo()
        {
            int ended = CurrentCombo;
            CurrentCombo = 0;
            OnComboBroken?.Invoke(ended);
            OnComboChanged?.Invoke(0);
        }

        public int ConsumePendingBonusXp()
        {
            int result = pendingBonusMilestones * bonusXpAmount;
            pendingBonusMilestones = 0;
            return result;
        }

        public void ResetForNewRun()
        {
            CurrentCombo = 0;
            MaxComboThisRun = 0;
            pendingBonusMilestones = 0;
        }
    }
}
