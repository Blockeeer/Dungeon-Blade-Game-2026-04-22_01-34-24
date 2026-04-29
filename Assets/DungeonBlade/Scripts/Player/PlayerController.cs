using UnityEngine;

namespace DungeonBlade.Player
{
    /// <summary>
    /// Full movement controller implementing GDD section 2.1.
    /// WASD move, single/double jump, dash, wall-run (1-2s),
    /// bunny-hop momentum preservation, and slide.
    /// Uses CharacterController for predictable collision.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // ─────── Tunables ───────
        [Header("Ground Movement")]
        public float walkSpeed = 6f;
        public float runSpeed = 9f;
        public float crouchSpeed = 3f;
        public float groundAccel = 60f;
        public float groundFriction = 10f;

        [Header("Air Movement")]
        public float airAccel = 15f;
        public float airMaxSpeed = 9f;
        public float airControl = 0.35f;
        public float bunnyHopWindow = 0.18f;   // grace window to chain jumps
        public float bunnyHopBoost = 1.10f;    // per successful bhop

        [Header("Jump")]
        public float jumpVelocity = 8.5f;
        public float doubleJumpVelocity = 7.5f;
        public float gravity = 22f;
        public float terminalVelocity = -40f;

        [Header("Dash")]
        public float dashSpeed = 20f;
        public float dashDuration = 0.18f;
        public float dashCooldown = 0.45f;
        public int maxDashCharges = 2;
        public float dashChargeRegen = 1.4f;   // seconds to regen one charge
        public float dashIFrameDuration = 0.18f;

        [Header("Wall Run")]
        public float wallRunDuration = 1.6f;
        public float wallRunGravity = 3f;
        public float wallRunSpeed = 10f;
        public float wallJumpOutwardForce = 9f;
        public float wallJumpUpForce = 7f;
        public float wallCheckDistance = 0.6f;
        public float wallRunCooldownAfter = 0.2f;
        public LayerMask wallLayers = ~0;

        [Header("Slide")]
        public float slideSpeed = 14f;
        public float slideDuration = 0.9f;
        public float slideFriction = 4f;
        public float slideHeight = 0.9f;

        [Header("Camera / Look")]
        public Transform cameraPivot;
        public float mouseSensitivity = 2f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        // ─────── State (exposed for combat / HUD / AI) ───────
        public Vector3 Velocity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsWallRunning => wallRunTimer > 0f;
        public bool IsDashing => dashTimer > 0f;
        public bool IsSliding => slideTimer > 0f;
        public bool IsInvulnerable => iFrameTimer > 0f;
        public int DashChargesAvailable { get; private set; }
        public float StandingHeight { get; private set; }
        public Vector3 WallNormal { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        // ─────── Internals ───────
        CharacterController cc;
        Transform cam;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        int jumpsUsed;
        float dashTimer, dashCooldownTimer, dashChargeRegenTimer;
        float iFrameTimer;
        float wallRunTimer, wallRunCooldownTimer;
        Vector3 wallRunDirection;
        float slideTimer;
        float lastLandTime = -10f;
        Vector2 moveInput;
        bool jumpPressed, dashPressed, slidePressed, blocking;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cameraPivot != null) cam = cameraPivot;
            StandingHeight = cc.height;
            DashChargesAvailable = maxDashCharges;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            ReadInput();
            UpdateLook();
            UpdateTimers();

            // Decide movement state
            if (IsWallRunning) { WallRunMove(); }
            else if (IsDashing) { DashMove(); }
            else if (IsSliding) { SlideMove(); }
            else { StandardMove(); }

            ApplyGravity();
            MoveCharacter();

            TryStartWallRun();
            TryStartDash();
            TryStartSlide();
            HandleJump();

