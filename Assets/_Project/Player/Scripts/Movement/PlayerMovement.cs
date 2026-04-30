using DungeonBlade.Core;
using UnityEngine;

namespace DungeonBlade.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Walk / Run")]
        [SerializeField] float walkSpeed = 6f;
        [SerializeField] float sprintSpeed = 9f;
        [SerializeField] float airAcceleration = 30f;
        [SerializeField] float groundAcceleration = 60f;
        [SerializeField] float groundFriction = 12f;

        [Header("Jump")]
        [SerializeField] float jumpHeight = 1.6f;
        [SerializeField] int maxAirJumps = 1;
        [SerializeField] float gravity = -22f;
        [SerializeField] float coyoteTime = 0.12f;
        [SerializeField] float jumpBufferTime = 0.12f;

        [Header("Bunny Hop")]
        [Tooltip("Window after landing in which jumping again preserves horizontal momentum.")]
        [SerializeField] float bhopWindow = 0.18f;
        [Tooltip("Speed multiplier applied per chained bhop, capped by bhopMaxSpeed.")]
        [SerializeField] float bhopGain = 1.05f;
        [SerializeField] float bhopMaxSpeed = 14f;

        [Header("Dash")]
        [SerializeField] float dashSpeed = 18f;
        [SerializeField] float dashDuration = 0.18f;
        [SerializeField] float dashCooldown = 0.6f;
        [SerializeField] float dashStaminaCost = 20f;

        [Header("Slide")]
        [SerializeField] float slideSpeed = 12f;
        [SerializeField] float slideDuration = 0.6f;
        [SerializeField] float slideHeight = 1.0f;

        [Header("Wall Run")]
        [SerializeField] float wallRunMaxDuration = 1.5f;
        [SerializeField] float wallRunSpeed = 8f;
        [SerializeField] float wallRunGravity = -3f;
        [SerializeField] float wallCheckDistance = 0.7f;
        [SerializeField] float wallRunMinAirTime = 0.1f;
        [SerializeField] float wallJumpForce = 7f;
        [SerializeField] LayerMask wallLayers = ~0;

        [Header("Camera")]
        [SerializeField] Transform cameraRig;
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;

        CharacterController _controller;
        PlayerStats _stats;
        PlayerInputActions _input;

        Vector3 _velocity;
        Vector2 _moveInput;
        float _yaw;
        float _pitch;

        bool _isGrounded;
        float _lastGroundedTime;
        float _lastJumpPressedTime = -999f;
        int _airJumpsUsed;

        float _lastLandTime = -999f;
        float _carriedHorizontalSpeed;

        bool _isDashing;
        float _dashEndTime;
        float _nextDashTime;
        Vector3 _dashDirection;

        public bool IsDashing => _isDashing;
        public bool IsInvulnerable => _isDashing;

        bool _isSliding;
        float _slideEndTime;
        float _defaultHeight;
        Vector3 _defaultCenter;

        bool _isWallRunning;
        float _wallRunEndTime;
        Vector3 _wallNormal;
        float _airTime;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
            _defaultHeight = _controller.height;
            _defaultCenter = _controller.center;
        }

        void Start()
        {
            _input = InputManager.Instance != null ? InputManager.Instance.Actions : new PlayerInputActions();
            if (InputManager.Instance == null) _input.Enable();

            _yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (_stats != null && _stats.IsDead) return;

            if (MenuState.IsAnyOpen)
            {
                _moveInput = Vector2.zero;
                UpdateGrounding();
                ApplyMovement();
                return;
            }

            ReadInput();
            UpdateLook();
            UpdateGrounding();
            UpdateWallRun();
            HandleDash();
            HandleSlide();
            HandleJump();
            ApplyMovement();
        }

        void ReadInput()
        {
            _moveInput = _input.Move.ReadValue<Vector2>();
        }

        void UpdateLook()
        {
            float sens = SettingsManager.Instance != null ? SettingsManager.Instance.MouseSensitivity : lookSensitivity;
            Vector2 look = _input.Look.ReadValue<Vector2>() * sens;
            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraRig != null)
            {
                cameraRig.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        void UpdateGrounding()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = _controller.isGrounded;

            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
                _airTime = 0f;
                if (!wasGrounded)
                {
                    OnLand();
                }
            }
            else
            {
                _airTime += Time.deltaTime;
            }
        }

        void OnLand()
        {
            _airJumpsUsed = 0;

            float horizontalSpeed = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
            _carriedHorizontalSpeed = horizontalSpeed;
            _lastLandTime = Time.time;
            _isWallRunning = false;
        }

        void HandleJump()
        {
            if (_input.Jump.WasPressedThisFrame())
            {
                _lastJumpPressedTime = Time.time;
            }

            bool jumpBuffered = Time.time - _lastJumpPressedTime <= jumpBufferTime;
            if (!jumpBuffered) return;

            bool canGroundJump = _isGrounded || (Time.time - _lastGroundedTime <= coyoteTime && _velocity.y <= 0f);
            bool canAirJump = !_isGrounded && _airJumpsUsed < maxAirJumps;
            bool canWallJump = _isWallRunning;

            if (canWallJump)
            {
                Vector3 jumpDir = (_wallNormal + Vector3.up).normalized;
                _velocity = jumpDir * wallJumpForce;
                _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
                _isWallRunning = false;
                _lastJumpPressedTime = -999f;
                return;
            }

            if (canGroundJump)
            {
                bool isBhop = Time.time - _lastLandTime <= bhopWindow;
                _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);

                if (isBhop && _carriedHorizontalSpeed > 0.1f)
                {
                    Vector3 horiz = new Vector3(_velocity.x, 0f, _velocity.z);
                    float currentMag = horiz.magnitude;
                    Vector3 dir = currentMag > 0.01f ? horiz / currentMag : transform.forward;
                    float boosted = Mathf.Min(bhopMaxSpeed, _carriedHorizontalSpeed * bhopGain);
                    _velocity.x = dir.x * boosted;
                    _velocity.z = dir.z * boosted;
                }

                _isGrounded = false;
                _lastJumpPressedTime = -999f;
            }
            else if (canAirJump)
            {
                _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
                _airJumpsUsed++;
                _lastJumpPressedTime = -999f;
            }
        }

        void HandleDash()
        {
            if (_isDashing)
            {
                if (Time.time >= _dashEndTime)
                {
                    _isDashing = false;
                }
                return;
            }

            if (Time.time < _nextDashTime) return;
            if (!_input.Dash.WasPressedThisFrame()) return;
            if (_stats != null && !_stats.TryConsumeStamina(dashStaminaCost)) return;

            Vector3 inputDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            if (inputDir.sqrMagnitude < 0.01f) inputDir = transform.forward;
            _dashDirection = inputDir.normalized;

            _isDashing = true;
            _dashEndTime = Time.time + dashDuration;
            _nextDashTime = Time.time + dashCooldown;
        }

        void HandleSlide()
        {
            bool crouchPressed = _input.Crouch.WasPressedThisFrame();

            if (!_isSliding && crouchPressed && _isGrounded && _moveInput.sqrMagnitude > 0.1f)
            {
                _isSliding = true;
                _slideEndTime = Time.time + slideDuration;
                _controller.height = slideHeight;
                _controller.center = new Vector3(_defaultCenter.x, slideHeight * 0.5f, _defaultCenter.z);
            }
            else if (_isSliding && (Time.time >= _slideEndTime || !_isGrounded))
            {
                EndSlide();
            }
        }

        void EndSlide()
        {
            _isSliding = false;
            _controller.height = _defaultHeight;
            _controller.center = _defaultCenter;
        }

        void UpdateWallRun()
        {
            if (_isGrounded || _airTime < wallRunMinAirTime)
            {
                _isWallRunning = false;
                return;
            }

            if (_isWallRunning && Time.time >= _wallRunEndTime)
            {
                _isWallRunning = false;
                return;
            }

            bool hasInput = _moveInput.sqrMagnitude > 0.1f;
            if (!hasInput && !_isWallRunning)
            {
                return;
            }

            Vector3 origin = transform.position + Vector3.up * (_controller.height * 0.5f);
            bool foundWall = false;

            if (Physics.Raycast(origin, transform.right, out RaycastHit rightHit, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore)
                && IsVerticalWall(rightHit.normal))
            {
                _wallNormal = rightHit.normal;
                foundWall = true;
            }
            else if (Physics.Raycast(origin, -transform.right, out RaycastHit leftHit, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore)
                && IsVerticalWall(leftHit.normal))
            {
                _wallNormal = leftHit.normal;
                foundWall = true;
            }

            if (foundWall)
            {
                if (!_isWallRunning)
                {
                    _isWallRunning = true;
                    _wallRunEndTime = Time.time + wallRunMaxDuration;
                }
            }
            else
            {
                _isWallRunning = false;
            }
        }

        static bool IsVerticalWall(Vector3 normal)
        {
            return Mathf.Abs(normal.y) < 0.3f;
        }

        void ApplyMovement()
        {
            if (_isDashing)
            {
                Vector3 dashStep = _dashDirection * dashSpeed;
                _controller.Move(dashStep * Time.deltaTime);
                _velocity.x = dashStep.x;
                _velocity.z = dashStep.z;
                _velocity.y = 0f;
                return;
            }

            if (_isSliding)
            {
                Vector3 slideDir = transform.forward * slideSpeed;
                _velocity.x = slideDir.x;
                _velocity.z = slideDir.z;
                _velocity.y += gravity * Time.deltaTime;
                _controller.Move(_velocity * Time.deltaTime);
                return;
            }

            if (_isWallRunning)
            {
                Vector3 wallForward = Vector3.Cross(_wallNormal, Vector3.up);
                if (Vector3.Dot(wallForward, transform.forward) < 0f) wallForward = -wallForward;

                Vector3 wallVel = wallForward * wallRunSpeed;
                _velocity.x = wallVel.x;
                _velocity.z = wallVel.z;
                _velocity.y += wallRunGravity * Time.deltaTime;
                _controller.Move(_velocity * Time.deltaTime);
                return;
            }

            Vector3 wishDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            float wishSpeed = _input.Sprint.IsPressed() ? sprintSpeed : walkSpeed;

            Vector3 horizontalVel = new Vector3(_velocity.x, 0f, _velocity.z);

            if (_isGrounded)
            {
                Vector3 target = wishDir * wishSpeed;
                horizontalVel = Vector3.MoveTowards(horizontalVel, target, groundAcceleration * Time.deltaTime);

                if (wishDir.sqrMagnitude < 0.01f)
                {
                    horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, groundFriction * Time.deltaTime);
                }
            }
            else
            {
                if (wishDir.sqrMagnitude > 0.01f)
                {
                    horizontalVel += wishDir * (airAcceleration * Time.deltaTime);
                    float airCap = Mathf.Max(wishSpeed, bhopMaxSpeed);
                    if (horizontalVel.magnitude > airCap)
                    {
                        horizontalVel = horizontalVel.normalized * airCap;
                    }
                }
            }

            _velocity.x = horizontalVel.x;
            _velocity.z = horizontalVel.z;

            if (_isGrounded && _velocity.y < 0f) _velocity.y = -2f;
            _velocity.y += gravity * Time.deltaTime;

            _controller.Move(_velocity * Time.deltaTime);
        }
    }
}
