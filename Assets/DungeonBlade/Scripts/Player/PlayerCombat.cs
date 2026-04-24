using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DungeonBlade.Combat;

namespace DungeonBlade.Player
{
    /// <summary>
    /// Hybrid sword + gun combat per GDD 2.2.
    /// - 3-hit sword combo (LMB taps)
    /// - Heavy attack (LMB hold)
    /// - Dash-attack (dash + heavy release)
    /// - Block/Parry (RMB hold; parry window on first 0.2s)
    /// - Gun fire (LMB while gun equipped), reload (R), ADS (RMB)
    /// - K-style cancel: firing gun while sword swing is recovering cancels into fire
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        public enum WeaponMode { Sword, Gun }

        [Header("Refs")]
        public Transform cameraTransform;
        public ComboCounter comboCounter;
        public Animator animator;

        [Header("Sword Stats")]
        public float swordRange = 2.5f;
        public float swordConeHalfAngle = 55f;       // degrees
        public float[] comboDamage = { 18f, 22f, 34f };
        public float[] comboTiming = { 0.28f, 0.28f, 0.42f };
        public float comboWindow = 0.6f;             // max gap between hits to continue combo
        public Vector3 swordKnockback = new Vector3(0, 3, 5);
        public float heavyChargeTime = 0.5f;
        public float heavyDamage = 55f;
        public float heavyStaminaCost = 25f;
        public float dashAttackDamageBonus = 1.5f;

        [Header("Block / Parry")]
        public float parryWindow = 0.2f;
        public float parryStaggerDuration = 1.2f;

        [Header("Gun Stats (default)")]
        public float gunRange = 60f;
        public float gunDamage = 25f;
        public float gunFireRate = 8f;              // rounds / sec
        public int   gunMagSize = 12;
        public int   gunReserveAmmo = 60;
        public float gunReloadTime = 1.4f;
        public float gunSpreadHip = 2.0f;           // deg
        public float gunSpreadAds = 0.4f;
        public LayerMask gunHitMask = ~0;

        [Header("K-Style")]
        public float slashToFireCancelWindow = 0.35f;  // seconds after slash where fire cancels recovery
        public float fireToSlashCancelWindow = 0.25f;

        [Header("Events")]
        public UnityEvent<int> OnComboStep;         // combo step 1..3
        public UnityEvent OnHeavyAttack;
        public UnityEvent OnParrySuccess;
        public UnityEvent<int, int> OnAmmoChanged;  // (current, reserve)
        public UnityEvent<WeaponMode> OnWeaponSwitched;

        // ─────── Runtime state ───────
        public WeaponMode CurrentWeapon { get; private set; } = WeaponMode.Sword;
        public int CurrentMagAmmo { get; private set; }
        public int CurrentReserveAmmo { get; private set; }
        public bool IsBlocking { get; private set; }
        public bool IsAimingDownSights { get; private set; }
        public bool IsReloading { get; private set; }
        public float LastSlashTime { get; private set; } = -99f;
        public float LastGunFireTime { get; private set; } = -99f;

        PlayerController controller;
        PlayerStats stats;

        int comboStep;
        float lastSwordHitTime = -99f;
        float swordRecoveryEndsAt;
        float heavyHoldStart = -1f;
        bool heavyQueued;
        float nextGunFireTime;
        float reloadEndTime;
        float blockStartTime = -1f;

