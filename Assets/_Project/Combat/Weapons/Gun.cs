using UnityEngine;

namespace DungeonBlade.Combat
{
    public class Gun : WeaponBase
    {
        [Header("Stats")]
        [SerializeField] float damage = 18f;
        [SerializeField] float fireRate = 6f;
        [SerializeField] float range = 60f;
        [SerializeField] int magSize = 12;
        [SerializeField] float reloadTime = 1.6f;

        [Header("Spread")]
        [SerializeField] float hipSpreadDegrees = 3f;
        [SerializeField] float adsSpreadDegrees = 0.4f;

        [Header("Refs")]
        [SerializeField] Transform muzzle;
        [SerializeField] Camera aimCamera;
        [SerializeField] LayerMask hitMask = ~0;

        public int Ammo { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsAiming { get; private set; }

        public System.Action OnFire;
        public System.Action OnHitDamageable;
        public System.Action<int, int> OnAmmoChanged;
        public System.Action<bool> OnAimChanged;

        public override bool IsBusy => false;

        float _nextFireTime;
        float _reloadFinishTime;

        void Awake()
        {
            Ammo = magSize;
        }

        public override void OnEquip()
        {
            base.OnEquip();
            if (aimCamera == null) aimCamera = Camera.main;
            OnAmmoChanged?.Invoke(Ammo, magSize);
        }

        public override void OnUnequip()
        {
            base.OnUnequip();
            CancelReload();
            SetAim(false);
        }

        public override void OnPrimaryPressed() => TryFire();
        public override void OnPrimaryReleased() { }
        public override void OnSecondaryPressed() => SetAim(true);
        public override void OnSecondaryReleased() => SetAim(false);
        public override void OnReloadPressed() => StartReload();

        public override void TickWeapon(float dt)
        {
            if (IsReloading && Time.time >= _reloadFinishTime)
            {
                IsReloading = false;
                Ammo = magSize;
                OnAmmoChanged?.Invoke(Ammo, magSize);
            }
        }

        void TryFire()
        {
            if (IsReloading) return;
            if (Time.time < _nextFireTime) return;
            if (Ammo <= 0)
            {
                StartReload();
                return;
            }

            _nextFireTime = Time.time + 1f / fireRate;
            Ammo--;
            OnAmmoChanged?.Invoke(Ammo, magSize);
            OnFire?.Invoke();

            FireRay();
        }

        void FireRay()
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera == null) return;

            Vector3 origin = aimCamera.transform.position;
            Vector3 dir = aimCamera.transform.forward;

            float spreadDeg = IsAiming ? adsSpreadDegrees : hipSpreadDegrees;
            if (spreadDeg > 0f)
            {
                Quaternion spread = Quaternion.Euler(
                    Random.Range(-spreadDeg, spreadDeg),
                    Random.Range(-spreadDeg, spreadDeg),
                    0f);
                dir = spread * dir;
            }

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform.root)) return;

                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    dmg.ApplyDamage(new DamageInfo
                    {
                        Amount = damage,
                        HitPoint = hit.point,
                        HitDirection = dir,
                        Knockback = 0f,
                        Source = transform.root.gameObject,
                        Type = DamageType.Ranged,
                    });
                    OnHitDamageable?.Invoke();
                }

                Debug.DrawLine(origin, hit.point, Color.yellow, 0.05f);
            }
            else
            {
                Debug.DrawRay(origin, dir * range, Color.yellow, 0.05f);
            }
        }

        void StartReload()
        {
            if (IsReloading || Ammo == magSize) return;
            IsReloading = true;
            _reloadFinishTime = Time.time + reloadTime;
        }

        void CancelReload()
        {
            IsReloading = false;
        }

        void SetAim(bool aim)
        {
            if (IsAiming == aim) return;
            IsAiming = aim;
            OnAimChanged?.Invoke(aim);
        }
    }
}