            DetectLanding();
        }

        // ─────── Input ───────
        void ReadInput()
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            jumpPressed  = Input.GetButtonDown("Jump");
            dashPressed  = Input.GetKeyDown(KeyCode.LeftShift);
            slidePressed = Input.GetKeyDown(KeyCode.LeftControl);
            blocking     = Input.GetMouseButton(1); // Right click (held)
        }

        void UpdateLook()
        {
            if (cam == null) return;
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            Yaw += mx;
            Pitch = Mathf.Clamp(Pitch - my, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0, Yaw, 0);
            cam.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }

        void UpdateTimers()
        {
            float dt = Time.deltaTime;
            if (dashTimer > 0f) dashTimer -= dt;
            if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;
            if (iFrameTimer > 0f) iFrameTimer -= dt;
            if (wallRunTimer > 0f) wallRunTimer -= dt;
            if (wallRunCooldownTimer > 0f) wallRunCooldownTimer -= dt;
            if (slideTimer > 0f)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f) EndSlide();
            }

            // Dash charge regen
            if (DashChargesAvailable < maxDashCharges)
            {
                dashChargeRegenTimer += dt;
                if (dashChargeRegenTimer >= dashChargeRegen)
                {
                    dashChargeRegenTimer = 0f;
                    DashChargesAvailable++;
                }
            }
        }

        // ─────── Movement Modes ───────
        Vector3 GetWishDirection()
        {
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;
            return (fwd * moveInput.y + right * moveInput.x).normalized;
        }

        void StandardMove()
        {
            Vector3 wish = GetWishDirection();
            float wishSpeed = (blocking ? walkSpeed * 0.5f : runSpeed);

            if (IsGrounded)
            {
                // Friction
                float speed = horizontalVelocity.magnitude;
                if (speed > 0f)
                {
                    float drop = speed * groundFriction * Time.deltaTime;
                    horizontalVelocity *= Mathf.Max(speed - drop, 0f) / speed;
                }
                // Accelerate toward wish
                float addSpeed = wishSpeed - Vector3.Dot(horizontalVelocity, wish);
                if (addSpeed > 0f)
                {
                    float accelAmt = groundAccel * Time.deltaTime * wishSpeed;
                    accelAmt = Mathf.Min(accelAmt, addSpeed);
                    horizontalVelocity += wish * accelAmt;
                }
            }
            else
            {
                // Air control (quake-style)
                float addSpeed = airMaxSpeed - Vector3.Dot(horizontalVelocity, wish);
                if (addSpeed > 0f)
                {
                    float accelAmt = airAccel * airControl * Time.deltaTime * airMaxSpeed;
                    accelAmt = Mathf.Min(accelAmt, addSpeed);
                    horizontalVelocity += wish * accelAmt;
                }
            }
        }

        void DashMove()
        {
            // Velocity set when dash started; kill vertical while dashing
            verticalVelocity = 0f;
            if (dashTimer <= 0f)
            {
                // Preserve some momentum after dash
                horizontalVelocity *= 0.6f;
            }
        }

        void SlideMove()
        {
            // Linear decay toward 0
            float speed = horizontalVelocity.magnitude;
            float drop = slideFriction * Time.deltaTime;
            if (speed - drop > 0f) horizontalVelocity *= (speed - drop) / speed;
            else horizontalVelocity = Vector3.zero;
        }

        void WallRunMove()
        {
            // Stick along wall direction, gentle upward push
            Vector3 projected = Vector3.ProjectOnPlane(transform.forward, WallNormal).normalized;
            wallRunDirection = projected;
            horizontalVelocity = projected * wallRunSpeed;
            verticalVelocity = Mathf.Max(verticalVelocity, -wallRunGravity * Time.deltaTime);
        }

        // ─────── Gravity / Move / Landing ───────
        void ApplyGravity()
        {
            if (IsGrounded && !IsDashing && verticalVelocity < 0f) verticalVelocity = -2f;
            else if (IsWallRunning) verticalVelocity -= wallRunGravity * Time.deltaTime;
            else verticalVelocity -= gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity);
        }

        void MoveCharacter()
        {
            Vector3 delta = horizontalVelocity * Time.deltaTime + Vector3.up * verticalVelocity * Time.deltaTime;
            cc.Move(delta);
            IsGrounded = cc.isGrounded;
            Velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        }

        void DetectLanding()
        {
            if (IsGrounded && verticalVelocity <= 0f)
            {
                lastLandTime = Time.time;
                jumpsUsed = 0;
            }
        }

        // ─────── Actions ───────
        void HandleJump()
        {
            if (!jumpPressed) return;

            if (IsWallRunning)
            {
                // Wall jump: combine outward push + vertical
                Vector3 jumpDir = (WallNormal * wallJumpOutwardForce + Vector3.up * wallJumpUpForce);
                horizontalVelocity = new Vector3(jumpDir.x, 0f, jumpDir.z);
                verticalVelocity = jumpDir.y;
                wallRunTimer = 0f;
                wallRunCooldownTimer = wallRunCooldownAfter;
                jumpsUsed = 1; // one jump spent on wall-jump
                return;
            }

            if (IsGrounded)
            {
                // Bunny hop boost if within window
                bool bhop = (Time.time - lastLandTime) < bunnyHopWindow;
                if (bhop)
                {
                    horizontalVelocity *= bunnyHopBoost;
                    // Cap at a sensible maximum
                    float maxBhop = runSpeed * 1.8f;
                    if (horizontalVelocity.magnitude > maxBhop)
                        horizontalVelocity = horizontalVelocity.normalized * maxBhop;
                }
                verticalVelocity = jumpVelocity;
                jumpsUsed = 1;
                if (IsSliding) EndSlide();
            }
            else if (jumpsUsed < 2)
            {
                // Double jump
                verticalVelocity = doubleJumpVelocity;
                jumpsUsed = 2;
            }
        }

        void TryStartDash()
        {
            if (!dashPressed) return;
            if (dashCooldownTimer > 0f || DashChargesAvailable <= 0) return;
            if (IsSliding) EndSlide();

            Vector3 wish = GetWishDirection();
            if (wish.sqrMagnitude < 0.01f) wish = transform.forward;

            horizontalVelocity = wish * dashSpeed;
            verticalVelocity = Mathf.Max(verticalVelocity, 1f);
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            iFrameTimer = dashIFrameDuration;
            DashChargesAvailable--;
            dashChargeRegenTimer = 0f;
        }

        void TryStartSlide()
        {
            if (!slidePressed) return;
            if (!IsGrounded) return;
            if (horizontalVelocity.magnitude < walkSpeed * 0.9f) return;
            slideTimer = slideDuration;
            cc.height = slideHeight;
            cc.center = new Vector3(0f, slideHeight * 0.5f, 0f);
            Vector3 wish = GetWishDirection();
            if (wish.sqrMagnitude < 0.01f) wish = transform.forward;
            horizontalVelocity = wish * slideSpeed;
        }

        void EndSlide()
        {
            slideTimer = 0f;
            cc.height = StandingHeight;
            cc.center = new Vector3(0f, StandingHeight * 0.5f, 0f);
        }

        void TryStartWallRun()
        {
            if (IsGrounded || IsWallRunning || wallRunCooldownTimer > 0f) return;
            if (horizontalVelocity.magnitude < runSpeed * 0.5f) return;

            // Raycast left/right for a wall
            RaycastHit hit;
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            if (Physics.Raycast(origin, transform.right, out hit, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore)
             || Physics.Raycast(origin, -transform.right, out hit, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore))
            {
                WallNormal = hit.normal;
                wallRunTimer = wallRunDuration;
                verticalVelocity = Mathf.Max(verticalVelocity, 0f);
            }
        }

        // ─────── External hooks ───────
        public void ApplyKnockback(Vector3 knock)
        {
            horizontalVelocity += new Vector3(knock.x, 0f, knock.z);
            verticalVelocity += knock.y;
        }

        public void SetMouseLook(bool enabled)
        {
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
            this.enabled = enabled;
        }

        public void Teleport(Vector3 position)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        /// <summary>Grant temporary invulnerability (used by skills like Battle Roll).</summary>
        public void GrantIFrames(float duration)
        {
            if (duration > iFrameTimer) iFrameTimer = duration;
        }
    }
}
