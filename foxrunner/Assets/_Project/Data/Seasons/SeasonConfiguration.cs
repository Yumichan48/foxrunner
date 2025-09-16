using UnityEngine;

namespace FoxRunner.Seasons
{
    /// <summary>
    /// Configuration for the seasonal dimension system
    /// Defines seasonal behavior, transitions, and dimensional mechanics
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonConfig", menuName = "FoxRunner/Seasons/Season Configuration")]
    public class SeasonConfiguration : ScriptableObject
    {
        [Header("=== SEASON CYCLE ===")]
        [Tooltip("Duration of each season in real-time minutes")]
        [Range(5f, 60f)]
        public float seasonDurationMinutes = 20f;

        [Tooltip("Transition time between seasons in seconds")]
        [Range(5f, 30f)]
        public float transitionDurationSeconds = 10f;

        [Tooltip("Starting season when game begins")]
        public SeasonType startingSeason = SeasonType.Spring;

        [Tooltip("Enable automatic season progression")]
        public bool enableAutoProgression = true;

        [Header("=== DIMENSIONAL MECHANICS ===")]
        [Tooltip("Number of dimensions per season")]
        [Range(3, 10)]
        public int dimensionsPerSeason = 5;

        [Tooltip("Dimension progression curve")]
        public AnimationCurve dimensionDifficultyCurve = AnimationCurve.Linear(1f, 1f, 5f, 3f);

        [Tooltip("Dimensional energy cost multiplier")]
        [Range(1f, 5f)]
        public float dimensionalEnergyCostMultiplier = 1.5f;

        [Header("=== SEASON CONFIGURATIONS ===")]
        [Tooltip("Configuration for each season")]
        public SeasonalConfiguration[] seasonalConfigurations;

        [Header("=== WEATHER SYSTEM ===")]
        [Tooltip("Enable dynamic weather effects")]
        public bool enableWeatherSystem = true;

        [Tooltip("Weather transition speed")]
        [Range(0.1f, 5f)]
        public float weatherTransitionSpeed = 1f;

        [Tooltip("Weather configurations for each season")]
        public WeatherConfiguration[] weatherConfigurations;

        [Header("=== LIGHTING SYSTEM ===")]
        [Tooltip("Enable dynamic lighting based on seasons")]
        public bool enableSeasonalLighting = true;

        [Tooltip("Lighting transition duration")]
        [Range(1f, 30f)]
        public float lightingTransitionDuration = 5f;

        [Tooltip("Lighting configurations")]
        public LightingConfiguration[] lightingConfigurations;

        [Header("=== DIMENSIONAL REWARDS ===")]
        [Tooltip("Base reward multiplier for dimensional progression")]
        [Range(1f, 10f)]
        public float baseDimensionalRewardMultiplier = 2f;

        [Tooltip("Dimensional progression milestones")]
        public DimensionalMilestone[] dimensionalMilestones;

        [Header("=== SPECIAL EVENTS ===")]
        [Tooltip("Enable seasonal special events")]
        public bool enableSpecialEvents = true;

        [Tooltip("Special event configurations")]
        public SeasonalEvent[] specialEvents;

        [Header("=== DEBUG ===")]
        [Tooltip("Enable debug mode for faster testing")]
        public bool enableDebugMode = false;

        [Tooltip("Debug season duration multiplier")]
        [Range(0.01f, 1f)]
        public float debugTimeMultiplier = 1f;

        [Tooltip("Show dimensional debug info")]
        public bool showDimensionalDebugInfo = false;

        void OnValidate()
        {
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            // Validate season configurations
            if (seasonalConfigurations == null || seasonalConfigurations.Length != 4)
            {
                Debug.LogWarning("[SeasonConfiguration] Must have exactly 4 seasonal configurations (Spring, Summer, Autumn, Winter)!");
            }

            // Validate dimensions per season
            if (dimensionsPerSeason < 1)
            {
                Debug.LogWarning("[SeasonConfiguration] Dimensions per season must be at least 1!");
                dimensionsPerSeason = 1;
            }

            // Validate duration
            if (seasonDurationMinutes <= 0)
            {
                Debug.LogWarning("[SeasonConfiguration] Season duration must be greater than 0!");
                seasonDurationMinutes = 20f;
            }

            // Validate weather configurations
            if (enableWeatherSystem && (weatherConfigurations == null || weatherConfigurations.Length == 0))
            {
                Debug.LogWarning("[SeasonConfiguration] Weather system enabled but no weather configurations found!");
            }

            // Validate lighting configurations
            if (enableSeasonalLighting && (lightingConfigurations == null || lightingConfigurations.Length == 0))
            {
                Debug.LogWarning("[SeasonConfiguration] Seasonal lighting enabled but no lighting configurations found!");
            }
        }

