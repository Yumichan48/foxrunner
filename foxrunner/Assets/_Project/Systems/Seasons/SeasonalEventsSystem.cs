using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxRunner.Seasons
{
    /// <summary>
    /// Manages seasonal special events, temporary bonuses, and unique seasonal mechanics
    /// Handles event scheduling, probability, effects, and rewards
    /// </summary>
    public class SeasonalEventsSystem : MonoBehaviour
    {
        #region Configuration
        private SeasonConfiguration config;
        #endregion

        #region Event State
        [Header("=== EVENT STATE (read only) ===")]
        [SerializeField] private bool systemActive;
        [SerializeField] private SeasonalEvent activeEvent;
        [SerializeField] private float eventTimer;
        [SerializeField] private float eventCheckTimer;
        [SerializeField] private bool hasActiveEvent;
        #endregion

        #region Event Management
        private Dictionary<SeasonType, List<SeasonalEvent>> seasonalEventPool;
        private List<GameplayModifier> activeEventModifiers;
        private float eventCheckInterval = 30f; // Check for events every 30 seconds
        private float lastEventTime;
        private int eventsTriggeredThisSession;
        #endregion

        #region Event History
        private Queue<SeasonalEventRecord> recentEvents;
        private Dictionary<string, int> eventCounters;
        private float cooldownPeriod = 300f; // 5 minutes between same event types
        #endregion

        #region Events
        public Action<SeasonalEvent> OnEventTriggered;
        public Action<SeasonalEvent> OnEventEnded;
        public Action<GameplayModifier> OnModifierApplied;
        public Action<GameplayModifier> OnModifierRemoved;
        public Action<EventReward> OnRewardGranted;
        #endregion

        #region Properties
        public bool SystemActive => systemActive;
        public bool HasActiveEvent => hasActiveEvent;
        public SeasonalEvent ActiveEvent => activeEvent;
        public float EventProgress => hasActiveEvent ? eventTimer / activeEvent.duration : 0f;
        public int EventsTriggeredThisSession => eventsTriggeredThisSession;
        #endregion

        #region Initialization
        public void Initialize(SeasonConfiguration seasonConfig)
        {
            config = seasonConfig;

            InitializeEventSystem();
            LoadEventState();

            systemActive = config.enableSpecialEvents;

            Debug.Log("[SeasonalEventsSystem] Initialized");
        }

        private void InitializeEventSystem()
        {
            seasonalEventPool = new Dictionary<SeasonType, List<SeasonalEvent>>();
            activeEventModifiers = new List<GameplayModifier>();
            recentEvents = new Queue<SeasonalEventRecord>();
            eventCounters = new Dictionary<string, int>();

            BuildEventPool();

            eventCheckTimer = 0f;
            lastEventTime = Time.time;
            eventsTriggeredThisSession = 0;
        }

        private void BuildEventPool()
        {
            // Initialize event pools for each season
            foreach (SeasonType season in Enum.GetValues(typeof(SeasonType)))
            {
                seasonalEventPool[season] = new List<SeasonalEvent>();
            }

            // Populate event pools from configuration
            if (config.specialEvents != null)
            {
                foreach (var eventConfig in config.specialEvents)
                {
                    if (seasonalEventPool.ContainsKey(eventConfig.season))
                    {
                        seasonalEventPool[eventConfig.season].Add(eventConfig);

                        // Initialize counter
                        if (!eventCounters.ContainsKey(eventConfig.eventName))
                        {
                            eventCounters[eventConfig.eventName] = 0;
                        }
                    }
                }
            }

            Debug.Log($"[SeasonalEventsSystem] Built event pools: {GetTotalEventCount()} events across all seasons");
        }

        private int GetTotalEventCount()
        {
            int total = 0;
            foreach (var pool in seasonalEventPool.Values)
            {
                total += pool.Count;
            }
            return total;
        }

        private void LoadEventState()
        {
            hasActiveEvent = false;
            activeEvent = null;
            eventTimer = 0f;
        }
        #endregion

        #region Event Management
        public void CheckForEvents(SeasonType currentSeason)
        {
            if (!systemActive || hasActiveEvent) return;

            eventCheckTimer += Time.deltaTime;

            if (eventCheckTimer >= eventCheckInterval)
            {
                eventCheckTimer = 0f;
                TryTriggerSeasonalEvent(currentSeason);
            }
        }

        private void TryTriggerSeasonalEvent(SeasonType season)
        {
            if (!seasonalEventPool.ContainsKey(season)) return;

            var availableEvents = GetAvailableEvents(season);
            if (availableEvents.Count == 0) return;

            // Calculate total probability
            float totalProbability = 0f;
            foreach (var evt in availableEvents)
            {
                totalProbability += evt.probability;
            }

            // Adjust probability based on time since last event
            float timeSinceLastEvent = Time.time - lastEventTime;
            float probabilityMultiplier = Mathf.Clamp01(timeSinceLastEvent / 120f); // Increases over 2 minutes

            float adjustedProbability = totalProbability * probabilityMultiplier;

            // Roll for event
            if (UnityEngine.Random.value <= adjustedProbability)
            {
                var selectedEvent = SelectEventFromPool(availableEvents);
                if (selectedEvent != null)
                {
                    TriggerEvent(selectedEvent);
                }
            }
        }

        private List<SeasonalEvent> GetAvailableEvents(SeasonType season)
        {
            var available = new List<SeasonalEvent>();

            if (!seasonalEventPool.ContainsKey(season)) return available;

            foreach (var evt in seasonalEventPool[season])
            {
                // Check if event is not on cooldown
                if (IsEventAvailable(evt))
                {
                    available.Add(evt);
                }
            }

            return available;
        }

        private bool IsEventAvailable(SeasonalEvent evt)
        {
            // Check if event is on cooldown
            foreach (var record in recentEvents)
            {
                if (record.eventName == evt.eventName &&
                    Time.time - record.triggerTime < cooldownPeriod)
                {
                    return false;
                }
            }

            return true;
        }

        private SeasonalEvent SelectEventFromPool(List<SeasonalEvent> availableEvents)
        {
            if (availableEvents.Count == 0) return null;

            float totalProbability = 0f;
            foreach (var evt in availableEvents)
            {
                totalProbability += evt.probability;
            }

            float randomValue = UnityEngine.Random.Range(0f, totalProbability);
            float currentProbability = 0f;

            foreach (var evt in availableEvents)
            {
                currentProbability += evt.probability;
                if (randomValue <= currentProbability)
                {
                    return evt;
                }
            }

            return availableEvents[0]; // Fallback
        }

        public void TriggerEvent(SeasonalEvent seasonalEvent)
        {
            if (hasActiveEvent) return;

            activeEvent = seasonalEvent;
            hasActiveEvent = true;
            eventTimer = 0f;
            lastEventTime = Time.time;
            eventsTriggeredThisSession++;

            // Apply event effects
            ApplyEventEffects(seasonalEvent);

            // Record event
            RecordEvent(seasonalEvent);

            // Notify systems
            OnEventTriggered?.Invoke(seasonalEvent);

            Debug.Log($"[SeasonalEventsSystem] Event triggered: {seasonalEvent.eventName} (Duration: {seasonalEvent.duration}s)");
        }

        public void EndActiveEvent()
        {
            if (!hasActiveEvent) return;

            var endingEvent = activeEvent;

            // Remove event effects
            RemoveEventEffects(endingEvent);

            // Grant end rewards
            GrantEventRewards(endingEvent);

            // Clear active event
            hasActiveEvent = false;
            activeEvent = null;
            eventTimer = 0f;

            // Notify systems
            OnEventEnded?.Invoke(endingEvent);

            Debug.Log($"[SeasonalEventsSystem] Event ended: {endingEvent.eventName}");
        }

        private void ApplyEventEffects(SeasonalEvent seasonalEvent)
        {
            // Apply gameplay modifiers
            if (seasonalEvent.modifiers != null)
            {
                foreach (var modifier in seasonalEvent.modifiers)
                {
                    ApplyGameplayModifier(modifier);
                }
            }

            // Apply any special event-specific effects
            ApplySpecialEventEffects(seasonalEvent);
        }

        private void RemoveEventEffects(SeasonalEvent seasonalEvent)
        {
            // Remove gameplay modifiers
            if (seasonalEvent.modifiers != null)
            {
                foreach (var modifier in seasonalEvent.modifiers)
                {
                    RemoveGameplayModifier(modifier);
                }
            }

            // Remove special effects
            RemoveSpecialEventEffects(seasonalEvent);
        }

        private void ApplyGameplayModifier(GameplayModifier modifier)
        {
            activeEventModifiers.Add(modifier);
            OnModifierApplied?.Invoke(modifier);

            Debug.Log($"[SeasonalEventsSystem] Applied modifier: {modifier.modifierType} = {modifier.modifierValue}");
        }

        private void RemoveGameplayModifier(GameplayModifier modifier)
        {
            activeEventModifiers.Remove(modifier);
            OnModifierRemoved?.Invoke(modifier);

            Debug.Log($"[SeasonalEventsSystem] Removed modifier: {modifier.modifierType}");
        }

        private void ApplySpecialEventEffects(SeasonalEvent seasonalEvent)
        {
            // Handle special event-specific effects that don't fit into standard modifiers
            switch (seasonalEvent.eventName.ToLower())
            {
                case "aurora borealis":
                    ApplyAuroraEffects();
                    break;
                case "rainbow bridge":
                    ApplyRainbowEffects();
                    break;
                case "meteor shower":
                    ApplyMeteorShowerEffects();
                    break;
                case "dimensional rift":
                    ApplyDimensionalRiftEffects();
                    break;
            }
        }

        private void RemoveSpecialEventEffects(SeasonalEvent seasonalEvent)
        {
            // Remove special effects
            switch (seasonalEvent.eventName.ToLower())
            {
                case "aurora borealis":
                    RemoveAuroraEffects();
                    break;
                case "rainbow bridge":
                    RemoveRainbowEffects();
                    break;
                case "meteor shower":
                    RemoveMeteorShowerEffects();
                    break;
                case "dimensional rift":
                    RemoveDimensionalRiftEffects();
                    break;
            }
        }

        private void GrantEventRewards(SeasonalEvent seasonalEvent)
        {
            if (seasonalEvent.rewards == null) return;

            foreach (var reward in seasonalEvent.rewards)
            {
                GrantReward(reward);
            }
        }

        private void GrantReward(EventReward reward)
        {
            OnRewardGranted?.Invoke(reward);
            Debug.Log($"[SeasonalEventsSystem] Granted reward: {reward.rewardType} x{reward.rewardAmount}");
        }

        private void RecordEvent(SeasonalEvent seasonalEvent)
        {
            var record = new SeasonalEventRecord
            {
                eventName = seasonalEvent.eventName,
                season = seasonalEvent.season,
                triggerTime = Time.time,
                duration = seasonalEvent.duration
            };

            recentEvents.Enqueue(record);

            // Update counter
            if (eventCounters.ContainsKey(seasonalEvent.eventName))
            {
                eventCounters[seasonalEvent.eventName]++;
            }

            // Maintain queue size
            while (recentEvents.Count > 20) // Keep last 20 events
            {
                recentEvents.Dequeue();
            }
        }
        #endregion

        #region Special Event Effects
        private void ApplyAuroraEffects()
        {
            // Aurora Borealis: Enhanced lighting and increased spirit point gain
            var lightingSystem = FindObjectOfType<SeasonalLightingSystem>();
            if (lightingSystem)
            {
                lightingSystem.SetCustomLighting(
                    new Color(0.6f, 0.8f, 1f, 1f), // Cool blue ambient
                    1.2f, // Increased intensity
                    new Color(0.8f, 0.9f, 1f, 1f), // Cool directional
                    1.5f // Enhanced directional intensity
                );
            }
        }

        private void RemoveAuroraEffects()
        {
            var lightingSystem = FindObjectOfType<SeasonalLightingSystem>();
            if (lightingSystem)
            {
                // Restore seasonal lighting
                var seasonalSystem = FindObjectOfType<SeasonalSystem>();
                if (seasonalSystem)
                {
                    lightingSystem.ApplySeasonalLighting(seasonalSystem.CurrentSeason);
                }
            }
        }

        private void ApplyRainbowEffects()
        {
            // Rainbow Bridge: Increased collectible variety and value
            Debug.Log("[SeasonalEventsSystem] Rainbow Bridge effects applied - increased collectible rewards");
        }

        private void RemoveRainbowEffects()
        {
            Debug.Log("[SeasonalEventsSystem] Rainbow Bridge effects removed");
        }

        private void ApplyMeteorShowerEffects()
        {
            // Meteor Shower: Temporary collectible shower
            StartCoroutine(MeteorShowerEffect());
        }

        private void RemoveMeteorShowerEffects()
        {
            // Effects are temporary and will end naturally
            Debug.Log("[SeasonalEventsSystem] Meteor Shower effects completed");
        }

        private void ApplyDimensionalRiftEffects()
        {
            // Dimensional Rift: Unstable energy but bonus dimensional energy gain
            var energySystem = FindObjectOfType<DimensionalEnergySystem>();
            if (energySystem)
            {
                energySystem.ModifyStability(-0.3f, activeEvent.duration, "Dimensional Rift");
                energySystem.SetEnergyEfficiency(2f);
            }
        }

        private void RemoveDimensionalRiftEffects()
        {
            var energySystem = FindObjectOfType<DimensionalEnergySystem>();
            if (energySystem)
            {
                energySystem.SetEnergyEfficiency(1f);
            }
        }

        private IEnumerator MeteorShowerEffect()
        {
            float duration = activeEvent?.duration ?? 60f;
            float elapsedTime = 0f;
            float meteorInterval = 2f; // Meteor every 2 seconds

            while (elapsedTime < duration && hasActiveEvent)
            {
                // Trigger meteor (enhanced collectible spawn)
                TriggerMeteor();

                yield return new WaitForSeconds(meteorInterval);
                elapsedTime += meteorInterval;
            }
        }

        private void TriggerMeteor()
        {
            // This would spawn special collectibles or trigger collection effects
            Debug.Log("[SeasonalEventsSystem] Meteor impact - bonus collectibles spawned");
        }
        #endregion

        #region System Updates
        public void UpdateSystem()
        {
            if (!systemActive) return;

            // Update active event
            if (hasActiveEvent)
            {
                UpdateActiveEvent();
            }

            // Clean up old event records
            CleanupEventHistory();
        }

        private void UpdateActiveEvent()
        {
            eventTimer += Time.deltaTime;

            // Check if event should end
            if (eventTimer >= activeEvent.duration)
            {
                EndActiveEvent();
            }
            else
            {
                // Update continuous event effects
                UpdateContinuousEventEffects();
            }
        }

        private void UpdateContinuousEventEffects()
        {
            // Handle events that have continuous effects during their duration
            if (activeEvent?.eventName.ToLower() == "aurora borealis")
            {
                // Gradually shift aurora colors
                UpdateAuroraLighting();
            }
        }

        private void UpdateAuroraLighting()
        {
            // Create shifting aurora colors
            float time = eventTimer / activeEvent.duration;
            float hueShift = Mathf.Sin(time * Mathf.PI * 4f) * 0.1f; // Shift hue over time

            Color auroraColor = Color.HSVToRGB(0.6f + hueShift, 0.7f, 1f);

            var lightingSystem = FindObjectOfType<SeasonalLightingSystem>();
            if (lightingSystem)
            {
                lightingSystem.SetCustomLighting(
                    auroraColor,
                    1.2f + Mathf.Sin(time * Mathf.PI * 2f) * 0.3f, // Pulsing intensity
                    Color.white,
                    1f
                );
            }
        }

        private void CleanupEventHistory()
        {
            // Remove old event records to prevent memory bloat
            while (recentEvents.Count > 0)
            {
                var oldestEvent = recentEvents.Peek();
                if (Time.time - oldestEvent.triggerTime > 3600f) // 1 hour
                {
                    recentEvents.Dequeue();
                }
                else
                {
                    break; // Queue is ordered, so if this one isn't old enough, neither are the rest
                }
            }
        }
        #endregion

        #region Public API
        public void ForceEvent(string eventName)
        {
            if (hasActiveEvent) return;

            // Find event by name
            foreach (var pool in seasonalEventPool.Values)
            {
                foreach (var evt in pool)
                {
                    if (evt.eventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                    {
                        TriggerEvent(evt);
                        return;
                    }
                }
            }

            Debug.LogWarning($"[SeasonalEventsSystem] Event not found: {eventName}");
        }

        public void SetSystemActive(bool active)
        {
            systemActive = active;

            if (!active && hasActiveEvent)
            {
                EndActiveEvent();
            }

            Debug.Log($"[SeasonalEventsSystem] System {(active ? "activated" : "deactivated")}");
        }

        public void SetCooldownPeriod(float seconds)
        {
            cooldownPeriod = Math.Max(0f, seconds);
            Debug.Log($"[SeasonalEventsSystem] Cooldown period set to {cooldownPeriod} seconds");
        }

        public void SetEventCheckInterval(float seconds)
        {
            eventCheckInterval = Math.Max(5f, seconds);
            Debug.Log($"[SeasonalEventsSystem] Event check interval set to {eventCheckInterval} seconds");
        }

        public List<GameplayModifier> GetActiveModifiers()
        {
            return new List<GameplayModifier>(activeEventModifiers);
        }

        public List<SeasonalEventRecord> GetRecentEvents()
        {
            return new List<SeasonalEventRecord>(recentEvents);
        }

        public int GetEventCount(string eventName)
        {
            return eventCounters.GetValueOrDefault(eventName, 0);
        }

        public float GetTimeToEventEnd()
        {
            if (!hasActiveEvent) return 0f;
            return activeEvent.duration - eventTimer;
        }

        public bool IsEventOnCooldown(string eventName)
        {
            foreach (var record in recentEvents)
            {
                if (record.eventName == eventName &&
                    Time.time - record.triggerTime < cooldownPeriod)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Save/Load Support
        public SeasonalEventsSaveData GetSaveData()
        {
            return new SeasonalEventsSaveData
            {
                systemActive = systemActive,
                eventsTriggeredThisSession = eventsTriggeredThisSession,
                eventCounters = new Dictionary<string, int>(eventCounters),
                lastEventTime = lastEventTime
            };
        }

        public void LoadFromSaveData(SeasonalEventsSaveData saveData)
        {
            if (saveData == null)
            {
                LoadDefaultData();
                return;
            }

            systemActive = saveData.systemActive;
            eventsTriggeredThisSession = saveData.eventsTriggeredThisSession;
            lastEventTime = saveData.lastEventTime;

            if (saveData.eventCounters != null)
            {
                eventCounters = new Dictionary<string, int>(saveData.eventCounters);
            }
        }

        private void LoadDefaultData()
        {
            systemActive = config.enableSpecialEvents;
            eventsTriggeredThisSession = 0;
            lastEventTime = Time.time;
            eventCounters.Clear();
        }
        #endregion
    }

    #region Supporting Data Structures
    [System.Serializable]
    public class SeasonalEventRecord
    {
        public string eventName;
        public SeasonType season;
        public float triggerTime;
        public float duration;
    }

    [System.Serializable]
    public class SeasonalEventsSaveData
    {
        public bool systemActive;
        public int eventsTriggeredThisSession;
        public Dictionary<string, int> eventCounters;
        public float lastEventTime;
    }
    #endregion
}