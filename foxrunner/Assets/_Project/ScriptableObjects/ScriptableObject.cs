using UnityEngine;

[CreateAssetMenu(fileName = "InputConfig", menuName = "FoxRunner/Input Configuration")]
public class InputConfiguration : ScriptableObject
{
    [Header("=== TOUCH SETTINGS ===")]
    [Tooltip("Touch sensitivity")]
    [Range(0.1f, 2f)]
    public float touchSensitivity = 1f;

    [Tooltip("Swipe threshold distance")]
    [Range(10f, 100f)]
    public float swipeThreshold = 50f;

    [Tooltip("Tap time threshold")]
    [Range(0.1f, 0.5f)]
    public float tapTimeThreshold = 0.2f;

    [Tooltip("Dead zone for touch")]
    [Range(0f, 50f)]
    public float touchDeadZone = 10f;

    [Header("=== GAMEPAD SETTINGS ===")]
    [Tooltip("Gamepad dead zone")]
    [Range(0.01f, 0.5f)]
    public float gamepadDeadZone = 0.125f;

    [Tooltip("Vibration enabled")]
    public bool vibrationEnabled = true;

    [Tooltip("Vibration intensity")]
    [Range(0f, 1f)]
    public float vibrationIntensity = 0.5f;

    [Header("=== KEYBOARD SETTINGS ===")]
    [Tooltip("Key repeat delay")]
    [Range(0.1f, 1f)]
    public float keyRepeatDelay = 0.5f;

    [Tooltip("Key repeat rate")]
    [Range(0.01f, 0.5f)]
    public float keyRepeatRate = 0.1f;

    [Header("=== INPUT BUFFERING ===")]
    [Tooltip("Input buffer time")]
    [Range(0f, 0.5f)]
    public float inputBufferTime = 0.15f;

    [Tooltip("Queue inputs")]
    public bool queueInputs = true;

    [Tooltip("Max queued inputs")]
    [Range(1, 5)]
    public int maxQueuedInputs = 2;

    [Header("=== ACCESSIBILITY ===")]
    [Tooltip("Hold to jump (accessibility)")]
    public bool holdToJump = false;

    [Tooltip("Auto-run enabled")]
    public bool autoRun = true;

    [Tooltip("One-touch mode")]
    public bool oneTouchMode = false;

    [Header("=== DEBUG ===")]
    [Tooltip("Show input debug UI")]
    public bool showInputDebug = false;

    [Tooltip("Log input events")]
    public bool logInputEvents = false;
}