        public SeasonalConfiguration GetSeasonConfiguration(SeasonType season)
        {
            if (seasonalConfigurations == null) return null;

            foreach (var config in seasonalConfigurations)
            {
                if (config.seasonType == season)
                    return config;
            }

            Debug.LogWarning($"[SeasonConfiguration] No configuration found for season: {season}");
            return null;
        }

        public WeatherConfiguration GetWeatherConfiguration(SeasonType season, WeatherType weatherType)
        {
            if (weatherConfigurations == null) return null;

            foreach (var config in weatherConfigurations)
            {
                if (config.season == season && config.weatherType == weatherType)
                    return config;
            }

            return null;
        }

        public LightingConfiguration GetLightingConfiguration(SeasonType season)
        {
            if (lightingConfigurations == null) return null;

            foreach (var config in lightingConfigurations)
            {
                if (config.season == season)
                    return config;
            }

            return null;
        }

        public float GetSeasonDuration()
        {
            float duration = seasonDurationMinutes * 60f; // Convert to seconds
            if (enableDebugMode)
            {
                duration *= debugTimeMultiplier;
            }
            return duration;
        }

        public float GetDimensionalDifficulty(int dimension)
        {
            float normalizedDimension = (float)dimension / dimensionsPerSeason;
            return dimensionDifficultyCurve.Evaluate(normalizedDimension);
        }

        public DimensionalMilestone GetDimensionalMilestone(int dimension)
        {
            if (dimensionalMilestones == null) return null;

            foreach (var milestone in dimensionalMilestones)
            {
                if (milestone.dimension == dimension)
                    return milestone;
            }

            return null;
        }
    }

    [System.Serializable]
    public class SeasonalConfiguration
    {
        [Header("Basic Data")]
        [Tooltip("Season type")]
        public SeasonType seasonType;

        [Tooltip("Display name")]
        public string displayName;

        [Tooltip("Description")]
        [TextArea(2, 4)]
        public string description;

        [Header("Environmental Settings")]
        [Tooltip("Ambient color for this season")]
        public Color ambientColor = Color.white;

        [Tooltip("Fog color")]
        public Color fogColor = Color.gray;

        [Tooltip("Fog density")]
        [Range(0f, 1f)]
        public float fogDensity = 0.1f;

        [Header("Gameplay Modifiers")]
        [Tooltip("Movement speed multiplier")]
        [Range(0.5f, 2f)]
        public float movementSpeedMultiplier = 1f;

        [Tooltip("Jump height multiplier")]
        [Range(0.5f, 2f)]
        public float jumpHeightMultiplier = 1f;

        [Tooltip("Collectible spawn rate multiplier")]
        [Range(0.5f, 3f)]
        public float collectibleSpawnMultiplier = 1f;

        [Header("Dimensional Properties")]
        [Tooltip("Dimensional energy drain rate")]
        [Range(0.5f, 3f)]
        public float dimensionalEnergyDrainRate = 1f;

        [Tooltip("Dimensional stability")]
        [Range(0.1f, 1f)]
        public float dimensionalStability = 1f;

        [Tooltip("Seasonal bonus effects")]
        public SeasonalBonus[] seasonalBonuses;

        [Header("Visual Elements")]
        [Tooltip("Particle system prefab for environmental effects")]
        public GameObject environmentalParticlesPrefab;

        [Tooltip("Material overrides for seasonal theming")]
        public MaterialOverride[] materialOverrides;

        [Tooltip("Seasonal music tracks")]
        public AudioClip[] musicTracks;

        [Header("Special Mechanics")]
        [Tooltip("Unique seasonal mechanics for this season")]
        public SeasonalMechanic[] uniqueMechanics;
    }

    [System.Serializable]
    public class WeatherConfiguration
    {
        [Tooltip("Season this weather belongs to")]
        public SeasonType season;

        [Tooltip("Weather type")]
        public WeatherType weatherType;

        [Tooltip("Display name")]
        public string displayName;

