using UnityEngine;

namespace DungeonBlade.Runtime
{
    /// <summary>
    /// Procedural placeholder animation for capsule / primitive characters.
    /// Attach to a character GameObject alongside PlayerController or EnemyBase.
    /// Drives: idle bob, walk bounce, jump squash, dash stretch, slash swing,
    /// hit flash. Hooks Animator events internally so when you swap in a
    /// real model + animator you can just remove this component.
    /// </summary>
    public class PlaceholderCharacterFX : MonoBehaviour
    {
        [Header("Body")]
        public Transform body;                  // scale bobs here
        public Transform leftLeg, rightLeg;     // optional swing targets
        public Transform leftArm, rightArm;
        public Transform swordPivot;            // visible sword mesh to swing
        public Transform gunPivot;              // visible gun mesh
        public Renderer[] tintable;             // flashed white on hit

        [Header("Config")]
        public float walkBobFrequency = 10f;
        public float walkBobAmplitude = 0.06f;
        public float idleBobFrequency = 1.5f;
        public float idleBobAmplitude = 0.03f;
        public float limbSwingAmplitude = 35f;
        public float slashDuration = 0.35f;
        public float hitFlashDuration = 0.12f;
        public Color hitFlashColor = Color.white;

        DungeonBlade.Player.PlayerController pc;
        DungeonBlade.Player.PlayerCombat combat;
        DungeonBlade.Player.PlayerStats stats;

        float walkPhase;
        float slashTimer;
        float hitFlashTimer;
        Color[] originalColors;
        Vector3 bodyOriginalLocal;

        void Awake()
        {
            pc = GetComponent<DungeonBlade.Player.PlayerController>();
            combat = GetComponent<DungeonBlade.Player.PlayerCombat>();
            stats = GetComponent<DungeonBlade.Player.PlayerStats>();
            if (body != null) bodyOriginalLocal = body.localPosition;

            if (tintable != null)
            {
                originalColors = new Color[tintable.Length];
                for (int i = 0; i < tintable.Length; i++)
                    if (tintable[i] != null && tintable[i].material.HasProperty("_Color"))
                        originalColors[i] = tintable[i].material.color;
            }

            if (stats != null) stats.OnDamaged.AddListener(_ => hitFlashTimer = hitFlashDuration);
            if (combat != null)
            {
                combat.OnComboStep.AddListener(_ => slashTimer = slashDuration);
                combat.OnHeavyAttack.AddListener(() => slashTimer = slashDuration * 1.2f);
                combat.OnWeaponSwitched.AddListener(OnWeaponSwitched);
                OnWeaponSwitched(combat.CurrentWeapon);
            }
        }

        void OnWeaponSwitched(DungeonBlade.Player.PlayerCombat.WeaponMode m)
        {
            if (swordPivot != null) swordPivot.gameObject.SetActive(m == DungeonBlade.Player.PlayerCombat.WeaponMode.Sword);
            if (gunPivot != null)   gunPivot.gameObject.SetActive(m == DungeonBlade.Player.PlayerCombat.WeaponMode.Gun);
        }

        void LateUpdate()
        {
            if (body == null) return;
            float dt = Time.deltaTime;

            // Locomotion
            bool moving = pc != null && new Vector2(pc.Velocity.x, pc.Velocity.z).sqrMagnitude > 0.5f;
            bool grounded = pc != null ? pc.IsGrounded : true;

            if (moving && grounded)
            {
                walkPhase += walkBobFrequency * dt;
                float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * walkBobAmplitude;
                body.localPosition = bodyOriginalLocal + Vector3.up * bob;

                float swing = Mathf.Sin(walkPhase) * limbSwingAmplitude;
                if (leftLeg)  leftLeg.localRotation  = Quaternion.Euler( swing, 0, 0);
                if (rightLeg) rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
                if (leftArm)  leftArm.localRotation  = Quaternion.Euler(-swing * 0.6f, 0, 0);
                if (rightArm) rightArm.localRotation = Quaternion.Euler( swing * 0.6f, 0, 0);
            }
            else
            {
                float bob = Mathf.Sin(Time.time * idleBobFrequency) * idleBobAmplitude;
                body.localPosition = Vector3.Lerp(body.localPosition, bodyOriginalLocal + Vector3.up * bob, 10 * dt);
                if (leftLeg)  leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, 10 * dt);
                if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, 10 * dt);
                if (leftArm)  leftArm.localRotation  = Quaternion.Slerp(leftArm.localRotation, Quaternion.identity, 10 * dt);
                if (rightArm) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, 10 * dt);
            }

            // Jump / airborne squash-stretch
            if (pc != null && !pc.IsGrounded)
            {
                float vy = pc.Velocity.y;
                float stretch = 1f + Mathf.Clamp(vy * 0.02f, -0.15f, 0.2f);
                body.localScale = Vector3.Lerp(body.localScale, new Vector3(1f / stretch, stretch, 1f / stretch), 8 * dt);
            }
            else
            {
                body.localScale = Vector3.Lerp(body.localScale, Vector3.one, 10 * dt);
            }

            // Dash stretch forward
            if (pc != null && pc.IsDashing && body != null)
            {
                body.localScale = Vector3.Lerp(body.localScale, new Vector3(0.8f, 0.9f, 1.35f), 15 * dt);
            }

            // Slash swing
            if (slashTimer > 0f && swordPivot != null)
            {
                slashTimer -= dt;
                float t = 1f - (slashTimer / slashDuration);
                float angle = Mathf.Lerp(-60f, 60f, t);
                swordPivot.localRotation = Quaternion.Euler(angle, 0, 0);
            }
            else if (swordPivot != null)
            {
                swordPivot.localRotation = Quaternion.Slerp(swordPivot.localRotation, Quaternion.identity, 10 * dt);
            }

            // Hit flash
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= dt;
                float alpha = hitFlashTimer / hitFlashDuration;
                if (tintable != null)
                {
                    for (int i = 0; i < tintable.Length; i++)
                    {
                        if (tintable[i] == null || originalColors == null || i >= originalColors.Length) continue;
                        if (!tintable[i].material.HasProperty("_Color")) continue;
                        tintable[i].material.color = Color.Lerp(originalColors[i], hitFlashColor, alpha);
                    }
                }
            }
        }
    }
}
