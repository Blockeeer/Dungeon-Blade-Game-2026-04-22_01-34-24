using System;
using DungeonBlade.Combat;
using DungeonBlade.Enemies;
using UnityEngine;

namespace DungeonBlade.Boss
{
    public enum BossPhase { Dormant, Phase1, Transition, Phase2, Phase3, Dead }

    public abstract class BossBase : EnemyBase
    {
        [Header("Boss Phases")]
        [Range(0f, 1f)] [SerializeField] protected float phase2Threshold = 0.66f;
        [Range(0f, 1f)] [SerializeField] protected float phase3Threshold = 0.33f;
        [SerializeField] protected float transitionDuration = 1.2f;
        [SerializeField] protected float transitionDamageReduction = 0.9f;
        [SerializeField] protected Color transitionFlashColor = new Color(0.6f, 0.2f, 1f);

        public BossPhase Phase { get; protected set; } = BossPhase.Dormant;
        public event Action<BossPhase> OnPhaseChanged;
        public event Action<float, float> OnBossHealthChanged;
        public event Action OnBossDefeated;

        protected float TransitionEndTime;
        bool _started;

        protected override void Awake()
        {
            base.Awake();
            Agent.enabled = false;
        }

        public virtual void ActivateBoss()
        {
            if (_started) return;
            _started = true;
            Agent.enabled = true;
            EnterPhase(BossPhase.Phase1);
            Debug.Log($"[Boss] {name} activated.");
        }

        public override void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive) return;

            if (Phase == BossPhase.Dormant)
            {
                Debug.Log($"[Boss] {name} ignored damage (dormant).");
                return;
            }

            float original = Health;
            float incoming = info.Amount;

            if (Phase == BossPhase.Transition)
            {
                incoming *= 1f - Mathf.Clamp01(transitionDamageReduction);
            }

            var scaled = info;
            scaled.Amount = incoming;
            base.ApplyDamage(scaled);

            OnBossHealthChanged?.Invoke(Mathf.Max(0f, Health), Stats.MaxHealth);

            if (IsAlive) CheckPhaseTransition(original);
        }

        void CheckPhaseTransition(float prevHealth)
        {
            float hpNorm = Health / Stats.MaxHealth;
            float prevNorm = prevHealth / Stats.MaxHealth;

            if (Phase == BossPhase.Phase1 && hpNorm <= phase2Threshold && prevNorm > phase2Threshold)
            {
                StartTransitionTo(BossPhase.Phase2);
            }
            else if (Phase == BossPhase.Phase2 && hpNorm <= phase3Threshold && prevNorm > phase3Threshold)
            {
                StartTransitionTo(BossPhase.Phase3);
            }
        }

        protected void StartTransitionTo(BossPhase next)
        {
            Debug.Log($"[Boss] {name} transitioning → {next}");
            Phase = BossPhase.Transition;
            TransitionEndTime = Time.time + transitionDuration;
            OnEnterTransition(next);
            _pendingNextPhase = next;
        }

        BossPhase _pendingNextPhase;

        protected override void UpdateState()
        {
            if (Phase == BossPhase.Dormant || Phase == BossPhase.Dead) return;

            if (Phase == BossPhase.Transition)
            {
                if (Time.time >= TransitionEndTime) EnterPhase(_pendingNextPhase);
                return;
            }

            UpdateBossPhase();
        }

        protected abstract void UpdateBossPhase();

        void EnterPhase(BossPhase next)
        {
            Phase = next;
            OnEnterPhase(next);
            OnPhaseChanged?.Invoke(next);
        }

        protected virtual void OnEnterPhase(BossPhase p) { }
        protected virtual void OnEnterTransition(BossPhase nextPhase) { }

        protected override void Die()
        {
            Phase = BossPhase.Dead;
            OnBossDefeated?.Invoke();
            base.Die();
        }

        protected override Transform FindPlayer()
        {
            if (Phase == BossPhase.Dormant) return null;
            return base.FindPlayer();
        }

        public void ForceAcquireTarget(Transform t)
        {
            AcquireTarget(t);
        }
    }
}