        [Tooltip("Probability of this weather occurring")]
        [Range(0f, 1f)]
        public float probability = 0.2f;

        [Tooltip("Minimum duration in seconds")]
        [Range(30f, 600f)]
        public float minDuration = 60f;

        [Tooltip("Maximum duration in seconds")]
        [Range(60f, 1200f)]
        public float maxDuration = 300f;

        [Header("Visual Effects")]
        [Tooltip("Weather particle system")]
        public GameObject weatherParticlesPrefab;

        [Tooltip("Screen overlay color")]
        public Color overlayColor = Color.clear;

        [Tooltip("Overlay intensity")]
        [Range(0f, 1f)]
        public float overlayIntensity = 0f;

        [Header("Gameplay Effects")]
        [Tooltip("Visibility reduction")]
        [Range(0f, 1f)]
        public float visibilityReduction = 0f;

        [Tooltip("Movement speed modifier")]
        [Range(0.1f, 2f)]
        public float movementSpeedModifier = 1f;

        [Tooltip("Audio effects")]
        public AudioClip[] weatherSounds;
    }

    [System.Serializable]
    public class LightingConfiguration
    {
        [Tooltip("Season")]
        public SeasonType season;

        [Tooltip("Ambient light color")]
        public Color ambientLightColor = Color.white;

        [Tooltip("Ambient light intensity")]
        [Range(0f, 2f)]
        public float ambientLightIntensity = 1f;

        [Tooltip("Directional light color")]
        public Color directionalLightColor = Color.white;

        [Tooltip("Directional light intensity")]
        [Range(0f, 3f)]
        public float directionalLightIntensity = 1f;

        [Tooltip("Shadow strength")]
        [Range(0f, 1f)]
        public float shadowStrength = 1f;

        [Tooltip("Skybox material")]
        public Material skyboxMaterial;
    }

    [System.Serializable]
    public class DimensionalMilestone
    {
        [Tooltip("Dimension number")]
        public int dimension;

        [Tooltip("Display name")]
        public string displayName;

        [Tooltip("Description")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Reward multiplier")]
        [Range(1f, 10f)]
        public float rewardMultiplier = 1.5f;

        [Tooltip("Special unlock")]
        public string specialUnlock;

        [Tooltip("Required dimensional energy")]
        public long requiredDimensionalEnergy;
    }

    [System.Serializable]
    public class SeasonalEvent
    {
        [Tooltip("Event name")]
        public string eventName;

        [Tooltip("Season this event occurs in")]
        public SeasonType season;

        [Tooltip("Event probability")]
        [Range(0f, 1f)]
        public float probability = 0.1f;

        [Tooltip("Event duration in seconds")]
        [Range(30f, 300f)]
        public float duration = 60f;

        [Tooltip("Event description")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Gameplay modifiers during event")]
        public GameplayModifier[] modifiers;

        [Tooltip("Special rewards")]
        public EventReward[] rewards;
    }

    [System.Serializable]
    public class SeasonalBonus
    {
        [Tooltip("Bonus type")]
        public SeasonalBonusType bonusType;

        [Tooltip("Bonus value")]
        public float bonusValue = 1f;

        [Tooltip("Description")]
        public string description;
    }

    [System.Serializable]
    public class MaterialOverride
    {
        [Tooltip("Target renderer name or tag")]
        public string targetName;

        [Tooltip("Seasonal material")]
        public Material seasonalMaterial;
    }

    [System.Serializable]
    public class SeasonalMechanic
    {
        [Tooltip("Mechanic name")]
        public string mechanicName;

        [Tooltip("Description")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Activation probability")]
        [Range(0f, 1f)]
        public float activationProbability = 0.1f;

        [Tooltip("Effect duration")]
        public float effectDuration = 10f;
    }

    [System.Serializable]
    public class GameplayModifier
    {
        [Tooltip("Modifier type")]
        public ModifierType modifierType;

        [Tooltip("Modifier value")]
        public float modifierValue = 1f;

        [Tooltip("Duration (-1 for permanent)")]
        public float duration = -1f;
    }

    [System.Serializable]
    public class EventReward
    {
        [Tooltip("Reward type")]
        public RewardType rewardType;

        [Tooltip("Reward amount")]
        public long rewardAmount = 100;

        [Tooltip("Special item")]
        public string specialItem;
    }
}