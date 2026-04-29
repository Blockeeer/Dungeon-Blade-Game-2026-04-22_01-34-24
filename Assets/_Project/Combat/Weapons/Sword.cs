using System.Collections.Generic;
using UnityEngine;

namespace DungeonBlade.Combat
{
    public class Sword : WeaponBase
    {
        [Header("Light Combo")]
        [SerializeField] float[] lightDamage = { 12f, 14f, 20f };
        [SerializeField] float lightWindup = 0.08f;
        [SerializeField] float lightActive = 0.12f;
        [SerializeField] float lightRecovery = 0.20f;
        [SerializeField] float comboChainWindow = 0.35f;

        [Header("Heavy Attack")]
        [SerializeField] float heavyDamage = 35f;
        [SerializeField] float heavyChargeTime = 0.45f;
        [SerializeField] float heavyWindup = 0.15f;
        [SerializeField] float heavyActive = 0.18f;
        [SerializeField] float heavyRecovery = 0.45f;
        [SerializeField] float heavyKnockback = 6f;

        [Header("Hit Detection")]
        [SerializeField] Transform hitOrigin;
        [SerializeField] float hitRadius = 1.2f;
        [SerializeField] float hitForwardOffset = 1.0f;
        [SerializeField] LayerMask hitMask = ~0;

        [Header("Block / Parry")]
        [SerializeField] float parryWindow = 0.18f;
        [SerializeField] float blockDamageReduction = 0.6f;

        public enum SwordState { Idle, Windup, Active, Recovery, Blocking }

        public SwordState State { get; private set; } = SwordState.Idle;
        public int CurrentComboIndex { get; private set; } = -1;
        public bool IsParryActive => State == SwordState.Blocking && Time.time - _blockStartTime <= parryWindow;
        public float BlockDamageReduction => blockDamageReduction;

        public override bool IsBusy => State == SwordState.Windup || State == SwordState.Active;
        public override bool CanBeCancelled => State == SwordState.Active || State == SwordState.Recovery;

        float _phaseEndTime;
        float _comboExpireTime;
        float _blockStartTime;
        float _heavyHoldStartTime = -1f;
        bool _queuedNextLight;
        readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        public System.Action<int, float> OnHit;
        public System.Action<int> OnSwingStart;

        void Update()
        {
            float now = Time.time;

            switch (State)
            {
                case SwordState.Windup:
                    if (now >= _phaseEndTime) EnterActive();
                    break;
                case SwordState.Active:
                    DoActiveTick();
                    if (now >= _phaseEndTime) EnterRecovery();
                    break;
                case SwordState.Recovery:
                    if (_queuedNextLight)
                    {
                        _queuedNextLight = false;
                        StartLightSwing();
                    }
                    else if (now >= _phaseEndTime)
                    {
                        State = SwordState.Idle;
                    }
                    break;
            }

            if (CurrentComboIndex >= 0 && now > _comboExpireTime && State == SwordState.Idle)
            {
                CurrentComboIndex = -1;
            }
        }

        public override void OnPrimaryPressed()
        {
            if (State == SwordState.Idle)
            {
                StartLightSwing();
            }
            else if (State == SwordState.Active || State == SwordState.Recovery)
            {
                _queuedNextLight = true;
            }
        }

        public override void OnPrimaryReleased() { }

        public override void OnSecondaryPressed()
        {
            if (State == SwordState.Idle)
            {
                State = SwordState.Blocking;
                _blockStartTime = Time.time;
            }
        }

        public override void OnSecondaryReleased()
        {
            if (State == SwordState.Blocking) State = SwordState.Idle;
        }

        public void StartHeavyHold()
        {
            if (_heavyHoldStartTime < 0f) _heavyHoldStartTime = Time.time;
        }

        public void TryReleaseHeavy()
        {
            if (_heavyHoldStartTime < 0f) return;
            float held = Time.time - _heavyHoldStartTime;
            _heavyHoldStartTime = -1f;
            if (held >= heavyChargeTime && State == SwordState.Idle)
            {
                StartHeavySwing();
            }
        }

        public override void OnEquip()
        {
            base.OnEquip();
            State = SwordState.Idle;
            CurrentComboIndex = -1;
            _queuedNextLight = false;
        }

        public override void OnUnequip()
        {
            base.OnUnequip();
            State = SwordState.Idle;
        }

        void StartLightSwing()
        {
            int next = (CurrentComboIndex < 0 || Time.time > _comboExpireTime)
                ? 0
                : Mathf.Min(CurrentComboIndex + 1, lightDamage.Length - 1);

            CurrentComboIndex = next;
            State = SwordState.Windup;
            _phaseEndTime = Time.time + lightWindup;
            _hitThisSwing.Clear();
            OnSwingStart?.Invoke(CurrentComboIndex);
        }

        void StartHeavySwing()
        {
            CurrentComboIndex = -1;
            State = SwordState.Windup;
            _phaseEndTime = Time.time + heavyWindup;
            _hitThisSwing.Clear();
            OnSwingStart?.Invoke(99);
        }

        void EnterActive()
        {
            State = SwordState.Active;
            bool isHeavy = CurrentComboIndex < 0;
            _phaseEndTime = Time.time + (isHeavy ? heavyActive : lightActive);
        }

        void EnterRecovery()
        {
            State = SwordState.Recovery;
            bool isHeavy = CurrentComboIndex < 0;
            _phaseEndTime = Time.time + (isHeavy ? heavyRecovery : lightRecovery);
            _comboExpireTime = Time.time + comboChainWindow + (isHeavy ? heavyRecovery : lightRecovery);
        }

        void DoActiveTick()
        {
            Vector3 origin = (hitOrigin != null ? hitOrigin.position : transform.position)
                             + transform.forward * hitForwardOffset;

            bool isHeavy = CurrentComboIndex < 0;
            float dmg = isHeavy ? heavyDamage : lightDamage[CurrentComboIndex];

            var template = new DamageInfo
            {
                Amount = dmg,
                Knockback = isHeavy ? heavyKnockback : 0f,
                Source = transform.root.gameObject,
                Type = DamageType.Melee,
                IsParryable = true,
            };

            int hits = MeleeHitbox.SphereSweep(origin, hitRadius, hitMask, _hitThisSwing, template);
            if (hits > 0) OnHit?.Invoke(CurrentComboIndex, dmg);
        }

        public void CancelForGunshot()
        {
            if (State == SwordState.Active || State == SwordState.Recovery)
            {
                State = SwordState.Idle;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (hitOrigin == null) return;
            Gizmos.color = Color.red;
            Vector3 c = hitOrigin.position + transform.forward * hitForwardOffset;
            Gizmos.DrawWireSphere(c, hitRadius);
        }
    }
}
