using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Seasons
{
    /// <summary>
    /// Core seasonal system managing season cycles, transitions, and dimensional mechanics
    /// Handles weather, lighting, environmental effects, and seasonal bonuses
    /// </summary>
    public class SeasonalSystem : MonoBehaviour
    {
        #region Singleton
        private static SeasonalSystem _instance;
        public static SeasonalSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SeasonalSystem>();
                }
                return _instance;
            }
        }
        #endregion
        #region Configuration
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private SeasonConfiguration config;

        [Header("=== DEBUG INFO (read only) ===")]
        [SerializeField] private SeasonType currentSeason;
        [SerializeField] private int currentDimension;
        [SerializeField] private float seasonTimer;
        [SerializeField] private float transitionProgress;
        [SerializeField] private bool isTransitioning;
        [SerializeField] private DimensionalState dimensionalState;
        #endregion

        #region Core Systems
        private SeasonalWeatherSystem weatherSystem;
        private SeasonalLightingSystem lightingSystem;
        private DimensionalEnergySystem energySystem;
        private SeasonalEventsSystem eventsSystem;
        #endregion

        #region Season State
        private float seasonStartTime;
        private float lastSeasonDuration;
        private SeasonType previousSeason;
        private WeatherType currentWeather;
        private bool systemInitialized;
        #endregion

        #region Dimensional Data
        private Dictionary<SeasonType, int> seasonalDimensionProgress;
        private Dictionary<SeasonType, long> dimensionalEnergyStored;
        private List<DimensionalMilestone> unlockedMilestones;
        private float dimensionalStability;
        private long totalDimensionalEnergy;
        #endregion

        #region Events
        public static Action<SeasonType, SeasonType> OnSeasonChanged; // previous, new
        public static Action<float> OnSeasonTransitionProgress; // 0-1 progress
        public static Action<int, SeasonType> OnDimensionChanged; // dimension, season
        public static Action<WeatherType> OnWeatherChanged;
        public static Action<DimensionalState> OnDimensionalStateChanged;
        public static Action<long> OnDimensionalEnergyChanged;
        public static Action<DimensionalMilestone> OnMilestoneUnlocked;
        #endregion

        #region Properties
        public bool IsInitialized => systemInitialized;
        public SeasonType CurrentSeason => currentSeason;
        public int CurrentDimension => currentDimension;
        public float SeasonProgress => seasonTimer / config.GetSeasonDuration();
        public bool IsTransitioning => isTransitioning;
        public DimensionalState DimensionalState => dimensionalState;
        public float DimensionalStability => dimensionalStability;
        public long TotalDimensionalEnergy => totalDimensionalEnergy;
        public WeatherType CurrentWeather => currentWeather;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            ValidateConfiguration();
            InitializeDataStructures();
        }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (!systemInitialized) return;

            UpdateSeasonTimer();
            UpdateSystems();
            UpdateDimensionalState();
        }
        #endregion

        #region Initialization
        private void ValidateConfiguration()
        {
            if (!config)
            {
                Debug.LogError("[SeasonalSystem] Configuration missing! Creating default configuration.");
                config = ScriptableObject.CreateInstance<SeasonConfiguration>();
            }
        }

        private void InitializeDataStructures()
        {
            seasonalDimensionProgress = new Dictionary<SeasonType, int>();
            dimensionalEnergyStored = new Dictionary<SeasonType, long>();
            unlockedMilestones = new List<DimensionalMilestone>();

            // Initialize progression for all seasons
            foreach (SeasonType season in Enum.GetValues(typeof(SeasonType)))
            {
                seasonalDimensionProgress[season] = 1;
                dimensionalEnergyStored[season] = 0;
            }
        }

        public void Initialize()
        {
            try
            {
                Debug.Log("[SeasonalSystem] Initializing seasonal dimension system...");

                InitializeSubSystems();
                LoadSeasonalData();
                StartSeason(config.startingSeason);

                systemInitialized = true;
                Debug.Log("[SeasonalSystem] Initialization complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SeasonalSystem] Initialization failed: {e.Message}");
                systemInitialized = false;
            }
        }

        private void InitializeSubSystems()
        {
            // Initialize weather system
            weatherSystem = GetComponent<SeasonalWeatherSystem>();
            if (!weatherSystem)
            {
                weatherSystem = gameObject.AddComponent<SeasonalWeatherSystem>();
            }
            weatherSystem.Initialize(config);

            // Initialize lighting system
            lightingSystem = GetComponent<SeasonalLightingSystem>();
            if (!lightingSystem)
            {
                lightingSystem = gameObject.AddComponent<SeasonalLightingSystem>();
            }
            lightingSystem.Initialize(config);

            // Initialize energy system
            energySystem = GetComponent<DimensionalEnergySystem>();
            if (!energySystem)
            {
                energySystem = gameObject.AddComponent<DimensionalEnergySystem>();
            }
            energySystem.Initialize(config);

            // Initialize events system
            eventsSystem = GetComponent<SeasonalEventsSystem>();
            if (!eventsSystem)
            {
                eventsSystem = gameObject.AddComponent<SeasonalEventsSystem>();
            }
            eventsSystem.Initialize(config);

            // Subscribe to subsystem events
            SubscribeToSubSystemEvents();
        }

        private void SubscribeToSubSystemEvents()
        {
            if (weatherSystem != null)
                weatherSystem.OnWeatherChanged += HandleWeatherChanged;

            if (energySystem != null)
            {
                energySystem.OnEnergyChanged += HandleDimensionalEnergyChanged;
                energySystem.OnStabilityChanged += HandleDimensionalStabilityChanged;
            }

            if (eventsSystem != null)
                eventsSystem.OnEventTriggered += HandleSeasonalEvent;
        }

        private void LoadSeasonalData()
        {
            // Load from save data if available
            // For now, use defaults
            currentSeason = config.startingSeason;
            currentDimension = 1;
            dimensionalStability = 1f;
            dimensionalState = DimensionalState.Stable;
        }
        #endregion

        #region Season Management
        public void StartSeason(SeasonType season)
        {
            if (isTransitioning) return;

            previousSeason = currentSeason;
            currentSeason = season;
            seasonStartTime = Time.time;
            seasonTimer = 0f;

            ApplySeasonalEffects();
            NotifySeasonChanged();

            Debug.Log($"[SeasonalSystem] Started season: {season}");
        }

        public void TransitionToNextSeason()
        {
            if (isTransitioning) return;

            SeasonType nextSeason = GetNextSeason(currentSeason);
            StartCoroutine(TransitionToSeasonCoroutine(nextSeason));
        }

        private SeasonType GetNextSeason(SeasonType current)
        {
            return current switch
            {
                SeasonType.Spring => SeasonType.Summer,
                SeasonType.Summer => SeasonType.Autumn,
                SeasonType.Autumn => SeasonType.Winter,
                SeasonType.Winter => SeasonType.Spring,
                _ => SeasonType.Spring
            };
        }

        private IEnumerator TransitionToSeasonCoroutine(SeasonType newSeason)
        {
            isTransitioning = true;
            transitionProgress = 0f;

            float transitionDuration = config.transitionDurationSeconds;
            float elapsedTime = 0f;

            Debug.Log($"[SeasonalSystem] Transitioning from {currentSeason} to {newSeason}");

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                transitionProgress = elapsedTime / transitionDuration;

                // Update transition progress for visual effects
                OnSeasonTransitionProgress?.Invoke(transitionProgress);

                // Update subsystems with transition progress
                lightingSystem?.UpdateTransition(currentSeason, newSeason, transitionProgress);
                weatherSystem?.UpdateTransition(currentSeason, newSeason, transitionProgress);

                yield return null;
            }

            // Complete transition
            StartSeason(newSeason);
            isTransitioning = false;
            transitionProgress = 0f;

            Debug.Log($"[SeasonalSystem] Transition to {newSeason} complete");
        }

        private void ApplySeasonalEffects()
        {
            var seasonConfig = config.GetSeasonConfiguration(currentSeason);
            if (seasonConfig == null) return;

            // Apply lighting
            lightingSystem?.ApplySeasonalLighting(currentSeason);

            // Set weather
            weatherSystem?.SetSeasonalWeather(currentSeason);

            // Apply dimensional properties
            ApplyDimensionalProperties(seasonConfig);

            // Apply gameplay modifiers
            ApplyGameplayModifiers(seasonConfig);
        }

        private void ApplyDimensionalProperties(SeasonalConfiguration seasonConfig)
        {
            dimensionalState = DimensionalState.Stable;
            dimensionalStability = seasonConfig.dimensionalStability;

            OnDimensionalStateChanged?.Invoke(dimensionalState);
        }

        private void ApplyGameplayModifiers(SeasonalConfiguration seasonConfig)
        {
            // Apply seasonal bonuses
            if (seasonConfig.seasonalBonuses != null)
            {
                foreach (var bonus in seasonConfig.seasonalBonuses)
                {
                    ApplySeasonalBonus(bonus);
                }
            }
        }

        private void ApplySeasonalBonus(SeasonalBonus bonus)
        {
            // Bonuses will be applied through events that other systems listen to
            Debug.Log($"[SeasonalSystem] Applying seasonal bonus: {bonus.bonusType} = {bonus.bonusValue}");
        }

        private void NotifySeasonChanged()
        {
            OnSeasonChanged?.Invoke(previousSeason, currentSeason);
        }
        #endregion

        #region Dimensional System
        public bool AdvanceDimension()
        {
            if (currentDimension >= config.dimensionsPerSeason) return false;

            // Check if player has enough dimensional energy
            long requiredEnergy = GetRequiredDimensionalEnergy(currentDimension + 1);
            if (totalDimensionalEnergy < requiredEnergy) return false;

            // Consume energy
            SpendDimensionalEnergy(requiredEnergy);

            // Advance dimension
            currentDimension++;
            seasonalDimensionProgress[currentSeason] = currentDimension;

            // Check for milestones
            CheckDimensionalMilestones();

            OnDimensionChanged?.Invoke(currentDimension, currentSeason);
            Debug.Log($"[SeasonalSystem] Advanced to dimension {currentDimension} in {currentSeason}");

            return true;
        }

        public bool CanAdvanceDimension()
        {
            if (currentDimension >= config.dimensionsPerSeason) return false;

            long requiredEnergy = GetRequiredDimensionalEnergy(currentDimension + 1);
            return totalDimensionalEnergy >= requiredEnergy;
        }

        public long GetRequiredDimensionalEnergy(int dimension)
        {
            float baseCost = 1000f; // Base dimensional energy cost
            float difficulty = config.GetDimensionalDifficulty(dimension);
            float seasonMultiplier = config.dimensionalEnergyCostMultiplier;

            return (long)(baseCost * difficulty * seasonMultiplier * dimension);
        }

        public void EarnDimensionalEnergy(long amount, string source = "Unknown")
        {
            totalDimensionalEnergy += amount;
            dimensionalEnergyStored[currentSeason] += amount;

            OnDimensionalEnergyChanged?.Invoke(totalDimensionalEnergy);

            Debug.Log($"[SeasonalSystem] Earned {amount} dimensional energy from {source}");
        }

        public bool SpendDimensionalEnergy(long amount)
        {
            if (totalDimensionalEnergy < amount) return false;

            totalDimensionalEnergy -= amount;

            // Deduct from current season's storage first
            if (dimensionalEnergyStored[currentSeason] >= amount)
            {
                dimensionalEnergyStored[currentSeason] -= amount;
            }
            else
            {
                // Distribute across all seasons
                long remaining = amount;
                foreach (var season in Enum.GetValues(typeof(SeasonType)))
                {
                    var seasonType = (SeasonType)season;
                    long available = dimensionalEnergyStored[seasonType];
                    long toDeduct = Math.Min(available, remaining);

                    dimensionalEnergyStored[seasonType] -= toDeduct;
                    remaining -= toDeduct;

                    if (remaining <= 0) break;
                }
            }

            OnDimensionalEnergyChanged?.Invoke(totalDimensionalEnergy);
            return true;
        }

        private void CheckDimensionalMilestones()
        {
            var milestone = config.GetDimensionalMilestone(currentDimension);
            if (milestone != null && !unlockedMilestones.Contains(milestone))
            {
                unlockedMilestones.Add(milestone);
                OnMilestoneUnlocked?.Invoke(milestone);

                Debug.Log($"[SeasonalSystem] Milestone unlocked: {milestone.displayName}");
            }
        }
        #endregion

        #region System Updates
        private void UpdateSeasonTimer()
        {
            if (isTransitioning) return;

            seasonTimer += Time.deltaTime;

            // Check for automatic season progression
            if (config.enableAutoProgression && seasonTimer >= config.GetSeasonDuration())
            {
                TransitionToNextSeason();
            }
        }

        private void UpdateSystems()
        {
            weatherSystem?.UpdateSystem();
            lightingSystem?.UpdateSystem();
            energySystem?.UpdateSystem();
            eventsSystem?.UpdateSystem();
        }

        private void UpdateDimensionalState()
        {
            // Update dimensional stability based on various factors
            float targetStability = CalculateTargetStability();
            dimensionalStability = Mathf.Lerp(dimensionalStability, targetStability, Time.deltaTime * 0.5f);

            // Update dimensional state based on stability
            DimensionalState newState = CalculateDimensionalState();
            if (newState != dimensionalState)
            {
                dimensionalState = newState;
                OnDimensionalStateChanged?.Invoke(dimensionalState);
            }
        }

        private float CalculateTargetStability()
        {
            var seasonConfig = config.GetSeasonConfiguration(currentSeason);
            float baseStability = seasonConfig?.dimensionalStability ?? 1f;

            // Apply modifiers based on current dimension, weather, etc.
            float dimensionModifier = 1f - (currentDimension * 0.05f); // Higher dimensions are less stable
            float weatherModifier = GetWeatherStabilityModifier();

            return Mathf.Clamp01(baseStability * dimensionModifier * weatherModifier);
        }

        private float GetWeatherStabilityModifier()
        {
            return currentWeather switch
            {
                WeatherType.Clear => 1f,
                WeatherType.Cloudy => 0.95f,
                WeatherType.Rainy => 0.9f,
                WeatherType.Stormy => 0.8f,
                WeatherType.Snowy => 0.85f,
                WeatherType.Foggy => 0.9f,
                WeatherType.Windy => 0.92f,
                WeatherType.Misty => 0.88f,
                _ => 1f
            };
        }

        private DimensionalState CalculateDimensionalState()
        {
            if (dimensionalStability > 0.8f) return DimensionalState.Stable;
            if (dimensionalStability > 0.6f) return DimensionalState.Unstable;
            if (dimensionalStability > 0.3f) return DimensionalState.Collapsing;
            return DimensionalState.Transitioning;
        }
        #endregion

        #region Event Handlers
        private void HandleWeatherChanged(WeatherType newWeather)
        {
            currentWeather = newWeather;
            OnWeatherChanged?.Invoke(newWeather);
            Debug.Log($"[SeasonalSystem] Weather changed to: {newWeather}");
        }

        private void HandleDimensionalEnergyChanged(long newAmount)
        {
            totalDimensionalEnergy = newAmount;
            OnDimensionalEnergyChanged?.Invoke(newAmount);
        }

        private void HandleDimensionalStabilityChanged(float newStability)
        {
            dimensionalStability = newStability;
        }

        private void HandleSeasonalEvent(SeasonalEvent seasonalEvent)
        {
            Debug.Log($"[SeasonalSystem] Seasonal event triggered: {seasonalEvent.eventName}");
            // Apply event effects
        }
        #endregion

        #region Public API
        public void ForceSeasonChange(SeasonType newSeason)
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionToSeasonCoroutine(newSeason));
        }

        public void SetDimensionalStability(float stability)
        {
            dimensionalStability = Mathf.Clamp01(stability);
            OnDimensionalStateChanged?.Invoke(CalculateDimensionalState());
        }

        public SeasonalConfiguration GetCurrentSeasonConfiguration()
        {
            return config.GetSeasonConfiguration(currentSeason);
        }

        public int GetSeasonalDimensionProgress(SeasonType season)
        {
            return seasonalDimensionProgress.GetValueOrDefault(season, 1);
        }

        public long GetSeasonalEnergyStored(SeasonType season)
        {
            return dimensionalEnergyStored.GetValueOrDefault(season, 0);
        }

        public bool IsMilestoneUnlocked(int dimension)
        {
            var milestone = config.GetDimensionalMilestone(dimension);
            return milestone != null && unlockedMilestones.Contains(milestone);
        }

        public float GetTimeToNextSeason()
        {
            if (isTransitioning) return 0f;
            return config.GetSeasonDuration() - seasonTimer;
        }
        #endregion

        #region Save/Load Support
        public SeasonalSaveData GetSaveData()
        {
            return new SeasonalSaveData
            {
                currentSeason = currentSeason,
                currentDimension = currentDimension,
                seasonTimer = seasonTimer,
                dimensionalStability = dimensionalStability,
                totalDimensionalEnergy = totalDimensionalEnergy,
                seasonalDimensionProgress = new Dictionary<SeasonType, int>(seasonalDimensionProgress),
                dimensionalEnergyStored = new Dictionary<SeasonType, long>(dimensionalEnergyStored),
                unlockedMilestoneIds = unlockedMilestones.ConvertAll(m => m.dimension)
            };
        }

        public void LoadFromSaveData(SeasonalSaveData saveData)
        {
            if (saveData == null)
            {
                LoadDefaultData();
                return;
            }

            currentSeason = saveData.currentSeason;
            currentDimension = saveData.currentDimension;
            seasonTimer = saveData.seasonTimer;
            dimensionalStability = saveData.dimensionalStability;
            totalDimensionalEnergy = saveData.totalDimensionalEnergy;

            if (saveData.seasonalDimensionProgress != null)
                seasonalDimensionProgress = new Dictionary<SeasonType, int>(saveData.seasonalDimensionProgress);

            if (saveData.dimensionalEnergyStored != null)
                dimensionalEnergyStored = new Dictionary<SeasonType, long>(saveData.dimensionalEnergyStored);

            // Restore unlocked milestones
            unlockedMilestones.Clear();
            if (saveData.unlockedMilestoneIds != null)
            {
                foreach (int milestoneId in saveData.unlockedMilestoneIds)
                {
                    var milestone = config.GetDimensionalMilestone(milestoneId);
                    if (milestone != null)
                        unlockedMilestones.Add(milestone);
                }
            }
        }

        private void LoadDefaultData()
        {
            currentSeason = config.startingSeason;
            currentDimension = 1;
            seasonTimer = 0f;
            dimensionalStability = 1f;
            totalDimensionalEnergy = 0;

            seasonalDimensionProgress.Clear();
            dimensionalEnergyStored.Clear();
            unlockedMilestones.Clear();

            foreach (SeasonType season in Enum.GetValues(typeof(SeasonType)))
            {
                seasonalDimensionProgress[season] = 1;
                dimensionalEnergyStored[season] = 0;
            }
        }
        #endregion
    }

    #region Supporting Data Structures
    [System.Serializable]
    public class SeasonalSaveData
    {
        public SeasonType currentSeason;
        public int currentDimension;
        public float seasonTimer;
        public float dimensionalStability;
        public long totalDimensionalEnergy;
        public Dictionary<SeasonType, int> seasonalDimensionProgress;
        public Dictionary<SeasonType, long> dimensionalEnergyStored;
        public List<int> unlockedMilestoneIds;
    }
    #endregion
}