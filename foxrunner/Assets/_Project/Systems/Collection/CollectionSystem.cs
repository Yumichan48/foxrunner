using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FoxRunner.Data;
using FoxRunner.Currency;
using DG.Tweening;

namespace FoxRunner.Collection
{
    /// <summary>
    /// Comprehensive collection system for Fox Runner
    /// Handles fruit collection, combo mechanics, auto-collection, and collection effects
    /// Integrates with currency system and provides smooth collection feedback
    /// </summary>
    public class CollectionSystem : MonoBehaviour
    {
        #region Configuration
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private CollectionConfiguration config;

        [Header("=== REFERENCES ===")]
        [SerializeField] private Transform player;
        [SerializeField] private LayerMask collectibleLayers = -1;

        [Header("=== STATE INFO (READ ONLY) ===")]
        [SerializeField] private int currentCombo = 0;
        [SerializeField] private float comboMultiplier = 1f;
        [SerializeField] private float comboTimeRemaining = 0f;
        [SerializeField] private int sessionCollectibles = 0;
        [SerializeField] private bool autoCollectionActive = false;
        #endregion

        #region Private Fields
        private List<Collectible> nearbyCollectibles = new List<Collectible>();
        private Dictionary<CollectibleType, int> collectionStats = new Dictionary<CollectibleType, int>();
        private Dictionary<CollectibleType, long> collectionValues = new Dictionary<CollectibleType, long>();

        // Combo system
        private float lastCollectionTime;
        private int maxComboThisSession;
        private bool comboActive;

        // Auto-collection
        private Coroutine autoCollectionCoroutine;
        private float currentAutoCollectionRadius;

        // Collection queues for smooth effects
        private Queue<CollectionData> pendingCollections = new Queue<CollectionData>();
        private bool processingCollections = false;

        // References
        private CurrencySystem currencySystem;
        #endregion

        #region Events
        public static Action<Collectible, int, float> OnCollectibleCollected; // collectible, combo, multiplier
        public static Action<int, float> OnComboChanged; // combo count, multiplier
        public static Action OnComboBreak;
        public static Action<int> OnComboMilestone; // milestone reached
        public static Action<CollectibleType, long> OnCurrencyEarned; // type, amount
        public static Action<float> OnAutoCollectionRadiusChanged;
        #endregion

        #region Properties
        public int CurrentCombo => currentCombo;
        public float ComboMultiplier => comboMultiplier;
        public float ComboTimeRemaining => comboTimeRemaining;
        public bool ComboActive => comboActive;
        public float AutoCollectionRadius => currentAutoCollectionRadius;
        public Dictionary<CollectibleType, int> CollectionStats => new Dictionary<CollectibleType, int>(collectionStats);
        public int SessionCollectibles => sessionCollectibles;
        public int MaxComboThisSession => maxComboThisSession;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            ValidateConfiguration();
            InitializeStats();
        }

        void Start()
        {
            InitializeReferences();
            StartAutoCollection();
        }