        readonly Collider[] hitBuffer = new Collider[16];

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            stats = GetComponent<PlayerStats>();
            if (cameraTransform == null && controller.cameraPivot != null)
                cameraTransform = controller.cameraPivot;
            CurrentMagAmmo = gunMagSize;
            CurrentReserveAmmo = gunReserveAmmo;
        }

        void Update()
        {
            if (stats.IsDead) return;

            HandleWeaponSwitch();
            HandleBlocking();
            HandleAds();
            HandleReload();

            if (CurrentWeapon == WeaponMode.Sword) HandleSwordInput();
            else HandleGunInput();

            // Combo drop if timing exceeded
            if (comboStep > 0 && Time.time - lastSwordHitTime > comboWindow)
                comboStep = 0;
        }

        // ─────── Weapon switching ───────
        void HandleWeaponSwitch()
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.mouseScrollDelta.y != 0f)
            {
                SetWeapon(CurrentWeapon == WeaponMode.Sword ? WeaponMode.Gun : WeaponMode.Sword);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetWeapon(WeaponMode.Sword);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetWeapon(WeaponMode.Gun);
        }

        public void SetWeapon(WeaponMode mode)
        {
            if (mode == CurrentWeapon) return;
            CurrentWeapon = mode;
            IsAimingDownSights = false;
            IsBlocking = false;
            OnWeaponSwitched?.Invoke(mode);
            if (animator != null) animator.SetInteger("WeaponMode", (int)mode);
        }

        // ─────── Sword ───────
        void HandleSwordInput()
        {
            // Heavy charge (hold LMB)
            if (Input.GetMouseButton(0))
            {
                if (heavyHoldStart < 0f) heavyHoldStart = Time.time;
                if (Time.time - heavyHoldStart >= heavyChargeTime && !heavyQueued)
                {
                    heavyQueued = true;
                }
            }

            // LMB released
            if (Input.GetMouseButtonUp(0))
            {
                float held = heavyHoldStart > 0f ? (Time.time - heavyHoldStart) : 0f;
                heavyHoldStart = -1f;

                if (heavyQueued)
                {
                    heavyQueued = false;
                    TryHeavyAttack();
                }
                else if (held < heavyChargeTime)
                {
                    TryLightCombo();
                }
            }
        }

        void TryLightCombo()
        {
            // Respect combo recovery, but K-style cancel window allows quick re-enter
            if (Time.time < swordRecoveryEndsAt && (Time.time - LastSlashTime) > slashToFireCancelWindow)
                return;

            comboStep = Mathf.Clamp(comboStep + 1, 1, 3);
            int idx = comboStep - 1;

            float dmg = comboDamage[idx];
            float recovery = comboTiming[idx];
            ExecuteSwordSwing(dmg, recovery, isHeavy:false);

            OnComboStep?.Invoke(comboStep);
            if (comboStep >= 3) comboStep = 0;   // reset after finisher
        }

        void TryHeavyAttack()
        {
            if (!stats.TrySpendStamina(heavyStaminaCost)) return;
            float dmg = heavyDamage;
            // Dash attack bonus
            if (controller.IsDashing) dmg *= dashAttackDamageBonus;
            ExecuteSwordSwing(dmg, 0.55f, isHeavy:true);
            OnHeavyAttack?.Invoke();
        }

        void ExecuteSwordSwing(float damage, float recovery, bool isHeavy)
        {
            LastSlashTime = Time.time;
            lastSwordHitTime = Time.time;
            swordRecoveryEndsAt = Time.time + recovery;

            if (animator != null)
            {
                animator.SetTrigger(isHeavy ? "HeavySlash" : "LightSlash");
                animator.SetInteger("ComboStep", comboStep);
            }

            // Cone hit detection
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            int count = Physics.OverlapSphereNonAlloc(origin, swordRange, hitBuffer);
            HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

            for (int i = 0; i < count; i++)
            {
                var c = hitBuffer[i];
                if (c == null || c.transform.root == transform) continue;
                Vector3 dir = c.transform.position - origin;
                dir.y = 0f;
                if (dir.magnitude > swordRange) continue;
                float angle = Vector3.Angle(transform.forward, dir.normalized);
                if (angle > swordConeHalfAngle) continue;

                var target = c.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead) continue;
                if (!alreadyHit.Add(target)) continue;

                Vector3 knock = transform.forward * swordKnockback.z + Vector3.up * swordKnockback.y;

                target.TakeDamage(new DamageInfo {
                    amount = damage,
                    source = DamageSource.PlayerMelee,
                    knockback = knock,
                    hitPoint = c.ClosestPoint(origin),
                    attacker = gameObject,
                    isHeavy = isHeavy,
                });

                comboCounter?.RegisterHit();
            }
        }

        // ─────── Block / Parry ───────
        void HandleBlocking()
        {
            if (CurrentWeapon != WeaponMode.Sword)
            {
                IsBlocking = false;
                return;
            }
            if (Input.GetMouseButtonDown(1))
            {
                IsBlocking = true;
                blockStartTime = Time.time;
            }
            if (Input.GetMouseButtonUp(1))
            {
                IsBlocking = false;
                blockStartTime = -1f;
            }
            if (animator != null) animator.SetBool("Blocking", IsBlocking);
        }

        /// <summary>
        /// Called externally (by EnemyBase) when the enemy actually lands a hit on the player.
        /// Returns true if the hit was parried.
        /// </summary>
        public bool TryParry(IDamageable attackerDamageable, GameObject attacker)
        {
            if (!IsBlocking) return false;
            if (Time.time - blockStartTime > parryWindow) return false;
            OnParrySuccess?.Invoke();
            // Stagger attacker
            if (attacker != null && attacker.TryGetComponent<DungeonBlade.Enemies.EnemyBase>(out var e))
                e.Stagger(parryStaggerDuration);
            return true;
        }

        // ─────── Gun ───────
        void HandleGunInput()
        {
            // Fire (LMB held or tapped, depending on weapon type — here semi-auto)
            bool fireInput = Input.GetMouseButton(0);
            if (fireInput && !IsReloading && Time.time >= nextGunFireTime && CurrentMagAmmo > 0)
            {
                FireGunOnce();
            }
            else if (fireInput && CurrentMagAmmo <= 0 && !IsReloading)
            {
                BeginReload();
            }
        }

        void HandleAds()
        {
            if (CurrentWeapon != WeaponMode.Gun) { IsAimingDownSights = false; return; }
            IsAimingDownSights = Input.GetMouseButton(1);
            if (animator != null) animator.SetBool("ADS", IsAimingDownSights);
        }

        void HandleReload()
        {
            if (CurrentWeapon != WeaponMode.Gun) return;
            if (!IsReloading && Input.GetKeyDown(KeyCode.R) && CurrentMagAmmo < gunMagSize && CurrentReserveAmmo > 0)
                BeginReload();

            if (IsReloading && Time.time >= reloadEndTime)
                FinishReload();
        }

        void BeginReload()
        {
            IsReloading = true;
            reloadEndTime = Time.time + gunReloadTime;
            if (animator != null) animator.SetTrigger("Reload");
        }

        void FinishReload()
        {
            IsReloading = false;
            int needed = gunMagSize - CurrentMagAmmo;
            int loaded = Mathf.Min(needed, CurrentReserveAmmo);
            CurrentMagAmmo += loaded;
            CurrentReserveAmmo -= loaded;
            OnAmmoChanged?.Invoke(CurrentMagAmmo, CurrentReserveAmmo);
        }

        void FireGunOnce()
        {
            // K-style: firing cancels sword recovery
            bool cancelSlashRecovery = (Time.time - LastSlashTime) < slashToFireCancelWindow;
            if (cancelSlashRecovery) swordRecoveryEndsAt = 0f;

            LastGunFireTime = Time.time;
            nextGunFireTime = Time.time + (1f / gunFireRate);
            CurrentMagAmmo--;
            OnAmmoChanged?.Invoke(CurrentMagAmmo, CurrentReserveAmmo);
            if (animator != null) animator.SetTrigger("Fire");

            // Raycast from camera through crosshair
            Vector3 origin = cameraTransform != null ? cameraTransform.position : transform.position + Vector3.up * 1.5f;
            Vector3 fwd = cameraTransform != null ? cameraTransform.forward : transform.forward;

            float spread = IsAimingDownSights ? gunSpreadAds : gunSpreadHip;
            spread *= Mathf.Deg2Rad;
            Vector3 dir = Quaternion.Euler(
                Random.Range(-spread, spread) * Mathf.Rad2Deg,
                Random.Range(-spread, spread) * Mathf.Rad2Deg,
                0) * fwd;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, gunRange, gunHitMask, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && !target.IsDead)
                {
                    target.TakeDamage(new DamageInfo {
                        amount = gunDamage,
                        source = DamageSource.PlayerRanged,
                        knockback = dir.normalized * 2f,
                        hitPoint = hit.point,
                        attacker = gameObject,
                    });
                    comboCounter?.RegisterHit();
                }
            }
        }

        public void AddReserveAmmo(int amount)
        {
            CurrentReserveAmmo = Mathf.Min(CurrentReserveAmmo + amount, 9999);
            OnAmmoChanged?.Invoke(CurrentMagAmmo, CurrentReserveAmmo);
        }
    }
}
