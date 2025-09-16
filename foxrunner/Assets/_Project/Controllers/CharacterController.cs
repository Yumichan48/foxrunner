using UnityEngine;
using UnityEngine.InputSystem;

namespace FoxRunner.Movement
{
    /// <summary>
    /// Production-ready character movement system for Fox Runner
    /// Implements dash, wall slide/jump, multi-jump, and variable jump height
    /// Uses Unity Character Controller with ScriptableObject configuration
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class CharacterMovementDriver : MonoBehaviour
    {
        #region Configuration & Components
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private MovementConfiguration config;

        [Header("=== COMPONENTS ===")]
        [SerializeField] private CharacterController controller;
        [SerializeField] private Animator animator;

        [Header("=== STATE INFO (READ ONLY) ===")]
        [SerializeField] private Vector3 velocity;
        [SerializeField] private float currentSpeed;
        [SerializeField] private bool isGrounded;
        [SerializeField] private int jumpsRemaining;
        [SerializeField] private bool isWallSliding;
        [SerializeField] private bool isDashing;
        [SerializeField] private float dashTimeRemaining;
        [SerializeField] private float dashCooldownRemaining;
        #endregion

        #region Private Fields
        // Component references
        private Transform groundCheck;
        private Transform ceilingCheck;
        private Transform wallCheckLeft;
        private Transform wallCheckRight;

        // Movement state
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private float lastGroundedTime;
        private bool wasGrounded;

        // Dash system
        private Vector3 dashDirection;
        private Vector3 dashStartPosition;
        private float dashProgress;

        // Wall system
        private bool isTouchingWallLeft;
        private bool isTouchingWallRight;
        private bool isTouchingWall => isTouchingWallLeft || isTouchingWallRight;
        private int wallDirection => isTouchingWallLeft ? -1 : (isTouchingWallRight ? 1 : 0);

        // Input buffering
        private bool dashInputQueued;
        private float dashInputQueueTime;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            InitializeComponents();
            ValidateConfiguration();
            SetupCheckPoints();
        }

        void Update()
        {
            UpdateTimers();
            UpdateGroundCheck();
            UpdateWallCheck();
            UpdateDashSystem();
            UpdateWallSlideSystem();
            UpdateMovement();
            UpdateAnimatorParameters();
        }

