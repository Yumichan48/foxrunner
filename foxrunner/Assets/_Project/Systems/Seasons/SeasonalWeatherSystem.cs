using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxRunner.Seasons
{
    /// <summary>
    /// Manages weather effects and transitions within the seasonal system
    /// Handles weather probability, duration, visual effects, and gameplay impact
    /// </summary>
    public class SeasonalWeatherSystem : MonoBehaviour
    {
        #region Configuration
        private SeasonConfiguration config;
        #endregion

        #region Weather State
        [Header("=== WEATHER STATE (read only) ===")]
        [SerializeField] private WeatherType currentWeather;
        [SerializeField] private float weatherTimer;
        [SerializeField] private float weatherDuration;
        [SerializeField] private bool isTransitioning;
        [SerializeField] private float transitionProgress;
        #endregion

        #region Weather Effects
        private Dictionary<WeatherType, GameObject> activeWeatherEffects;
        private Dictionary<WeatherType, AudioSource> weatherAudioSources;
        private Camera mainCamera;
        private Material weatherOverlayMaterial;
        #endregion

        #region Events
        public Action<WeatherType> OnWeatherChanged;
        public Action<WeatherType, float> OnWeatherTransition; // weather, progress
        #endregion

        #region Properties
        public WeatherType CurrentWeather => currentWeather;
        public float WeatherProgress => weatherTimer / weatherDuration;
        public bool IsTransitioning => isTransitioning;
        #endregion

        #region Initialization
        public void Initialize(SeasonConfiguration seasonConfig)
        {
            config = seasonConfig;

            InitializeWeatherSystems();
            LoadWeatherState();

            Debug.Log("[SeasonalWeatherSystem] Initialized");
        }

        private void InitializeWeatherSystems()
        {
            activeWeatherEffects = new Dictionary<WeatherType, GameObject>();
            weatherAudioSources = new Dictionary<WeatherType, AudioSource>();

            mainCamera = Camera.main;
            if (!mainCamera)
            {
                Debug.LogWarning("[SeasonalWeatherSystem] Main camera not found!");
            }

            CreateWeatherOverlayMaterial();
        }

        private void CreateWeatherOverlayMaterial()
        {
            // Create a simple overlay material for weather effects
            weatherOverlayMaterial = new Material(Shader.Find("UI/Default"));
            weatherOverlayMaterial.color = Color.clear;
        }

        private void LoadWeatherState()
        {
            currentWeather = WeatherType.Clear;
            weatherTimer = 0f;
            weatherDuration = 0f;
        }
        #endregion

        #region Weather Management
        public void SetSeasonalWeather(SeasonType season)
        {
            // Select appropriate weather for the season
            WeatherType newWeather = SelectWeatherForSeason(season);
            ChangeWeather(newWeather);
        }

        private WeatherType SelectWeatherForSeason(SeasonType season)
        {
            // Get weather configurations for this season
            var availableWeathers = GetAvailableWeathersForSeason(season);
            if (availableWeathers.Count == 0)
            {
                return WeatherType.Clear;
            }

            // Select based on probability
            float totalProbability = 0f;
            foreach (var weather in availableWeathers)
            {
                totalProbability += weather.probability;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalProbability);
            float currentProbability = 0f;

            foreach (var weather in availableWeathers)
            {
                currentProbability += weather.probability;
                if (randomValue <= currentProbability)
                {
                    return weather.weatherType;
                }
            }

            return WeatherType.Clear;
        }

        private List<WeatherConfiguration> GetAvailableWeathersForSeason(SeasonType season)
        {
            var availableWeathers = new List<WeatherConfiguration>();

            if (config.weatherConfigurations != null)
            {
                foreach (var weatherConfig in config.weatherConfigurations)
                {
                    if (weatherConfig.season == season)
                    {
                        availableWeathers.Add(weatherConfig);
                    }
                }
            }

            return availableWeathers;
        }

        public void ChangeWeather(WeatherType newWeather)
        {
            if (currentWeather == newWeather) return;

            StartCoroutine(WeatherTransitionCoroutine(newWeather));
        }

        private IEnumerator WeatherTransitionCoroutine(WeatherType newWeather)
        {
            isTransitioning = true;
            transitionProgress = 0f;

            WeatherType previousWeather = currentWeather;
            float transitionDuration = config.weatherTransitionSpeed;

            Debug.Log($"[SeasonalWeatherSystem] Transitioning from {previousWeather} to {newWeather}");

            // Transition out old weather
            while (transitionProgress < 1f)
            {
                transitionProgress += Time.deltaTime / transitionDuration;

                ApplyWeatherTransition(previousWeather, newWeather, transitionProgress);
                OnWeatherTransition?.Invoke(newWeather, transitionProgress);

                yield return null;
            }

            // Complete transition
            CompleteWeatherTransition(newWeather);
            isTransitioning = false;
            transitionProgress = 0f;

            Debug.Log($"[SeasonalWeatherSystem] Weather transition to {newWeather} complete");
        }

        private void ApplyWeatherTransition(WeatherType fromWeather, WeatherType toWeather, float progress)
        {
            // Fade out old weather effects
            if (activeWeatherEffects.ContainsKey(fromWeather))
            {
                var oldEffect = activeWeatherEffects[fromWeather];
                if (oldEffect)
                {
                    FadeWeatherEffect(oldEffect, 1f - progress);
                }
            }

            // Fade in new weather effects
            if (activeWeatherEffects.ContainsKey(toWeather))
            {
                var newEffect = activeWeatherEffects[toWeather];
                if (newEffect)
                {
                    FadeWeatherEffect(newEffect, progress);
                }
            }
            else if (progress > 0.5f)
            {
                // Create new weather effect halfway through transition
                CreateWeatherEffect(toWeather);
            }

            // Update overlay
            UpdateWeatherOverlay(toWeather, progress);

            // Update audio
            UpdateWeatherAudio(fromWeather, toWeather, progress);
        }

        private void CompleteWeatherTransition(WeatherType newWeather)
        {
            // Clean up old weather effects
            CleanupInactiveWeatherEffects(newWeather);

            // Set new weather
            currentWeather = newWeather;

            // Set weather duration
            var weatherConfig = config.GetWeatherConfiguration(SeasonalSystem.Instance?.CurrentSeason ?? SeasonType.Spring, newWeather);
            if (weatherConfig != null)
            {
                weatherDuration = UnityEngine.Random.Range(weatherConfig.minDuration, weatherConfig.maxDuration);
            }
            else
            {
                weatherDuration = 300f; // Default 5 minutes
            }

            weatherTimer = 0f;

            // Notify weather change
            OnWeatherChanged?.Invoke(newWeather);
        }

        public void UpdateTransition(SeasonType fromSeason, SeasonType toSeason, float progress)
        {
            // Update weather effects during seasonal transitions
            // This allows weather to gradually change with the seasons
        }
        #endregion

        #region Weather Effects
        private void CreateWeatherEffect(WeatherType weatherType)
        {
            if (activeWeatherEffects.ContainsKey(weatherType)) return;

            var weatherConfig = GetCurrentWeatherConfig(weatherType);
            if (weatherConfig?.weatherParticlesPrefab == null) return;

            GameObject weatherEffect = Instantiate(weatherConfig.weatherParticlesPrefab);
            weatherEffect.name = $"Weather_{weatherType}";

            // Position relative to camera or world
            if (mainCamera)
            {
                weatherEffect.transform.SetParent(mainCamera.transform);
                weatherEffect.transform.localPosition = Vector3.forward * 10f;
            }

            activeWeatherEffects[weatherType] = weatherEffect;

            Debug.Log($"[SeasonalWeatherSystem] Created weather effect: {weatherType}");
        }

        private void FadeWeatherEffect(GameObject weatherEffect, float alpha)
        {
            if (!weatherEffect) return;

            // Fade particle systems
            var particleSystems = weatherEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                var color = main.startColor;
                if (color.mode == ParticleSystemGradientMode.Color)
                {
                    Color newColor = color.color;
                    newColor.a = alpha;
                    var newColorValue = new ParticleSystem.MinMaxGradient(newColor);
                    main.startColor = newColorValue;
                }
            }

            // Fade renderers
            var renderers = weatherEffect.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
            }
        }

        private void CleanupInactiveWeatherEffects(WeatherType activeWeather)
        {
            var toRemove = new List<WeatherType>();

            foreach (var kvp in activeWeatherEffects)
            {
                if (kvp.Key != activeWeather)
                {
                    if (kvp.Value)
                    {
                        Destroy(kvp.Value);
                    }
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var weather in toRemove)
            {
                activeWeatherEffects.Remove(weather);
            }
        }

        private void UpdateWeatherOverlay(WeatherType weatherType, float intensity)
        {
            if (!weatherOverlayMaterial) return;

            var weatherConfig = GetCurrentWeatherConfig(weatherType);
            if (weatherConfig == null) return;

            Color overlayColor = weatherConfig.overlayColor;
            overlayColor.a = weatherConfig.overlayIntensity * intensity;
            weatherOverlayMaterial.color = overlayColor;
        }

        private void UpdateWeatherAudio(WeatherType fromWeather, WeatherType toWeather, float progress)
        {
            // Fade out old weather audio
            if (weatherAudioSources.ContainsKey(fromWeather))
            {
                var oldAudio = weatherAudioSources[fromWeather];
                if (oldAudio)
                {
                    oldAudio.volume = (1f - progress) * 0.5f;
                }
            }

            // Fade in new weather audio
            if (weatherAudioSources.ContainsKey(toWeather))
            {
                var newAudio = weatherAudioSources[toWeather];
                if (newAudio)
                {
                    newAudio.volume = progress * 0.5f;
                }
            }
            else if (progress > 0.5f)
            {
                CreateWeatherAudio(toWeather);
            }
        }

        private void CreateWeatherAudio(WeatherType weatherType)
        {
            var weatherConfig = GetCurrentWeatherConfig(weatherType);
            if (weatherConfig?.weatherSounds == null || weatherConfig.weatherSounds.Length == 0) return;

            GameObject audioGO = new GameObject($"WeatherAudio_{weatherType}");
            audioGO.transform.SetParent(transform);

            AudioSource audioSource = audioGO.AddComponent<AudioSource>();
            audioSource.clip = weatherConfig.weatherSounds[0]; // Use first sound for now
            audioSource.loop = true;
            audioSource.volume = 0f;
            audioSource.Play();

            weatherAudioSources[weatherType] = audioSource;
        }

        private WeatherConfiguration GetCurrentWeatherConfig(WeatherType weatherType)
        {
            var seasonalSystem = FindObjectOfType<SeasonalSystem>();
            if (!seasonalSystem) return null;

            return config.GetWeatherConfiguration(seasonalSystem.CurrentSeason, weatherType);
        }
        #endregion

        #region System Updates
        public void UpdateSystem()
        {
            if (isTransitioning) return;

            // Update weather timer
            weatherTimer += Time.deltaTime;

            // Check for weather change
            if (weatherTimer >= weatherDuration)
            {
                SelectAndChangeWeather();
            }

            // Update active weather effects
            UpdateActiveWeatherEffects();
        }

        private void SelectAndChangeWeather()
        {
            var seasonalSystem = FindObjectOfType<SeasonalSystem>();
            if (seasonalSystem)
            {
                WeatherType newWeather = SelectWeatherForSeason(seasonalSystem.CurrentSeason);
                ChangeWeather(newWeather);
            }
        }

        private void UpdateActiveWeatherEffects()
        {
            // Update any dynamic weather effects here
            foreach (var kvp in activeWeatherEffects)
            {
                if (kvp.Value)
                {
                    UpdateWeatherEffectIntensity(kvp.Key, kvp.Value);
                }
            }
        }

        private void UpdateWeatherEffectIntensity(WeatherType weatherType, GameObject weatherEffect)
        {
            // Apply dynamic intensity changes based on various factors
            var weatherConfig = GetCurrentWeatherConfig(weatherType);
            if (weatherConfig == null) return;

            // Example: Vary intensity based on dimensional stability
            var seasonalSystem = FindObjectOfType<SeasonalSystem>();
            if (seasonalSystem)
            {
                float stabilityModifier = seasonalSystem.DimensionalStability;
                ApplyIntensityModifier(weatherEffect, stabilityModifier);
            }
        }

        private void ApplyIntensityModifier(GameObject weatherEffect, float modifier)
        {
            var particleSystems = weatherEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var emission = ps.emission;
                var rate = emission.rateOverTime;

                if (rate.mode == ParticleSystemCurveMode.Constant)
                {
                    emission.rateOverTime = rate.constant * modifier;
                }
            }
        }
        #endregion

        #region Public API
        public void ForceWeatherChange(WeatherType newWeather)
        {
            ChangeWeather(newWeather);
        }

        public float GetWeatherIntensity()
        {
            if (isTransitioning)
            {
                return transitionProgress;
            }

            return 1f;
        }

        public bool IsWeatherActive(WeatherType weatherType)
        {
            return currentWeather == weatherType;
        }

        public float GetTimeToWeatherChange()
        {
            return weatherDuration - weatherTimer;
        }

        public GameplayModifier[] GetCurrentWeatherModifiers()
        {
            var weatherConfig = GetCurrentWeatherConfig(currentWeather);
            if (weatherConfig == null) return new GameplayModifier[0];

            var modifiers = new List<GameplayModifier>();

            // Movement speed modifier
            if (weatherConfig.movementSpeedModifier != 1f)
            {
                modifiers.Add(new GameplayModifier
                {
                    modifierType = ModifierType.MovementSpeed,
                    modifierValue = weatherConfig.movementSpeedModifier,
                    duration = -1f // Permanent while weather is active
                });
            }

            // Visibility modifier (affects collection range, etc.)
            if (weatherConfig.visibilityReduction > 0f)
            {
                modifiers.Add(new GameplayModifier
                {
                    modifierType = ModifierType.AutoCollectionRadius,
                    modifierValue = 1f - weatherConfig.visibilityReduction,
                    duration = -1f
                });
            }

            return modifiers.ToArray();
        }
        #endregion

        #region Cleanup
        void OnDestroy()
        {
            // Clean up weather effects
            foreach (var kvp in activeWeatherEffects)
            {
                if (kvp.Value)
                {
                    Destroy(kvp.Value);
                }
            }

            // Clean up audio sources
            foreach (var kvp in weatherAudioSources)
            {
                if (kvp.Value)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            // Clean up materials
            if (weatherOverlayMaterial)
            {
                Destroy(weatherOverlayMaterial);
            }
        }
        #endregion
    }
}