        void Update()
        {
            UpdateComboTimer();
            UpdateAutoCollection();
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsCollectible(other))
            {
                CollectCollectible(other.GetComponent<Collectible>());
            }
        }
        #endregion

        #region Initialization
        private void ValidateConfiguration()
        {
            if (!config)
            {
                Debug.LogError("[CollectionSystem] Configuration missing! Creating default configuration.");
                config = ScriptableObject.CreateInstance<CollectionConfiguration>();
            }

            if (!player)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (!player)
                {
                    Debug.LogWarning("[CollectionSystem] Player reference not set and could not find Player tag!");
                }
            }
        }

        private void InitializeStats()
        {
            // Initialize collection stats for all collectible types
            foreach (CollectibleType type in Enum.GetValues(typeof(CollectibleType)))
            {
                collectionStats[type] = 0;
                collectionValues[type] = 0;
            }

            currentAutoCollectionRadius = config.baseAutoCollectionRadius;
        }

        private void InitializeReferences()
        {
            // Get currency system reference
            currencySystem = FindObjectOfType<CurrencySystem>();
            if (!currencySystem)
            {
                Debug.LogWarning("[CollectionSystem] CurrencySystem not found! Currency rewards will not work.");
            }
        }
        #endregion

        #region Collection Core
        public void CollectCollectible(Collectible collectible)
        {
            if (!collectible || collectible.IsCollected) return;

            // Mark as collected immediately to prevent double collection
            collectible.SetCollected(true);

            // Create collection data
            CollectionData collectionData = new CollectionData
            {
                collectible = collectible,
                collectionTime = Time.time,
                comboCount = currentCombo + 1,
                comboMultiplier = CalculateComboMultiplier(currentCombo + 1)
            };

            // Add to processing queue
            pendingCollections.Enqueue(collectionData);

            // Start processing if not already running
            if (!processingCollections)
            {
                StartCoroutine(ProcessCollectionQueue());
            }
        }

        private IEnumerator ProcessCollectionQueue()
        {
            processingCollections = true;

            while (pendingCollections.Count > 0)
            {
                CollectionData data = pendingCollections.Dequeue();
                yield return StartCoroutine(ProcessSingleCollection(data));

                // Small delay between collections for visual clarity
                yield return new WaitForSeconds(config.collectionProcessingDelay);
            }

            processingCollections = false;
        }

        private IEnumerator ProcessSingleCollection(CollectionData data)
        {
            Collectible collectible = data.collectible;

            // Update combo
            UpdateCombo();

            // Calculate rewards
            CollectionReward reward = CalculateReward(collectible, data.comboMultiplier);

            // Update statistics
            UpdateCollectionStats(collectible, reward);

            // Play collection effects
            yield return StartCoroutine(PlayCollectionEffects(collectible, data.comboCount, data.comboMultiplier));

            // Award currency
            AwardCurrency(reward);

            // Fire events
            OnCollectibleCollected?.Invoke(collectible, data.comboCount, data.comboMultiplier);

            // Cleanup collectible
            CleanupCollectible(collectible);
        }

        private bool IsCollectible(Collider other)
        {
            return ((1 << other.gameObject.layer) & collectibleLayers) != 0 &&
                   other.GetComponent<Collectible>() != null;
        }
        #endregion

        #region Combo System
        private void UpdateCombo()
        {
            currentCombo++;
            comboTimeRemaining = config.comboTimeWindow;
            comboActive = true;
            lastCollectionTime = Time.time;

            // Update max combo for session
            if (currentCombo > maxComboThisSession)
            {
                maxComboThisSession = currentCombo;
            }

            // Calculate new multiplier
            float oldMultiplier = comboMultiplier;
            comboMultiplier = CalculateComboMultiplier(currentCombo);

            // Check for combo milestones
            CheckComboMilestones();

            OnComboChanged?.Invoke(currentCombo, comboMultiplier);

            Debug.Log($"[CollectionSystem] Combo: {currentCombo} (x{comboMultiplier:F2})");
        }

        private void UpdateComboTimer()
        {
            if (!comboActive) return;

            comboTimeRemaining -= Time.deltaTime;

            if (comboTimeRemaining <= 0)
            {
                BreakCombo();
            }
        }

        private void BreakCombo()
        {
            if (currentCombo > 0)
            {
                Debug.Log($"[CollectionSystem] Combo broken at {currentCombo}!");
                OnComboBreak?.Invoke();
            }

            currentCombo = 0;
            comboMultiplier = 1f;
            comboTimeRemaining = 0f;
            comboActive = false;
        }

        private float CalculateComboMultiplier(int combo)
        {
            if (combo <= 1) return 1f;

            // Progressive combo multiplier with diminishing returns
            float baseMultiplier = 1f + (combo - 1) * config.comboMultiplierPerStep;
            float maxMultiplier = config.maxComboMultiplier;

            // Apply diminishing returns curve
            float normalizedCombo = (float)combo / config.maxComboForFullMultiplier;
            float curve = config.comboMultiplierCurve.Evaluate(normalizedCombo);

            return Mathf.Clamp(baseMultiplier * curve, 1f, maxMultiplier);
        }

        private void CheckComboMilestones()
        {
            int[] milestones = config.comboMilestones;
            if (milestones == null) return;

            foreach (int milestone in milestones)
            {
                if (currentCombo == milestone)
                {
                    OnComboMilestone?.Invoke(milestone);
                    PlayMilestoneEffects(milestone);
                    break;
                }
            }
        }
        #endregion

        #region Auto Collection
        private void StartAutoCollection()
        {
            if (config.enableAutoCollection)
            {
                autoCollectionCoroutine = StartCoroutine(AutoCollectionLoop());
            }
        }

        private IEnumerator AutoCollectionLoop()
        {
            while (true)
            {
                if (autoCollectionActive && player)
                {
                    CollectNearbyCollectibles();
                }

                yield return new WaitForSeconds(config.autoCollectionInterval);
            }
        }

        private void UpdateAutoCollection()
        {
            // Auto collection is enabled by equipment or upgrades
            autoCollectionActive = currentAutoCollectionRadius > 0;
        }

        private void CollectNearbyCollectibles()
        {
            if (!player) return;

            // Find collectibles within auto-collection radius
            Collider[] nearbyColliders = Physics.OverlapSphere(
                player.position,
                currentAutoCollectionRadius,
                collectibleLayers
            );

            foreach (Collider collider in nearbyColliders)
            {
                Collectible collectible = collider.GetComponent<Collectible>();
                if (collectible && !collectible.IsCollected)
                {
                    // Animate collectible toward player before collecting
                    StartCoroutine(AnimateToPlayer(collectible));
                }
            }
        }

        private IEnumerator AnimateToPlayer(Collectible collectible)
        {
            if (!player || !collectible) yield break;

            Vector3 startPos = collectible.transform.position;
            float duration = config.autoCollectionAnimationTime;
            float elapsed = 0f;

            while (elapsed < duration && collectible && player)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Use easing curve for smooth animation
                Vector3 currentPos = Vector3.Lerp(startPos, player.position, config.autoCollectionCurve.Evaluate(progress));
                collectible.transform.position = currentPos;

                yield return null;
            }

            // Collect the collectible
            if (collectible && !collectible.IsCollected)
            {
                CollectCollectible(collectible);
            }
        }

        public void SetAutoCollectionRadius(float radius)
        {
            float oldRadius = currentAutoCollectionRadius;
            currentAutoCollectionRadius = Mathf.Max(0f, radius);

            OnAutoCollectionRadiusChanged?.Invoke(currentAutoCollectionRadius);

            if (oldRadius != currentAutoCollectionRadius)
            {
                Debug.Log($"[CollectionSystem] Auto-collection radius changed: {oldRadius:F1} -> {currentAutoCollectionRadius:F1}");
            }
        }
        #endregion

        #region Reward Calculation
        private CollectionReward CalculateReward(Collectible collectible, float multiplier)
        {
            CollectionReward reward = new CollectionReward();

            // Get base values from configuration
            CollectibleTypeData typeData = config.GetCollectibleData(collectible.Type);
            if (typeData == null)
            {
                Debug.LogWarning($"[CollectionSystem] No configuration found for collectible type: {collectible.Type}");
                return reward;
            }

            // Calculate base reward
            reward.coins = (long)(typeData.baseCoinValue * multiplier);
            reward.experience = (long)(typeData.baseExperienceValue * multiplier);
            reward.spiritPoints = (long)(typeData.baseSpiritPointValue * multiplier);

            // Apply rarity multiplier
            float rarityMultiplier = GetRarityMultiplier(collectible.Rarity);
            reward.coins = (long)(reward.coins * rarityMultiplier);
            reward.experience = (long)(reward.experience * rarityMultiplier);
            reward.spiritPoints = (long)(reward.spiritPoints * rarityMultiplier);

            // Apply seasonal bonuses
            float seasonalMultiplier = GetSeasonalMultiplier(collectible.Type);
            reward.coins = (long)(reward.coins * seasonalMultiplier);
            reward.experience = (long)(reward.experience * seasonalMultiplier);

            // Special collectible bonuses
            if (collectible.IsSpecial)
            {
                reward.coins *= 2;
                reward.experience *= 2;
                reward.spiritPoints *= 2;
                reward.bonusCurrency = typeData.specialBonusCurrency;
                reward.bonusAmount = typeData.specialBonusAmount;
            }

            return reward;
        }

        private float GetRarityMultiplier(CollectibleRarity rarity)
        {
            return rarity switch
            {
                CollectibleRarity.Common => 1.0f,
                CollectibleRarity.Uncommon => 1.5f,
                CollectibleRarity.Rare => 2.0f,
                CollectibleRarity.Epic => 3.0f,
                CollectibleRarity.Legendary => 5.0f,
                _ => 1.0f
            };
        }

        private float GetSeasonalMultiplier(CollectibleType type)
        {
            // TODO: Integrate with seasonal system
            // For now, return base multiplier
            return 1.0f;
        }
        #endregion

        #region Effects & Feedback
        private IEnumerator PlayCollectionEffects(Collectible collectible, int combo, float multiplier)
        {
            if (!collectible) yield break;

            // Play particle effects
            if (config.collectionParticlePrefab)
            {
                GameObject particles = Instantiate(config.collectionParticlePrefab, collectible.transform.position, Quaternion.identity);
                Destroy(particles, 2f);
            }

            // Play sound effect
            PlayCollectionSound(collectible.Type, combo);

            // Show floating text
            if (config.showFloatingText)
            {
                ShowFloatingText(collectible, combo, multiplier);
            }

            // Screen shake for high combos
            if (combo >= config.screenShakeComboThreshold)
            {
                TriggerScreenShake(combo);
            }

            yield return new WaitForSeconds(0.1f);
        }

        private void PlayMilestoneEffects(int milestone)
        {
            Debug.Log($"[CollectionSystem] Combo milestone reached: {milestone}!");

            // Play special particle effect
            if (config.milestoneParticlePrefab)
            {
                GameObject particles = Instantiate(config.milestoneParticlePrefab, player.position, Quaternion.identity);
                Destroy(particles, 3f);
            }

            // Play milestone sound
            PlayMilestoneSound(milestone);

            // Screen flash or other dramatic effect
            TriggerMilestoneScreenEffect();
        }

        private void ShowFloatingText(Collectible collectible, int combo, float multiplier)
        {
            // TODO: Integrate with UI system for floating text
            Debug.Log($"[CollectionSystem] +{collectible.Type} x{multiplier:F1} (Combo: {combo})");
        }

        private void PlayCollectionSound(CollectibleType type, int combo)
        {
            // TODO: Integrate with audio system
            Debug.Log($"[CollectionSystem] Playing collection sound for {type} (combo: {combo})");
        }

        private void PlayMilestoneSound(int milestone)
        {
            // TODO: Integrate with audio system
            Debug.Log($"[CollectionSystem] Playing milestone sound for combo {milestone}");
        }

        private void TriggerScreenShake(int combo)
        {
            // TODO: Integrate with camera shake system
            float intensity = Mathf.Clamp01(combo / 50f) * 0.5f;
            Debug.Log($"[CollectionSystem] Screen shake intensity: {intensity:F2}");
        }

        private void TriggerMilestoneScreenEffect()
        {
            // TODO: Integrate with screen effects system
            Debug.Log("[CollectionSystem] Milestone screen effect triggered");
        }
        #endregion

        #region Statistics & Currency
        private void UpdateCollectionStats(Collectible collectible, CollectionReward reward)
        {
            collectionStats[collectible.Type]++;
            collectionValues[collectible.Type] += reward.coins;
            sessionCollectibles++;

            Debug.Log($"[CollectionSystem] Collected {collectible.Type} (Total: {collectionStats[collectible.Type]})");
        }

        private void AwardCurrency(CollectionReward reward)
        {
            if (currencySystem == null) return;

            // Award coins
            if (reward.coins > 0)
            {
                currencySystem.EarnCurrency(CurrencyType.Coins, reward.coins, "Collection");
                OnCurrencyEarned?.Invoke(CollectibleType.Coin, reward.coins);
            }

            // Award spirit points
            if (reward.spiritPoints > 0)
            {
                currencySystem.EarnCurrency(CurrencyType.SpiritPoints, reward.spiritPoints, "Collection");
            }

            // Award bonus currency
            if (reward.bonusCurrency != CurrencyType.Coins && reward.bonusAmount > 0)
            {
                currencySystem.EarnCurrency(reward.bonusCurrency, reward.bonusAmount, "Special Collection");
            }

            // TODO: Award experience through experience system
            if (reward.experience > 0)
            {
                Debug.Log($"[CollectionSystem] Would award {reward.experience} experience");
            }
        }

        private void CleanupCollectible(Collectible collectible)
        {
            if (!collectible) return;

            // Animate destruction
            collectible.transform.DOScale(0f, 0.2f).OnComplete(() =>
            {
                if (collectible && collectible.gameObject)
                {
                    Destroy(collectible.gameObject);
                }
            });
        }
        #endregion

        #region Public API
        public void AddComboTime(float additionalTime)
        {
            if (comboActive)
            {
                comboTimeRemaining += additionalTime;
                comboTimeRemaining = Mathf.Min(comboTimeRemaining, config.maxComboTime);
            }
        }

        public void SetComboMultiplierBonus(float bonus, float duration)
        {
            StartCoroutine(ApplyTemporaryComboBonus(bonus, duration));
        }

        private IEnumerator ApplyTemporaryComboBonus(float bonus, float duration)
        {
            float originalMultiplier = comboMultiplier;
            comboMultiplier += bonus;

            yield return new WaitForSeconds(duration);

            comboMultiplier = originalMultiplier;
        }

        public CollectionStats GetCollectionStats()
        {
            return new CollectionStats
            {
                totalCollectibles = sessionCollectibles,
                maxCombo = maxComboThisSession,
                currentCombo = currentCombo,
                comboMultiplier = comboMultiplier,
                autoCollectionRadius = currentAutoCollectionRadius,
                collectionsByType = new Dictionary<CollectibleType, int>(collectionStats),
                valuesByType = new Dictionary<CollectibleType, long>(collectionValues)
            };
        }
        #endregion

        #region Gizmos
        void OnDrawGizmos()
        {
            if (!config || !config.showDebugGizmos) return;

            if (player && currentAutoCollectionRadius > 0)
            {
                Gizmos.color = autoCollectionActive ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(player.position, currentAutoCollectionRadius);
            }
        }
        #endregion
    }

    #region Supporting Data Structures
    [Serializable]
    public class CollectionData
    {
        public Collectible collectible;
        public float collectionTime;
        public int comboCount;
        public float comboMultiplier;
    }

    [Serializable]
    public class CollectionReward
    {
        public long coins;
        public long experience;
        public long spiritPoints;
        public CurrencyType bonusCurrency;
        public long bonusAmount;
    }

    [Serializable]
    public class CollectionStats
    {
        public int totalCollectibles;
        public int maxCombo;
        public int currentCombo;
        public float comboMultiplier;
        public float autoCollectionRadius;
        public Dictionary<CollectibleType, int> collectionsByType;
        public Dictionary<CollectibleType, long> valuesByType;
    }

    public enum CollectibleType
    {
        Coin,           // Basic currency
        SpringFruit,    // Spring season fruit
        SummerFruit,    // Summer season fruit
        AutumnFruit,    // Autumn season fruit
        WinterFruit,    // Winter season fruit
        SpiritOrb,      // Spirit points
        GemShard,       // Premium currency fragment
        Material,       // Crafting materials
        PowerUp,        // Temporary power-ups
        SpecialItem     // Unique/rare collectibles
    }

    public enum CollectibleRarity
    {
        Common,         // Standard collectibles
        Uncommon,       // 1.5x value
        Rare,           // 2x value
        Epic,           // 3x value
        Legendary       // 5x value
    }
    #endregion
}