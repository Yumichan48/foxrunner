using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "TweenConfig", menuName = "FoxRunner/Tween Configuration")]
public class TweenConfiguration : ScriptableObject
{
    [Header("=== UI TWEENS ===")]
    [Tooltip("UI fade duration")]
    [Range(0.1f, 2f)]
    public float uiFadeDuration = 0.3f;

    [Tooltip("UI slide duration")]
    [Range(0.1f, 2f)]
    public float uiSlideDuration = 0.5f;

    [Tooltip("UI bounce scale")]
    [Range(1f, 2f)]
    public float uiBounceScale = 1.2f;

    [Tooltip("UI ease type")]
    public Ease uiEaseType = Ease.OutBack;

    [Header("=== GAMEPLAY TWEENS ===")]
    [Tooltip("Coin collection tween")]
    public float coinCollectDuration = 0.5f;
    public Ease coinCollectEase = Ease.InBack;

    [Tooltip("Power-up pulse")]
    public float powerUpPulseDuration = 0.5f;
    public float powerUpPulseScale = 1.3f;

    [Header("=== CAMERA TWEENS ===")]
    [Tooltip("Camera shake settings")]
    public float shakeStrength = 0.3f;
    public int shakeVibrato = 10;
    public float shakeDuration = 0.5f;

    [Header("=== PERFORMANCE ===")]
    [Tooltip("Max simultaneous tweens")]
    public int maxTweens = 50;

    [Tooltip("Tween capacity")]
    public int tweenCapacity = 200;

    [Tooltip("Sequence capacity")]
    public int sequenceCapacity = 50;
}