        void OnDrawGizmos()
        {
            if (config && config.showDebugInfo)
            {
                DrawDebugGizmos();
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            if (!controller) controller = GetComponent<CharacterController>();
            if (!animator) animator = GetComponent<Animator>();
        }

        private void ValidateConfiguration()
        {
            if (!config)
            {
                Debug.LogError($"[{name}] Movement Configuration asset missing! Movement will be disabled.", this);
                enabled = false;
                return;
            }

            if (!controller)
            {
                Debug.LogError($"[{name}] CharacterController component missing!", this);
                enabled = false;
                return;
            }

            if (!animator)
            {
                Debug.LogWarning($"[{name}] Animator component missing. Animation integration disabled.", this);
            }
        }

        private void SetupCheckPoints()
        {
            // Find or create check points
            groundCheck = transform.Find("GroundCheck");
            if (!groundCheck)
            {
                GameObject go = new GameObject("GroundCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.down * (controller.height * 0.5f + config.groundCheckDistance);
                groundCheck = go.transform;
            }

            ceilingCheck = transform.Find("CeilingCheck");
            if (!ceilingCheck)
            {
                GameObject go = new GameObject("CeilingCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.up * (controller.height * 0.5f + 0.1f);
                ceilingCheck = go.transform;
            }

            wallCheckLeft = transform.Find("WallCheckLeft");
            if (!wallCheckLeft)
            {
                GameObject go = new GameObject("WallCheckLeft");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.left * (controller.radius + 0.1f);
                wallCheckLeft = go.transform;
            }

            wallCheckRight = transform.Find("WallCheckRight");
            if (!wallCheckRight)
            {
                GameObject go = new GameObject("WallCheckRight");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.right * (controller.radius + 0.1f);
                wallCheckRight = go.transform;
            }
        }
        #endregion

        #region Update Systems
        private void UpdateTimers()
        {
            // Dash cooldown
            if (dashCooldownRemaining > 0)
                dashCooldownRemaining -= Time.deltaTime;

            // Jump buffer
            if (jumpBufferCounter > 0)
                jumpBufferCounter -= Time.deltaTime;

            // Dash input queue
            if (dashInputQueueTime > 0)
            {
                dashInputQueueTime -= Time.deltaTime;
                if (dashInputQueueTime <= 0)
                    dashInputQueued = false;
            }
        }

        private void UpdateGroundCheck()
        {
            wasGrounded = isGrounded;

            // Primary ground check using Character Controller
            isGrounded = controller.isGrounded;

            // Secondary check using sphere overlap for reliability
            if (!isGrounded && groundCheck)
            {
                isGrounded = Physics.CheckSphere(
                    groundCheck.position,
                    config.groundCheckRadius,
                    config.groundLayers
                );
            }

            // Handle ground state changes
            if (isGrounded && !wasGrounded)
            {
                OnLanded();
            }
            else if (!isGrounded && wasGrounded)
            {
                OnBecameAirborne();
            }

            // Update coyote time
            if (isGrounded)
            {
                coyoteTimeCounter = config.coyoteTime;
                jumpsRemaining = config.maxJumps;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void UpdateWallCheck()
        {
            if (!config.canWallSlide && !config.canWallJump) return;

            // Check wall contact
            isTouchingWallLeft = Physics.CheckSphere(
                wallCheckLeft.position,
                config.groundCheckRadius * 0.5f,
                config.groundLayers
            );

            isTouchingWallRight = Physics.CheckSphere(
                wallCheckRight.position,
                config.groundCheckRadius * 0.5f,
                config.groundLayers
            );
        }

        private void UpdateDashSystem()
        {
            if (!isDashing) return;

            dashTimeRemaining -= Time.deltaTime;
            dashProgress = 1f - (dashTimeRemaining / config.dashDuration);

            if (dashTimeRemaining <= 0)
            {
                EndDash();
            }
            else
            {
                // Calculate dash movement using smoothstep curve for feel
                float easedProgress = Mathf.SmoothStep(0f, 1f, dashProgress);
                Vector3 targetPosition = dashStartPosition + dashDirection * config.dashDistance;
                Vector3 dashPosition = Vector3.Lerp(dashStartPosition, targetPosition, easedProgress);

                // Apply dash movement
                Vector3 dashMovement = dashPosition - transform.position;
                controller.Move(dashMovement);
            }
        }

        private void UpdateWallSlideSystem()
        {
            if (!config.canWallSlide) return;

            bool shouldWallSlide = !isGrounded && isTouchingWall && velocity.y < 0;

            if (shouldWallSlide && !isWallSliding)
            {
                StartWallSlide();
            }
            else if (!shouldWallSlide && isWallSliding)
            {
                EndWallSlide();
            }

            if (isWallSliding)
            {
                // Apply wall slide velocity
                velocity.y = Mathf.Max(velocity.y, -config.wallSlideSpeed);
            }
        }

        private void UpdateMovement()
        {
            if (isDashing) return; // Don't apply movement during dash

            // Apply auto-run if enabled
            if (config.autoRun)
            {
                currentSpeed = config.baseRunSpeed;
            }

            // Apply gravity
            if (!isGrounded && !isWallSliding)
            {
                ApplyGravity();
            }
            else if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to stay grounded
            }

            // Apply horizontal movement
            Vector3 horizontalMovement = Vector3.right * currentSpeed;
            Vector3 verticalMovement = Vector3.up * velocity.y;
            Vector3 totalMovement = (horizontalMovement + verticalMovement) * Time.deltaTime;

            // Move the character
            controller.Move(totalMovement);
        }

        private void UpdateAnimatorParameters()
        {
            if (!animator) return;

            // Update animator parameters
            animator.SetFloat(config.speedParam, currentSpeed);
            animator.SetBool(config.isGroundedParam, isGrounded);
            animator.SetFloat("VelocityY", velocity.y);
            animator.SetBool("IsWallSliding", isWallSliding);
            animator.SetBool("IsDashing", isDashing);
        }
        #endregion

        #region Physics & Movement
        private void ApplyGravity()
        {
            float gravityForce = Physics.gravity.y * config.gravityScale;

            // Apply fall multiplier for better game feel
            if (velocity.y < 0)
            {
                gravityForce *= config.fallMultiplier;
            }

            velocity.y += gravityForce * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -config.maxFallSpeed);
        }
        #endregion

        #region Input System Callbacks
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                jumpBufferCounter = config.jumpBufferTime;
                TryJump();
            }
            else if (context.canceled)
            {

                // Cut jump short for variable jump height
                if (config.variableJumpHeight && velocity.y > 0)
                {
                    velocity.y *= config.minJumpHeightPercent;
                }
            }
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (CanDash())
                {
                    StartDash();
                }
                else
                {
                    // Queue dash input for buffering
                    dashInputQueued = true;
                    dashInputQueueTime = config.jumpBufferTime;
                }
            }
        }
        #endregion

        #region Jump System
        private void TryJump()
        {
            // Check for wall jump first
            if (config.canWallJump && isWallSliding)
            {
                PerformWallJump();
                return;
            }

            // Regular jump
            if (CanJump())
            {
                PerformJump();
            }
        }

