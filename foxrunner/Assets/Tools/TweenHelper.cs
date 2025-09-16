using UnityEngine;
using DG.Tweening;

public class TweenHelper : MonoBehaviour
{
    [Header("=== CONFIGURATION ===")]
    [SerializeField] private TweenConfiguration config;

    private static TweenHelper instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDOTween();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeDOTween()
    {
        // Configure DOTween with our settings
        DOTween.SetTweensCapacity(config.tweenCapacity, config.sequenceCapacity);
        DOTween.defaultEaseType = config.uiEaseType;
        DOTween.defaultAutoPlay = AutoPlay.All;
        DOTween.defaultAutoKill = true;

        Debug.Log("DOTween initialized with custom settings");
    }

    // Tween helper methods
    public static void PulseScale(Transform target, float scale, float duration)
    {
        target.DOScale(scale, duration).SetLoops(2, LoopType.Yoyo);
    }

    public static void CollectAnimation(Transform item, Transform target, System.Action onComplete)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(item.DOScale(1.5f, 0.2f));
        sequence.Append(item.DOMove(target.position, 0.3f).SetEase(Ease.InBack));
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    public static void ShakeCamera(float strength, float duration)
    {
        Camera.main.DOShakePosition(duration, strength);
    }
}