using UnityEngine;

[CreateAssetMenu(fileName = "MovementConfig", menuName = "FoxRunner/Movement Configuration")]
public class MovementConfiguration : ScriptableObject
{
    [Header("=== RUNNING SETTINGS ===")]
    [Tooltip("Base running speed")]
    [Range(1f, 20f)]
    public float baseRunSpeed = 5f;

    [Tooltip("Maximum run speed")]
    [Range(1f, 30f)]
    public float maxRunSpeed = 8f;

    [Tooltip("Acceleration rate")]
    [Range(1f, 50f)]
    public float acceleration = 10f;

    [Tooltip("Deceleration rate")]
    [Range(1f, 50f)]
    public float deceleration = 15f;

    [Tooltip("Auto-run enabled")]
    public bool autoRun = true;

    [Header("=== JUMP SETTINGS ===")]
    [Tooltip("Jump height in units")]
    [Range(1f, 10f)]
    public float jumpHeight = 3f;

    [Tooltip("Time to reach jump peak")]
    [Range(0.1f, 1f)]
    public float jumpDuration = 0.4f;

    [Tooltip("Maximum number of jumps")]
    [Range(1, 3)]
    public int maxJumps = 2;

    [Tooltip("Double/Triple jump height multiplier")]
    [Range(0.5f, 1f)]
    public float multiJumpMultiplier = 0.85f;

    [Tooltip("Variable jump height (hold for higher)")]
    public bool variableJumpHeight = true;

    [Tooltip("Minimum jump height percentage")]
    [Range(0.1f, 1f)]
    public float minJumpHeightPercent = 0.3f;

    [Header("=== AIR CONTROL ===")]
    [Tooltip("Air control amount")]
    [Range(0f, 1f)]
    public float airControl = 0.3f;

    [Tooltip("Fall speed multiplier")]
    [Range(1f, 3f)]
    public float fallMultiplier = 2f;

    [Tooltip("Max fall speed")]
    [Range(10f, 50f)]
    public float maxFallSpeed = 20f;

    [Tooltip("Glide ability enabled")]
    public bool canGlide = false;

    [Tooltip("Glide fall speed")]
    [Range(0.5f, 5f)]
    public float glideSpeed = 2f;

    [Header("=== GROUND DETECTION ===")]
    [Tooltip("Ground check distance")]
    [Range(0.01f, 0.5f)]
    public float groundCheckDistance = 0.1f;

    [Tooltip("Ground check radius")]
    [Range(0.1f, 1f)]
    public float groundCheckRadius = 0.3f;

    [Tooltip("What counts as ground")]
    public LayerMask groundLayers = -1;

    [Tooltip("Coyote time (jump after leaving ground)")]
    [Range(0f, 0.5f)]
    public float coyoteTime = 0.1f;

    [Tooltip("Jump buffer (early jump input)")]
    [Range(0f, 0.5f)]
    public float jumpBufferTime = 0.15f;

    [Header("=== ABILITIES ===")]
    [Tooltip("Dash enabled")]
    public bool canDash = false;

    [Tooltip("Dash distance")]
    [Range(1f, 10f)]
    public float dashDistance = 3f;

    [Tooltip("Dash duration")]
    [Range(0.05f, 0.5f)]
    public float dashDuration = 0.2f;

    [Tooltip("Dash cooldown")]
    [Range(0.5f, 5f)]
    public float dashCooldown = 1f;

    [Tooltip("Wall slide enabled")]
    public bool canWallSlide = false;

    [Tooltip("Wall slide speed")]
    [Range(0.5f, 5f)]
    public float wallSlideSpeed = 2f;

    [Tooltip("Wall jump enabled")]
    public bool canWallJump = false;

    [Header("=== PHYSICS FEEL ===")]
    [Tooltip("Gravity scale")]
    [Range(0.5f, 3f)]
    public float gravityScale = 1f;

    [Tooltip("Physics material for ground")]
    public PhysicMaterial groundPhysicsMaterial;

    [Tooltip("Physics material for walls")]
    public PhysicMaterial wallPhysicsMaterial;

    [Header("=== EFFECTS ===")]
    [Tooltip("Screen shake on land")]
    public bool screenShakeOnLand = true;

    [Tooltip("Particles on jump")]
    public bool particlesOnJump = true;

    [Tooltip("Dust trail while running")]
    public bool dustTrailEnabled = true;

    [Header("=== ANIMATION TRIGGERS ===")]
    [Tooltip("Animation parameter names")]
    public string speedParam = "Speed";
    public string isGroundedParam = "IsGrounded";
    public string jumpTrigger = "Jump";
    public string landTrigger = "Land";
    public string dashTrigger = "Dash";

    [Header("=== DEBUG ===")]
    [Tooltip("Show debug gizmos")]
    public bool showDebugInfo = true;

    [Tooltip("Debug line color")]
    public Color debugColor = Color.green;
}