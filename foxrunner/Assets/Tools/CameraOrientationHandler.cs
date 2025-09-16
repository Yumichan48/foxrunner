using UnityEngine;
using UnityEngine.U2D;
using System.Collections;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class CameraOrientationHandler : MonoBehaviour
{
    [Header("=== CONFIGURATION ===")]
    [Tooltip("Camera configuration asset")]
    [SerializeField] private CameraConfiguration config;

    [Header("=== COMPONENTS ===")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;

    [Header("=== RUNTIME STATE ===")]
    [SerializeField] private bool isPortrait = true;
    [SerializeField] private float currentAspect;
    [SerializeField] private string orientationState = "Portrait";

    [Header("=== EVENTS ===")]
    [Tooltip("Unity Event triggered on orientation change")]
    public UnityEngine.Events.UnityEvent<bool> OnOrientationChanged;

    private Coroutine transitionCoroutine;
    private float lastCheckTime;
    private const float CHECK_INTERVAL = 0.5f;

    void Awake()
    {
        // Get components if not assigned
        if (!mainCamera) mainCamera = GetComponent<Camera>();
        if (!pixelPerfectCamera) pixelPerfectCamera = GetComponent<PixelPerfectCamera>();

        // Validate
        if (!config)
        {
            Debug.LogError("Camera Configuration asset is missing!");
            return;
        }

        // Apply initial settings
        ApplyConfiguration();
    }

    void Start()
    {
        // Set target frame rate from config
        Application.targetFrameRate = config.targetFrameRate;
        QualitySettings.vSyncCount = config.vSyncCount;

        // Initial orientation check
        CheckOrientation();
    }

    void Update()
    {
        // Periodic orientation check
        if (Time.time - lastCheckTime > CHECK_INTERVAL)
        {
            lastCheckTime = Time.time;
            CheckOrientation();
        }
    }

    void ApplyConfiguration()
    {
        // Apply pixel perfect settings
        pixelPerfectCamera.assetsPPU = config.pixelsPerUnit;
        pixelPerfectCamera.pixelSnapping = config.pixelSnapping;
        pixelPerfectCamera.upscaleRT = config.upscaleRenderTexture;
        pixelPerfectCamera.cropFrameX = config.cropFrame;
        pixelPerfectCamera.cropFrameY = config.cropFrame;
    }

    void CheckOrientation()
    {
        bool shouldBePortrait = Screen.height > Screen.width;

        if (shouldBePortrait != isPortrait)
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            transitionCoroutine = StartCoroutine(TransitionOrientation(shouldBePortrait));
        }
    }

    IEnumerator TransitionOrientation(bool toPortrait)
    {
        isPortrait = toPortrait;
        orientationState = toPortrait ? "Portrait" : "Landscape";

        // Get start values
        Vector2Int startRes = new Vector2Int(
            pixelPerfectCamera.refResolutionX,
            pixelPerfectCamera.refResolutionY
        );
        float startOrtho = mainCamera.orthographicSize;

        // Get target values
        Vector2Int targetRes = toPortrait ?
            config.portraitResolution :
            config.landscapeResolution;
        float targetOrtho = toPortrait ?
            config.portraitOrthoSize :
            config.landscapeOrthoSize;

        // Animate transition
        float elapsed = 0;
        while (elapsed < config.transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.transitionDuration;
            float curveValue = config.transitionCurve.Evaluate(t);

            // Interpolate values
            pixelPerfectCamera.refResolutionX =
                Mathf.RoundToInt(Mathf.Lerp(startRes.x, targetRes.x, curveValue));
            pixelPerfectCamera.refResolutionY =
                Mathf.RoundToInt(Mathf.Lerp(startRes.y, targetRes.y, curveValue));
            mainCamera.orthographicSize =
                Mathf.Lerp(startOrtho, targetOrtho, curveValue);

            yield return null;
        }

        // Ensure final values
        pixelPerfectCamera.refResolutionX = targetRes.x;
        pixelPerfectCamera.refResolutionY = targetRes.y;
        mainCamera.orthographicSize = targetOrtho;

        // Trigger event
        OnOrientationChanged?.Invoke(isPortrait);

        Debug.Log($"Orientation changed to: {orientationState}");
    }

    // Public methods for external control
    public void ForcePortrait() => StartCoroutine(TransitionOrientation(true));
    public void ForceLandscape() => StartCoroutine(TransitionOrientation(false));
    public bool IsPortrait() => isPortrait;
}