        private bool CanJump()
        {
            return (jumpsRemaining > 0 || coyoteTimeCounter > 0) && !isDashing;
        }

        private void PerformJump()
        {
            float jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * config.jumpHeight);

            // Apply multi-jump multiplier
            if (jumpsRemaining < config.maxJumps)
            {
                jumpVelocity *= config.multiJumpMultiplier;
            }

            velocity.y = jumpVelocity;
            jumpsRemaining--;
            coyoteTimeCounter = 0f; // Use up coyote time

            // Animation and effects
            if (animator) animator.SetTrigger(config.jumpTrigger);

            // Stop wall sliding
            if (isWallSliding)
            {
                EndWallSlide();
            }

            OnJumpPerformed();
        }

        private void PerformWallJump()
        {
            if (!config.canWallJump || !isWallSliding) return;

            // Wall jump has horizontal component opposite to wall
            float jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * config.jumpHeight);
            velocity.y = jumpVelocity;

            // Add horizontal push away from wall
            float horizontalPush = config.baseRunSpeed * 1.5f; // 50% extra push
            velocity.x = -wallDirection * horizontalPush;

            // End wall sliding
            EndWallSlide();

            // Animation and effects
            if (animator) animator.SetTrigger(config.jumpTrigger);
            OnJumpPerformed();
        }
        #endregion

        #region Dash System
        private bool CanDash()
        {
            return config.canDash && dashCooldownRemaining <= 0 && !isDashing;
        }

        private void StartDash()
        {
            if (!CanDash()) return;

            isDashing = true;
            dashTimeRemaining = config.dashDuration;
            dashStartPosition = transform.position;
            dashDirection = Vector3.right; // Forward dash for runner
            dashProgress = 0f;
            dashCooldownRemaining = config.dashCooldown;

            // Disable gravity during dash
            velocity.y = 0f;

            // Animation and effects
            if (animator) animator.SetTrigger(config.dashTrigger);
            OnDashStarted();
        }

        private void EndDash()
        {
            isDashing = false;
            dashTimeRemaining = 0f;

            OnDashEnded();
        }
        #endregion

        #region Wall Slide System
        private void StartWallSlide()
        {
            if (!config.canWallSlide) return;

            isWallSliding = true;
            OnWallSlideStarted();
        }

        private void EndWallSlide()
        {
            isWallSliding = false;
            OnWallSlideEnded();
        }
        #endregion

        #region Event Handlers
        private void OnLanded()
        {
            if (animator) animator.SetTrigger(config.landTrigger);

            // Screen shake effect
            if (config.screenShakeOnLand)
            {
                // TODO: Integrate with camera shake system
            }

            // Process queued dash input
            if (dashInputQueued && CanDash())
            {
                StartDash();
                dashInputQueued = false;
            }
        }

        private void OnBecameAirborne()
        {
            lastGroundedTime = Time.time;
        }

        private void OnJumpPerformed()
        {
            // Particle effects
            if (config.particlesOnJump)
            {
                // TODO: Integrate with particle system
            }
        }

        private void OnDashStarted()
        {
            // Effects and feedback for dash start
        }

        private void OnDashEnded()
        {
            // Effects and feedback for dash end
        }

        private void OnWallSlideStarted()
        {
            // Effects for wall slide start
        }

        private void OnWallSlideEnded()
        {
            // Effects for wall slide end
        }
        #endregion

        #region Animation Events
        /// <summary>
        /// Called from animation clips for footstep sounds
        /// </summary>
        public void FootstepEvent()
        {
            // TODO: Integrate with audio system
        }

        /// <summary>
        /// Called from animation clips for dust particle effects
        /// </summary>
        public void DustPuffEvent()
        {
            // TODO: Integrate with particle system
        }
        #endregion

        #region Debug & Gizmos
        private void DrawDebugGizmos()
        {
            Gizmos.color = config.debugColor;

            // Ground check
            if (groundCheck)
            {
                Gizmos.DrawWireSphere(groundCheck.position, config.groundCheckRadius);
            }

            // Wall checks
            if (wallCheckLeft)
            {
                Gizmos.color = isTouchingWallLeft ? Color.red : config.debugColor;
                Gizmos.DrawWireSphere(wallCheckLeft.position, config.groundCheckRadius * 0.5f);
            }

            if (wallCheckRight)
            {
                Gizmos.color = isTouchingWallRight ? Color.red : config.debugColor;
                Gizmos.DrawWireSphere(wallCheckRight.position, config.groundCheckRadius * 0.5f);
            }

            // Dash trajectory
            if (isDashing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(dashStartPosition, dashStartPosition + dashDirection * config.dashDistance);
            }
        }
        #endregion
    }
}