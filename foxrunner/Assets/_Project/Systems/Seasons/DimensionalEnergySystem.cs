using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoxRunner.Seasons
{
    /// <summary>
    /// Manages dimensional energy mechanics including generation, consumption, and stability effects
    /// Handles energy fluctuations, dimensional events, and energy-based progression
    /// </summary>
    public class DimensionalEnergySystem : MonoBehaviour
    {
        #region Configuration
        private SeasonConfiguration config;
        #endregion

        #region Energy State
        [Header("=== ENERGY STATE (read only) ===")]
        [SerializeField] private long currentEnergy;
        [SerializeField] private long maxEnergy;
        [SerializeField] private float energyRegenRate;
        [SerializeField] private float stabilityLevel;
        [SerializeField] private bool isRegenerating;
        #endregion

        #region Energy Generation
        private Dictionary<string, float> energyGenerators;
        private float baseRegenRate = 10f; // Energy per second
        private float lastRegenTime;
        private float energyEfficiency = 1f;
        #endregion

        #region Stability System
        private float targetStability = 1f;
        private float stabilityDecayRate = 0.1f;
        private float stabilityRegenRate = 0.05f;
        private List<StabilityModifier> activeStabilityModifiers;
        #endregion

        #region Dimensional Events
        private float eventTimer;
        private float nextEventTime;
        private bool eventSystemActive;
        private DimensionalEventType? activeEvent;
        private float activeEventDuration;
        private float activeEventTimer;
        #endregion

        #region Events
        public Action<long> OnEnergyChanged;
        public Action<float> OnStabilityChanged;
        public Action<float> OnRegenRateChanged;
        public Action<DimensionalEventType> OnDimensionalEventStarted;
        public Action<DimensionalEventType> OnDimensionalEventEnded;
        public Action OnEnergyCapacityChanged;
        #endregion

        #region Properties
        public long CurrentEnergy => currentEnergy;
        public long MaxEnergy => maxEnergy;
        public float EnergyPercentage => maxEnergy > 0 ? (float)currentEnergy / maxEnergy : 0f;
        public float StabilityLevel => stabilityLevel;
        public float EnergyRegenRate => energyRegenRate;
        public bool IsRegenerating => isRegenerating;
        public DimensionalEventType? ActiveEvent => activeEvent;
        public bool HasActiveEvent => activeEvent.HasValue;
        #endregion

        #region Initialization
        public void Initialize(SeasonConfiguration seasonConfig)
        {
            config = seasonConfig;

            InitializeEnergySystem();
            InitializeStabilitySystem();
            InitializeEventSystem();

            Debug.Log("[DimensionalEnergySystem] Initialized");
        }

        private void InitializeEnergySystem()
        {
            energyGenerators = new Dictionary<string, float>();

            // Initialize base values
            maxEnergy = 10000; // Base max energy
            currentEnergy = maxEnergy / 2; // Start at half capacity
            energyRegenRate = baseRegenRate;
            isRegenerating = true;
            lastRegenTime = Time.time;

            // Add default energy generators
            AddEnergyGenerator("Base", baseRegenRate);
        }

        private void InitializeStabilitySystem()
        {
            activeStabilityModifiers = new List<StabilityModifier>();
            stabilityLevel = 1f;
            targetStability = 1f;
        }

        private void InitializeEventSystem()
        {
            eventSystemActive = true;
            eventTimer = 0f;
            nextEventTime = UnityEngine.Random.Range(30f, 120f); // First event in 30-120 seconds
            activeEvent = null;
        }
        #endregion

        #region Energy Management
        public bool SpendEnergy(long amount, string reason = "Unknown")
        {
            if (currentEnergy < amount) return false;

            currentEnergy -= amount;
            currentEnergy = Math.Max(0, currentEnergy);

            OnEnergyChanged?.Invoke(currentEnergy);

            Debug.Log($"[DimensionalEnergySystem] Spent {amount} energy for {reason}. Remaining: {currentEnergy}");
            return true;
        }

        public void GainEnergy(long amount, string source = "Unknown")
        {
            currentEnergy += amount;
            currentEnergy = Math.Min(maxEnergy, currentEnergy);

            OnEnergyChanged?.Invoke(currentEnergy);

            Debug.Log($"[DimensionalEnergySystem] Gained {amount} energy from {source}. Current: {currentEnergy}");
        }

        public void AddEnergyGenerator(string name, float energyPerSecond)
        {
            energyGenerators[name] = energyPerSecond;
            RecalculateRegenRate();

            Debug.Log($"[DimensionalEnergySystem] Added energy generator: {name} ({energyPerSecond}/s)");
        }

        public void RemoveEnergyGenerator(string name)
        {
            if (energyGenerators.Remove(name))
            {
                RecalculateRegenRate();
                Debug.Log($"[DimensionalEnergySystem] Removed energy generator: {name}");
            }
        }

        public void ModifyEnergyGenerator(string name, float newEnergyPerSecond)
        {
            if (energyGenerators.ContainsKey(name))
            {
                energyGenerators[name] = newEnergyPerSecond;
                RecalculateRegenRate();
                Debug.Log($"[DimensionalEnergySystem] Modified energy generator: {name} to {newEnergyPerSecond}/s");
            }
        }

        private void RecalculateRegenRate()
        {
            float totalRegen = 0f;
            foreach (var generator in energyGenerators.Values)
            {
                totalRegen += generator;
            }

            energyRegenRate = totalRegen * energyEfficiency * stabilityLevel;
            OnRegenRateChanged?.Invoke(energyRegenRate);
        }

        public void SetMaxEnergy(long newMaxEnergy)
        {
            maxEnergy = Math.Max(1000, newMaxEnergy); // Minimum of 1000
            currentEnergy = Math.Min(currentEnergy, maxEnergy);

            OnEnergyCapacityChanged?.Invoke();
            OnEnergyChanged?.Invoke(currentEnergy);

            Debug.Log($"[DimensionalEnergySystem] Max energy set to: {maxEnergy}");
        }

        public void SetEnergyEfficiency(float efficiency)
        {
            energyEfficiency = Mathf.Clamp01(efficiency);
            RecalculateRegenRate();

            Debug.Log($"[DimensionalEnergySystem] Energy efficiency set to: {energyEfficiency:P1}");
        }
        #endregion

        #region Stability System
        public void ModifyStability(float amount, float duration = -1f, string reason = "Unknown")
        {
            if (duration > 0f)
            {
                // Temporary modifier
                var modifier = new StabilityModifier
                {
                    amount = amount,
                    duration = duration,
                    remainingTime = duration,
                    reason = reason
                };
                activeStabilityModifiers.Add(modifier);
            }
            else
            {
                // Permanent modifier
                targetStability = Mathf.Clamp01(targetStability + amount);
            }

            RecalculateStability();
            Debug.Log($"[DimensionalEnergySystem] Stability modified by {amount:F2} for {reason}");
        }

        private void UpdateStabilitySystem()
        {
            // Update temporary modifiers
            for (int i = activeStabilityModifiers.Count - 1; i >= 0; i--)
            {
                var modifier = activeStabilityModifiers[i];
                modifier.remainingTime -= Time.deltaTime;

                if (modifier.remainingTime <= 0f)
                {
                    activeStabilityModifiers.RemoveAt(i);
                    RecalculateStability();
                }
            }

            // Gradually move toward target stability
            float stabilitySpeed = stabilityLevel < targetStability ? stabilityRegenRate : stabilityDecayRate;
            stabilityLevel = Mathf.MoveTowards(stabilityLevel, targetStability, stabilitySpeed * Time.deltaTime);

            // Check if stability changed significantly
            float previousStability = stabilityLevel;
            if (Mathf.Abs(stabilityLevel - previousStability) > 0.01f)
            {
                OnStabilityChanged?.Invoke(stabilityLevel);
                RecalculateRegenRate(); // Stability affects energy regen
            }
        }

        private void RecalculateStability()
        {
            float totalModifier = 0f;
            foreach (var modifier in activeStabilityModifiers)
            {
                totalModifier += modifier.amount;
            }

            float newTargetStability = Mathf.Clamp01(1f + totalModifier);
            if (Mathf.Abs(newTargetStability - targetStability) > 0.001f)
            {
                targetStability = newTargetStability;
            }
        }

        public void ForceStabilityLevel(float level)
        {
            stabilityLevel = Mathf.Clamp01(level);
            targetStability = stabilityLevel;
            OnStabilityChanged?.Invoke(stabilityLevel);
            RecalculateRegenRate();

            Debug.Log($"[DimensionalEnergySystem] Stability forced to: {stabilityLevel:P1}");
        }
        #endregion

        #region Dimensional Events
        private void UpdateEventSystem()
        {
            if (!eventSystemActive) return;

            eventTimer += Time.deltaTime;

            // Update active event
            if (activeEvent.HasValue)
            {
                UpdateActiveEvent();
            }
            else
            {
                // Check for new event
                if (eventTimer >= nextEventTime)
                {
                    TriggerRandomEvent();
                    ScheduleNextEvent();
                }
            }
        }

        private void UpdateActiveEvent()
        {
            activeEventTimer += Time.deltaTime;

            if (activeEventTimer >= activeEventDuration)
            {
                EndActiveEvent();
            }
            else
            {
                // Update event effects
                UpdateEventEffects();
            }
        }

        private void TriggerRandomEvent()
        {
            var eventTypes = Enum.GetValues(typeof(DimensionalEventType));
            var randomEvent = (DimensionalEventType)eventTypes.GetValue(UnityEngine.Random.Range(0, eventTypes.Length));

            TriggerEvent(randomEvent);
        }

        public void TriggerEvent(DimensionalEventType eventType)
        {
            if (activeEvent.HasValue) return; // Can't trigger if one is already active

            activeEvent = eventType;
            activeEventTimer = 0f;
            activeEventDuration = GetEventDuration(eventType);

            ApplyEventEffects(eventType);
            OnDimensionalEventStarted?.Invoke(eventType);

            Debug.Log($"[DimensionalEnergySystem] Dimensional event started: {eventType} (Duration: {activeEventDuration:F1}s)");
        }

        private void EndActiveEvent()
        {
            if (!activeEvent.HasValue) return;

            var endingEvent = activeEvent.Value;
            RemoveEventEffects(endingEvent);

            OnDimensionalEventEnded?.Invoke(endingEvent);
            activeEvent = null;
            activeEventTimer = 0f;

            Debug.Log($"[DimensionalEnergySystem] Dimensional event ended: {endingEvent}");
        }

        private float GetEventDuration(DimensionalEventType eventType)
        {
            return eventType switch
            {
                DimensionalEventType.StabilityFluctuation => UnityEngine.Random.Range(15f, 45f),
                DimensionalEventType.EnergyBoost => UnityEngine.Random.Range(10f, 30f),
                DimensionalEventType.CollectibleStorm => UnityEngine.Random.Range(20f, 60f),
                DimensionalEventType.TimeDistortion => UnityEngine.Random.Range(30f, 90f),
                DimensionalEventType.GravityShift => UnityEngine.Random.Range(25f, 75f),
                DimensionalEventType.ElementalSurge => UnityEngine.Random.Range(40f, 120f),
                _ => 30f
            };
        }

        private void ApplyEventEffects(DimensionalEventType eventType)
        {
            switch (eventType)
            {
                case DimensionalEventType.StabilityFluctuation:
                    ModifyStability(-0.3f, activeEventDuration, "Stability Fluctuation Event");
                    break;

                case DimensionalEventType.EnergyBoost:
                    SetEnergyEfficiency(energyEfficiency * 2f);
                    break;

                case DimensionalEventType.CollectibleStorm:
                    // This would trigger increased collectible spawning
                    // Handled by other systems listening to the event
                    break;

                case DimensionalEventType.TimeDistortion:
                    ModifyStability(-0.1f, activeEventDuration, "Time Distortion Event");
                    SetEnergyEfficiency(energyEfficiency * 1.5f);
                    break;

                case DimensionalEventType.GravityShift:
                    ModifyStability(-0.2f, activeEventDuration, "Gravity Shift Event");
                    break;

                case DimensionalEventType.ElementalSurge:
                    SetEnergyEfficiency(energyEfficiency * 3f);
                    ModifyStability(0.2f, activeEventDuration, "Elemental Surge Event");
                    break;
            }
        }

        private void RemoveEventEffects(DimensionalEventType eventType)
        {
            switch (eventType)
            {
                case DimensionalEventType.EnergyBoost:
                    SetEnergyEfficiency(energyEfficiency / 2f);
                    break;

                case DimensionalEventType.TimeDistortion:
                    SetEnergyEfficiency(energyEfficiency / 1.5f);
                    break;

                case DimensionalEventType.ElementalSurge:
                    SetEnergyEfficiency(energyEfficiency / 3f);
                    break;
            }
        }

        private void UpdateEventEffects()
        {
            if (!activeEvent.HasValue) return;

            // Apply continuous effects for certain events
            switch (activeEvent.Value)
            {
                case DimensionalEventType.StabilityFluctuation:
                    // Random stability fluctuations
                    if (UnityEngine.Random.value < 0.1f) // 10% chance per second
                    {
                        float fluctuation = UnityEngine.Random.Range(-0.1f, 0.1f);
                        ModifyStability(fluctuation, 1f, "Fluctuation");
                    }
                    break;

                case DimensionalEventType.EnergyBoost:
                    // Boost energy regeneration
                    if (UnityEngine.Random.value < 0.5f) // 50% chance per second
                    {
                        GainEnergy(10, "Energy Boost Event");
                    }
                    break;
            }
        }

        private void ScheduleNextEvent()
        {
            eventTimer = 0f;
            nextEventTime = UnityEngine.Random.Range(60f, 300f); // Next event in 1-5 minutes

            Debug.Log($"[DimensionalEnergySystem] Next dimensional event scheduled in {nextEventTime:F1} seconds");
        }

        public void SetEventSystemActive(bool active)
        {
            eventSystemActive = active;
            if (!active && activeEvent.HasValue)
            {
                EndActiveEvent();
            }

            Debug.Log($"[DimensionalEnergySystem] Event system {(active ? "activated" : "deactivated")}");
        }
        #endregion

        #region System Updates
        public void UpdateSystem()
        {
            UpdateEnergyRegeneration();
            UpdateStabilitySystem();
            UpdateEventSystem();
        }

        private void UpdateEnergyRegeneration()
        {
            if (!isRegenerating || energyRegenRate <= 0f || currentEnergy >= maxEnergy) return;

            float deltaTime = Time.time - lastRegenTime;
            if (deltaTime >= 1f) // Update every second
            {
                long energyToAdd = (long)(energyRegenRate * deltaTime);
                if (energyToAdd > 0)
                {
                    GainEnergy(energyToAdd, "Regeneration");
                }

                lastRegenTime = Time.time;
            }
        }
        #endregion

        #region Public API
        public bool CanAffordEnergy(long amount)
        {
            return currentEnergy >= amount;
        }

        public float GetTimeToFullEnergy()
        {
            if (currentEnergy >= maxEnergy || energyRegenRate <= 0f) return 0f;

            long missingEnergy = maxEnergy - currentEnergy;
            return missingEnergy / energyRegenRate;
        }

        public void SetRegenerationActive(bool active)
        {
            isRegenerating = active;
            if (active)
            {
                lastRegenTime = Time.time;
            }

            Debug.Log($"[DimensionalEnergySystem] Energy regeneration {(active ? "enabled" : "disabled")}");
        }

        public Dictionary<string, float> GetEnergyGenerators()
        {
            return new Dictionary<string, float>(energyGenerators);
        }

        public List<StabilityModifier> GetActiveStabilityModifiers()
        {
            return new List<StabilityModifier>(activeStabilityModifiers);
        }

        public void EmergencyStabilize()
        {
            // Emergency function to stabilize dimensional energy
            ForceStabilityLevel(1f);
            SetEnergyEfficiency(1f);
            activeStabilityModifiers.Clear();

            if (activeEvent.HasValue)
            {
                EndActiveEvent();
            }

            Debug.Log("[DimensionalEnergySystem] Emergency stabilization activated");
        }
        #endregion

        #region Save/Load Support
        public DimensionalEnergySaveData GetSaveData()
        {
            return new DimensionalEnergySaveData
            {
                currentEnergy = currentEnergy,
                maxEnergy = maxEnergy,
                stabilityLevel = stabilityLevel,
                energyEfficiency = energyEfficiency,
                energyGenerators = new Dictionary<string, float>(energyGenerators),
                eventSystemActive = eventSystemActive,
                nextEventTime = nextEventTime - eventTimer
            };
        }

        public void LoadFromSaveData(DimensionalEnergySaveData saveData)
        {
            if (saveData == null)
            {
                LoadDefaultData();
                return;
            }

            currentEnergy = saveData.currentEnergy;
            maxEnergy = saveData.maxEnergy;
            stabilityLevel = saveData.stabilityLevel;
            energyEfficiency = saveData.energyEfficiency;
            eventSystemActive = saveData.eventSystemActive;

            if (saveData.energyGenerators != null)
            {
                energyGenerators = new Dictionary<string, float>(saveData.energyGenerators);
            }

            if (saveData.nextEventTime > 0f)
            {
                nextEventTime = saveData.nextEventTime;
                eventTimer = 0f;
            }

            RecalculateRegenRate();
        }

        private void LoadDefaultData()
        {
            // Reset to default values
            InitializeEnergySystem();
            InitializeStabilitySystem();
            InitializeEventSystem();
        }
        #endregion
    }

    #region Supporting Data Structures
    [System.Serializable]
    public class StabilityModifier
    {
        public float amount;
        public float duration;
        public float remainingTime;
        public string reason;
    }

    [System.Serializable]
    public class DimensionalEnergySaveData
    {
        public long currentEnergy;
        public long maxEnergy;
        public float stabilityLevel;
        public float energyEfficiency;
        public Dictionary<string, float> energyGenerators;
        public bool eventSystemActive;
        public float nextEventTime;
    }
    #endregion
}