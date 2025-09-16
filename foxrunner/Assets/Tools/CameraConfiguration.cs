using UnityEngine;

[CreateAssetMenu(fileName = "CameraConfig", menuName = "FoxRunner/Camera Configuration")]
public class CameraConfiguration : ScriptableObject
{
    [Header("=== PORTRAIT SETTINGS ===")]
    [Tooltip("Reference resolution for portrait mode")]
    public Vector2Int portraitResolution = new Vector2Int(360, 640);

    [Tooltip("Camera orthographic size in portrait")]
    [Range(3f, 15f)]
    public float portraitOrthoSize = 8f;

    [Tooltip("UI scale for portrait mode")]
    [Range(0.5f, 2f)]
    public float portraitUIScale = 1f;

    [Header("=== LANDSCAPE SETTINGS ===")]
    [Tooltip("Reference resolution for landscape mode")]
    public Vector2Int landscapeResolution = new Vector2Int(640, 360);

    [Tooltip("Camera orthographic size in landscape")]
    [Range(3f, 15f)]
    public float landscapeOrthoSize = 5f;

    [Tooltip("UI scale for landscape mode")]
    [Range(0.5f, 2f)]
    public float landscapeUIScale = 1f;

    [Header("=== PIXEL PERFECT SETTINGS ===")]
    [Tooltip("Pixels per unit for all sprites")]
    [Range(8, 64)]
    public int pixelsPerUnit = 16;

    [Tooltip("Enable pixel snapping")]
    public bool pixelSnapping = true;

    [Tooltip("Enable upscale render texture")]
    public bool upscaleRenderTexture = true;

    [Tooltip("Crop frame to maintain aspect")]
    public bool cropFrame = true;

    [Header("=== TRANSITION SETTINGS ===")]
    [Tooltip("Time to transition between orientations")]
    [Range(0f, 1f)]
    public float transitionDuration = 0.3f;

    [Tooltip("Transition ease curve")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("=== CAMERA EFFECTS ===")]
    [Tooltip("Camera shake intensity multiplier")]
    [Range(0f, 2f)]
    public float shakeIntensity = 1f;

    [Tooltip("Enable camera bounds")]
    public bool useCameraBounds = false;

    [Tooltip("Camera bounds for gameplay")]
    public Bounds cameraBounds = new Bounds(Vector3.zero, Vector3.one * 100);

    [Header("=== PERFORMANCE ===")]
    [Tooltip("Target FPS for the game")]
    public int targetFrameRate = 60;

    [Tooltip("V-Sync setting (0=off, 1=on, 2=double)")]
    [Range(0, 2)]
    public int vSyncCount = 0;

    [Header("=== DEBUG SETTINGS ===")]
    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    [Tooltip("Debug text color")]
    public Color debugTextColor = Color.green;
}