using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoxRunner.Seasons
{
    /// <summary>
    /// Manages dynamic lighting changes based on seasons and time
    /// Handles ambient lighting, directional light, shadows, and skybox transitions
    /// </summary>
    public class SeasonalLightingSystem : MonoBehaviour
    {
        #region Configuration
        private SeasonConfiguration config;
        #endregion

        #region Lighting Components
        [Header("=== LIGHTING COMPONENTS ===")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool autoFindComponents = true;
        #endregion

        #region Lighting State
        [Header("=== LIGHTING STATE (read only) ===")]
        [SerializeField] private SeasonType currentSeason;
        [SerializeField] private bool isTransitioning;
        [SerializeField] private float transitionProgress;
        #endregion

        #region Lighting Data
        private LightingConfiguration currentLightingConfig;
        private LightingConfiguration targetLightingConfig;
        private Material originalSkybox;
        private Color originalAmbientColor;
        private float originalAmbientIntensity;
        private Color originalDirectionalColor;
        private float originalDirectionalIntensity;
        private float originalShadowStrength;
        #endregion

        #region Initialization
        public void Initialize(SeasonConfiguration seasonConfig)
        {
            config = seasonConfig;

            FindLightingComponents();
            StoreLightingDefaults();
            LoadLightingState();

            Debug.Log("[SeasonalLightingSystem] Initialized");
        }

        private void FindLightingComponents()
        {
            if (!autoFindComponents) return;

            // Find directional light (usually the sun)
            if (!directionalLight)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }

            // Find main camera
            if (!mainCamera)
            {
                mainCamera = Camera.main;
            }

            if (!directionalLight)
            {
                Debug.LogWarning("[SeasonalLightingSystem] No directional light found! Creating default sun light.");
                CreateDefaultDirectionalLight();
            }

            if (!mainCamera)
            {
                Debug.LogWarning("[SeasonalLightingSystem] No main camera found!");
            }
        }

        private void CreateDefaultDirectionalLight()
        {
            GameObject sunLightGO = new GameObject("Seasonal Sun Light");
            sunLightGO.transform.SetParent(transform);
            sunLightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            directionalLight = sunLightGO.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = Color.white;
            directionalLight.intensity = 1f;
            directionalLight.shadows = LightShadows.Soft;
        }

        private void StoreLightingDefaults()
        {
            // Store original lighting settings to restore if needed
            if (directionalLight)
            {
                originalDirectionalColor = directionalLight.color;
                originalDirectionalIntensity = directionalLight.intensity;
                originalShadowStrength = directionalLight.shadowStrength;
            }

            originalAmbientColor = RenderSettings.ambientLight;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalSkybox = RenderSettings.skybox;
        }

        private void LoadLightingState()
        {
            currentSeason = SeasonType.Spring;
            isTransitioning = false;
            transitionProgress = 0f;
        }
        #endregion

        #region Lighting Management
        public void ApplySeasonalLighting(SeasonType season)
        {
            if (!config.enableSeasonalLighting) return;

            var lightingConfig = config.GetLightingConfiguration(season);
            if (lightingConfig == null)
            {
                Debug.LogWarning($"[SeasonalLightingSystem] No lighting configuration found for season: {season}");
                return;
            }

            currentSeason = season;
            currentLightingConfig = lightingConfig;

            ApplyLightingImmediate(lightingConfig);

            Debug.Log($"[SeasonalLightingSystem] Applied lighting for season: {season}");
        }

        public void UpdateTransition(SeasonType fromSeason, SeasonType toSeason, float progress)
        {
            if (!config.enableSeasonalLighting) return;

            var fromConfig = config.GetLightingConfiguration(fromSeason);
            var toConfig = config.GetLightingConfiguration(toSeason);

            if (fromConfig == null || toConfig == null) return;

            isTransitioning = true;
            transitionProgress = progress;
            targetLightingConfig = toConfig;

            ApplyLightingTransition(fromConfig, toConfig, progress);
        }

        private void ApplyLightingImmediate(LightingConfiguration lightingConfig)
        {
            // Apply ambient lighting
            RenderSettings.ambientLight = lightingConfig.ambientLightColor;
            RenderSettings.ambientIntensity = lightingConfig.ambientLightIntensity;

            // Apply directional light settings
            if (directionalLight)
            {
                directionalLight.color = lightingConfig.directionalLightColor;
                directionalLight.intensity = lightingConfig.directionalLightIntensity;
                directionalLight.shadowStrength = lightingConfig.shadowStrength;
            }

            // Apply skybox
            if (lightingConfig.skyboxMaterial)
            {
                RenderSettings.skybox = lightingConfig.skyboxMaterial;
            }

            // Update dynamic lighting
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyLightingTransition(LightingConfiguration fromConfig, LightingConfiguration toConfig, float progress)
        {
            // Interpolate ambient lighting
            Color ambientColor = Color.Lerp(fromConfig.ambientLightColor, toConfig.ambientLightColor, progress);
            float ambientIntensity = Mathf.Lerp(fromConfig.ambientLightIntensity, toConfig.ambientLightIntensity, progress);

            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;

            // Interpolate directional light
            if (directionalLight)
            {
                directionalLight.color = Color.Lerp(fromConfig.directionalLightColor, toConfig.directionalLightColor, progress);
                directionalLight.intensity = Mathf.Lerp(fromConfig.directionalLightIntensity, toConfig.directionalLightIntensity, progress);
                directionalLight.shadowStrength = Mathf.Lerp(fromConfig.shadowStrength, toConfig.shadowStrength, progress);
            }

            // Transition skybox at midpoint
            if (progress >= 0.5f && toConfig.skyboxMaterial && RenderSettings.skybox != toConfig.skyboxMaterial)
            {
                RenderSettings.skybox = toConfig.skyboxMaterial;
            }

            // Update dynamic lighting periodically during transition
            if (Time.frameCount % 10 == 0) // Every 10 frames
            {
                DynamicGI.UpdateEnvironment();
            }
        }
        #endregion

        #region Time-Based Lighting
        public void UpdateTimeBasedLighting(float timeOfDay)
        {
            if (!directionalLight || currentLightingConfig == null) return;

            // Update sun position based on time of day (0-1)
            float sunAngle = (timeOfDay * 360f) - 90f; // -90 to start at sunrise
            Vector3 sunRotation = new Vector3(sunAngle, -30f, 0f);
            directionalLight.transform.rotation = Quaternion.Euler(sunRotation);

            // Adjust intensity based on sun position
            float intensityMultiplier = CalculateSunIntensityMultiplier(timeOfDay);
            directionalLight.intensity = currentLightingConfig.directionalLightIntensity * intensityMultiplier;

            // Adjust color temperature
            Color timeColor = CalculateTimeBasedColor(timeOfDay);
            directionalLight.color = Color.Lerp(currentLightingConfig.directionalLightColor, timeColor, 0.5f);
        }

        private float CalculateSunIntensityMultiplier(float timeOfDay)
        {
            // Create a curve where intensity is highest at midday (0.5) and lowest at midnight (0.0, 1.0)
            float adjustedTime = (timeOfDay + 0.5f) % 1.0f; // Shift so midnight is at 0

            if (adjustedTime < 0.25f) // Night to dawn
            {
                return Mathf.Lerp(0.1f, 0.8f, adjustedTime * 4f);
            }
            else if (adjustedTime < 0.75f) // Day
            {
                return Mathf.Lerp(0.8f, 1.0f, (adjustedTime - 0.25f) * 2f);
            }
            else // Dusk to night
            {
                return Mathf.Lerp(1.0f, 0.1f, (adjustedTime - 0.75f) * 4f);
            }
        }

        private Color CalculateTimeBasedColor(float timeOfDay)
        {
            // Warm colors during sunrise/sunset, cool during midday, blue during night
            if (timeOfDay < 0.2f) // Early morning
            {
                return Color.Lerp(new Color(0.4f, 0.5f, 0.8f), new Color(1f, 0.8f, 0.6f), timeOfDay * 5f);
            }
            else if (timeOfDay < 0.8f) // Day
            {
                return Color.white;
            }
            else // Evening
            {
                return Color.Lerp(Color.white, new Color(1f, 0.6f, 0.4f), (timeOfDay - 0.8f) * 5f);
            }
        }
        #endregion

        #region Weather Integration
        public void ApplyWeatherLighting(WeatherType weatherType, float intensity)
        {
            if (!directionalLight || currentLightingConfig == null) return;

            // Modify lighting based on weather conditions
            switch (weatherType)
            {
                case WeatherType.Cloudy:
                    ApplyCloudyLighting(intensity);
                    break;
                case WeatherType.Rainy:
                    ApplyRainyLighting(intensity);
                    break;
                case WeatherType.Stormy:
                    ApplyStormyLighting(intensity);
                    break;
                case WeatherType.Snowy:
                    ApplySnowyLighting(intensity);
                    break;
                case WeatherType.Foggy:
                    ApplyFoggyLighting(intensity);
                    break;
                case WeatherType.Clear:
                default:
                    RestoreSeasonalLighting();
                    break;
            }
        }

        private void ApplyCloudyLighting(float intensity)
        {
            float dimming = 0.8f * intensity;
            directionalLight.intensity = currentLightingConfig.directionalLightIntensity * dimming;

            Color cloudyTint = new Color(0.9f, 0.9f, 1f);
            directionalLight.color = Color.Lerp(currentLightingConfig.directionalLightColor, cloudyTint, intensity * 0.5f);
        }

        private void ApplyRainyLighting(float intensity)
        {
            float dimming = 0.6f * intensity;
            directionalLight.intensity = currentLightingConfig.directionalLightIntensity * dimming;

            Color rainyTint = new Color(0.7f, 0.8f, 0.9f);
            directionalLight.color = Color.Lerp(currentLightingConfig.directionalLightColor, rainyTint, intensity * 0.7f);
        }

        private void ApplyStormyLighting(float intensity)
        {
            float dimming = 0.4f * intensity;
            directionalLight.intensity = currentLightingConfig.directionalLightIntensity * dimming;

            Color stormyTint = new Color(0.5f, 0.6f, 0.7f);
            directionalLight.color = Color.Lerp(currentLightingConfig.directionalLightColor, stormyTint, intensity);

            // Add random lightning flashes
            if (UnityEngine.Random.value < 0.01f) // 1% chance per frame
            {
                StartCoroutine(LightningFlash());
            }
        }

        private void ApplySnowyLighting(float intensity)
        {
            float brightening = 1.2f * intensity;
            directionalLight.intensity = currentLightingConfig.directionalLightIntensity * brightening;

            Color snowyTint = new Color(0.9f, 0.95f, 1f);
            directionalLight.color = Color.Lerp(currentLightingConfig.directionalLightColor, snowyTint, intensity * 0.3f);
        }

        private void ApplyFoggyLighting(float intensity)
        {
            float dimming = 0.7f * intensity;
            directionalLight.intensity = currentLightingConfig.directionalLightIntensity * dimming;

            // Increase ambient lighting to simulate fog scattering
            Color foggyAmbient = Color.Lerp(RenderSettings.ambientLight, Color.gray, intensity * 0.5f);
            RenderSettings.ambientLight = foggyAmbient;
        }

        private void RestoreSeasonalLighting()
        {
            if (currentLightingConfig != null)
            {
                ApplyLightingImmediate(currentLightingConfig);
            }
        }

        private IEnumerator LightningFlash()
        {
            float originalIntensity = directionalLight.intensity;
            Color originalColor = directionalLight.color;

            // Flash bright white
            directionalLight.intensity = originalIntensity * 3f;
            directionalLight.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            // Return to normal
            directionalLight.intensity = originalIntensity;
            directionalLight.color = originalColor;
        }
        #endregion

        #region System Updates
        public void UpdateSystem()
        {
            // Update any dynamic lighting effects
            if (isTransitioning)
            {
                // Continue transition effects if needed
            }

            // Update time-based lighting if enabled
            // This could be connected to a day/night cycle system later
        }
        #endregion

        #region Public API
        public void ForceSeasonLighting(SeasonType season)
        {
            ApplySeasonalLighting(season);
        }

        public void SetCustomLighting(Color ambientColor, float ambientIntensity, Color directionalColor, float directionalIntensity)
        {
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;

            if (directionalLight)
            {
                directionalLight.color = directionalColor;
                directionalLight.intensity = directionalIntensity;
            }

            DynamicGI.UpdateEnvironment();
        }

        public void RestoreOriginalLighting()
        {
            RenderSettings.ambientLight = originalAmbientColor;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.skybox = originalSkybox;

            if (directionalLight)
            {
                directionalLight.color = originalDirectionalColor;
                directionalLight.intensity = originalDirectionalIntensity;
                directionalLight.shadowStrength = originalShadowStrength;
            }

            DynamicGI.UpdateEnvironment();
        }

        public LightingConfiguration GetCurrentLightingConfiguration()
        {
            return currentLightingConfig;
        }

        public bool IsLightingTransitioning()
        {
            return isTransitioning;
        }

        public float GetTransitionProgress()
        {
            return transitionProgress;
        }
        #endregion

        #region Utility Methods
        public void CaptureCurrentLightingAsConfiguration()
        {
            // Utility method to capture current lighting settings as a configuration
            // Useful for designers to quickly create lighting configs

            Debug.Log($"[SeasonalLightingSystem] Current Lighting Settings:");
            Debug.Log($"Ambient Color: {RenderSettings.ambientLight}");
            Debug.Log($"Ambient Intensity: {RenderSettings.ambientIntensity}");

            if (directionalLight)
            {
                Debug.Log($"Directional Color: {directionalLight.color}");
                Debug.Log($"Directional Intensity: {directionalLight.intensity}");
                Debug.Log($"Shadow Strength: {directionalLight.shadowStrength}");
            }

            Debug.Log($"Skybox: {RenderSettings.skybox?.name ?? "None"}");
        }
        #endregion

        #region Cleanup
        void OnDestroy()
        {
            // Restore original lighting when system is destroyed
            if (Application.isPlaying)
            {
                RestoreOriginalLighting();
            }
        }
        #endregion
    }
}