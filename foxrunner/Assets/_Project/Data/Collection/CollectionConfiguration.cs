using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Collection
{
    /// <summary>
    /// Configuration for the collection system
    /// Defines collectible values, combo mechanics, and collection behavior
    /// </summary>
    [CreateAssetMenu(fileName = "CollectionConfig", menuName = "FoxRunner/Collection/Collection Configuration")]
    public class CollectionConfiguration : ScriptableObject
    {
        [Header("=== COMBO SYSTEM ===")]
        [Tooltip("Time window to maintain combo (seconds)")]
        [Range(1f, 10f)]
        public float comboTimeWindow = 3f;

        [Tooltip("Maximum combo time that can be accumulated")]
        [Range(5f, 30f)]
        public float maxComboTime = 15f;

        [Tooltip("Multiplier increase per combo step")]
        [Range(0.01f, 0.5f)]
        public float comboMultiplierPerStep = 0.1f;

        [Tooltip("Maximum combo multiplier")]
        [Range(2f, 20f)]
        public float maxComboMultiplier = 10f;

        [Tooltip("Combo count needed for maximum multiplier")]
        [Range(10, 200)]
        public int maxComboForFullMultiplier = 100;

        [Tooltip("Combo multiplier curve for diminishing returns")]
        public AnimationCurve comboMultiplierCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Combo milestones that trigger special effects")]
        public int[] comboMilestones = { 10, 25, 50, 100, 200, 500 };

        [Header("=== AUTO COLLECTION ===")]
        [Tooltip("Enable auto-collection system")]
        public bool enableAutoCollection = true;

        [Tooltip("Base auto-collection radius")]
        [Range(0f, 10f)]
        public float baseAutoCollectionRadius = 2f;

        [Tooltip("Auto-collection check interval (seconds)")]
        [Range(0.1f, 2f)]
        public float autoCollectionInterval = 0.5f;

        [Tooltip("Animation time for auto-collected items")]
        [Range(0.1f, 2f)]
        public float autoCollectionAnimationTime = 0.8f;

        [Tooltip("Animation curve for auto-collection movement")]
        public AnimationCurve autoCollectionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("=== COLLECTIBLE VALUES ===")]
        [Tooltip("Value configurations for each collectible type")]
        public CollectibleTypeData[] collectibleTypes;

        [Header("=== VISUAL EFFECTS ===")]
        [Tooltip("Particle effect for normal collection")]
        public GameObject collectionParticlePrefab;

        [Tooltip("Particle effect for combo milestones")]
        public GameObject milestoneParticlePrefab;

        [Tooltip("Show floating text on collection")]
        public bool showFloatingText = true;

        [Tooltip("Collection processing delay for visual clarity")]
        [Range(0f, 0.5f)]
        public float collectionProcessingDelay = 0.05f;

        [Header("=== SCREEN EFFECTS ===")]
        [Tooltip("Combo count required for screen shake")]
        [Range(5, 50)]
        public int screenShakeComboThreshold = 20;

        [Tooltip("Screen shake intensity curve based on combo")]
        public AnimationCurve screenShakeIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("=== AUDIO ===")]
        [Tooltip("Audio configurations for collection sounds")]
        public CollectionAudioConfiguration[] audioConfigurations;

        [Header("=== BALANCE ===")]
        [Tooltip("Value scaling with player level")]
        public AnimationCurve levelScalingCurve = AnimationCurve.Linear(1f, 1f, 500f, 5f);

        [Tooltip("Seasonal bonus multipliers")]
        public SeasonalBonusConfiguration[] seasonalBonuses;

        [Header("=== DEBUG ===")]
        [Tooltip("Show debug gizmos in scene view")]
        public bool showDebugGizmos = true;

        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        [Tooltip("Debug value multiplier")]
        [Range(0.1f, 100f)]
        public float debugValueMultiplier = 1f;

        void OnValidate()
        {
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            // Validate combo system
            if (comboTimeWindow <= 0)
            {
                Debug.LogWarning("[CollectionConfiguration] Combo time window must be greater than 0!");
                comboTimeWindow = 3f;
            }

            if (maxComboMultiplier < 1f)
            {
                Debug.LogWarning("[CollectionConfiguration] Max combo multiplier must be at least 1!");
                maxComboMultiplier = 10f;
            }

            // Validate collectible types
            if (collectibleTypes == null || collectibleTypes.Length == 0)
            {
                Debug.LogWarning("[CollectionConfiguration] No collectible types configured!");
            }
            else
            {
                foreach (var type in collectibleTypes)
                {
                    if (type.baseCoinValue < 0)
                    {
                        Debug.LogWarning($"[CollectionConfiguration] {type.type} has negative coin value!");
                    }
                }
            }

            // Validate milestones
            if (comboMilestones != null && comboMilestones.Length > 0)
            {
                for (int i = 1; i < comboMilestones.Length; i++)
                {
                    if (comboMilestones[i] <= comboMilestones[i - 1])
                    {
                        Debug.LogWarning("[CollectionConfiguration] Combo milestones should be in ascending order!");
                        break;
                    }
                }
            }
        }

        public CollectibleTypeData GetCollectibleData(CollectibleType type)
        {
            if (collectibleTypes == null) return null;

            foreach (var typeData in collectibleTypes)
            {
                if (typeData.type == type)
                    return typeData;
            }

            Debug.LogWarning($"[CollectionConfiguration] No data found for collectible type: {type}");
            return null;
        }

        public CollectionAudioConfiguration GetAudioConfiguration(CollectibleType type)
        {
            if (audioConfigurations == null) return null;

            foreach (var audioConfig in audioConfigurations)
            {
                if (audioConfig.collectibleType == type)
                    return audioConfig;
            }

            return null;
        }

        public float GetSeasonalBonus(SeasonType season, CollectibleType type)
        {
            if (seasonalBonuses == null) return 1f;

            foreach (var bonus in seasonalBonuses)
            {
                if (bonus.season == season && bonus.collectibleType == type)
                    return bonus.multiplier;
            }

            return 1f;
        }

        public float GetLevelScaling(int playerLevel)
        {
            return levelScalingCurve.Evaluate(playerLevel) * debugValueMultiplier;
        }
    }

    [System.Serializable]
    public class CollectibleTypeData
    {
        [Header("Basic Data")]
        [Tooltip("Collectible type")]
        public CollectibleType type;

        [Tooltip("Display name")]
        public string displayName;

        [Tooltip("Description")]
        [TextArea(2, 4)]
        public string description;

        [Header("Base Values")]
        [Tooltip("Base coin value")]
        public long baseCoinValue = 1;

        [Tooltip("Base experience value")]
        public long baseExperienceValue = 1;

        [Tooltip("Base spirit point value")]
        public long baseSpiritPointValue = 0;

        [Header("Special Properties")]
        [Tooltip("Special bonus currency for rare/special variants")]
        public CurrencyType specialBonusCurrency = CurrencyType.Coins;

        [Tooltip("Special bonus amount")]
        public long specialBonusAmount = 0;

        [Tooltip("Drop weight (affects spawn frequency)")]
        [Range(0.1f, 10f)]
        public float dropWeight = 1f;

        [Tooltip("Can this collectible be auto-collected?")]
        public bool canAutoCollect = true;

        [Header("Visual Configuration")]
        [Tooltip("Prefab for this collectible type")]
        public GameObject prefab;

        [Tooltip("Icon for UI display")]
        public Sprite icon;

        [Tooltip("Base color for this collectible type")]
        public Color baseColor = Color.white;
    }

    [System.Serializable]
    public class CollectionAudioConfiguration
    {
        [Tooltip("Collectible type")]
        public CollectibleType collectibleType;

        [Tooltip("Audio clip for normal collection")]
        public AudioClip normalCollectionClip;

        [Tooltip("Audio clip for combo collection")]
        public AudioClip comboCollectionClip;

        [Tooltip("Audio clip for special collection")]
        public AudioClip specialCollectionClip;

        [Tooltip("Volume modifier")]
        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;

        [Tooltip("Pitch variation range")]
        [Range(0f, 1f)]
        public float pitchVariation = 0.1f;
    }

    [System.Serializable]
    public class SeasonalBonusConfiguration
    {
        [Tooltip("Season type")]
        public SeasonType season;

        [Tooltip("Collectible type")]
        public CollectibleType collectibleType;

        [Tooltip("Bonus multiplier during this season")]
        [Range(0.1f, 10f)]
        public float multiplier = 1.5f;

        [Tooltip("Description of the bonus")]
        public string description;
    }
}