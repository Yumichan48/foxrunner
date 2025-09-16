using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Crafting
{
    /// <summary>
    /// Core crafting system managing all crafting operations
    /// Handles recipe crafting, queue management, mastery progression
    /// Integrates with currency, equipment, and progression systems
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        #region Configuration
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private CraftingConfiguration config;

        [Header("=== DEBUG INFO (READ ONLY) ===")]
        [SerializeField] private bool isInitialized;
        [SerializeField] private int totalRecipesKnown;
        [SerializeField] private int totalItemsCrafted;
        [SerializeField] private Dictionary<CraftingStationType, int> stationLevels = new Dictionary<CraftingStationType, int>();
        #endregion

        #region Private Fields
        private Dictionary<CraftingStationType, CraftingStation> stations = new Dictionary<CraftingStationType, CraftingStation>();
        private Dictionary<CraftingStationType, int> masteryLevels = new Dictionary<CraftingStationType, int>();
        private Dictionary<CraftingStationType, long> masteryExperience = new Dictionary<CraftingStationType, long>();
        private Dictionary<string, bool> knownRecipes = new Dictionary<string, bool>();
        private Dictionary<string, int> materialInventory = new Dictionary<string, int>();
        private List<CraftingQueueItem> activeQueue = new List<CraftingQueueItem>();

        // Performance optimization
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f;
        #endregion

        #region Events
        public static Action<string, int, CraftingQuality> OnItemCrafted;
        public static Action<CraftingStationType, int, int> OnMasteryLevelUp; // station, old level, new level
        public static Action<string> OnRecipeUnlocked;
        public static Action<CraftingStationType, int> OnStationUpgraded;
        public static Action<CraftingQueueItem> OnQueueItemCompleted;
        public static Action<CraftingQueueItem> OnQueueItemStarted;
        public static Action<string, int, int> OnMaterialChanged; // material ID, old amount, new amount
        #endregion

        #region Properties
        public bool IsInitialized => isInitialized;
        public CraftingConfiguration Configuration => config;
        public Dictionary<CraftingStationType, int> StationLevels => new Dictionary<CraftingStationType, int>(stationLevels);
        public Dictionary<CraftingStationType, int> MasteryLevels => new Dictionary<CraftingStationType, int>(masteryLevels);
        public Dictionary<string, bool> KnownRecipes => new Dictionary<string, bool>(knownRecipes);
        public Dictionary<string, int> MaterialInventory => new Dictionary<string, int>(materialInventory);
        public List<CraftingQueueItem> ActiveQueue => new List<CraftingQueueItem>(activeQueue);
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            ValidateConfiguration();
        }

        void Update()
        {
            if (!isInitialized) return;

            // Update at intervals for performance
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
            lastUpdateTime = Time.time;

            UpdateCraftingQueue();
            UpdateDebugInfo();
        }
        #endregion

        #region Initialization
        public void Initialize()
        {
            try
            {
                Debug.Log("[CraftingSystem] Initializing crafting system...");

                InitializeStations();
                InitializeMastery();
                InitializeRecipes();
                InitializeMaterials();

                isInitialized = true;
                Debug.Log("[CraftingSystem] Crafting system initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CraftingSystem] Failed to initialize: {e.Message}");
                isInitialized = false;
            }
        }

        private void ValidateConfiguration()
        {
            if (!config)
            {
                Debug.LogError("[CraftingSystem] Configuration missing!");
                return;
            }

            if (config.craftingStations == null || config.craftingStations.Length != 5)
            {
                Debug.LogWarning("[CraftingSystem] Should have exactly 5 crafting stations!");
            }
        }

        private void InitializeStations()
        {
            if (config.craftingStations == null) return;

            foreach (var stationData in config.craftingStations)
            {
                var station = new CraftingStation(stationData, this);
                stations[stationData.stationType] = station;
                stationLevels[stationData.stationType] = 1;
            }
        }

        private void InitializeMastery()
        {
            // Initialize mastery levels for all station types
            foreach (CraftingStationType stationType in Enum.GetValues(typeof(CraftingStationType)))
            {
                if (stationType == CraftingStationType.None) continue;

                masteryLevels[stationType] = 1;
                masteryExperience[stationType] = 0;
            }
        }

        private void InitializeRecipes()
        {
            if (config.recipes == null) return;

            // Mark all recipes as unknown initially
            foreach (var recipe in config.recipes)
            {
                knownRecipes[recipe.recipeId] = false;
            }

            // Unlock basic recipes that have no requirements
            UnlockBasicRecipes();
        }

        private void InitializeMaterials()
        {
            if (config.materials == null) return;

            // Initialize material inventory
            foreach (var material in config.materials)
            {
                materialInventory[material.materialId] = 0;
            }
        }

        private void UnlockBasicRecipes()
        {
            if (config.recipes == null || config.recipeUnlocks == null) return;

            foreach (var recipe in config.recipes)
            {
                // Check if recipe has no unlock requirements
                var unlock = config.recipeUnlocks.FirstOrDefault(u => u.recipeId == recipe.recipeId);
                if (unlock == null || (unlock.requiredPlayerLevel <= 1 && unlock.requiredMasteryLevel <= 1))
                {
                    UnlockRecipe(recipe.recipeId);
                }
            }
        }
        #endregion

        #region Public API - Crafting Operations
        public bool CanCraftRecipe(string recipeId, int quantity = 1)
        {
            if (!isInitialized || !knownRecipes.ContainsKey(recipeId) || !knownRecipes[recipeId])
                return false;

            var recipe = config.GetRecipe(recipeId);
            if (recipe == null) return false;

            // Check station availability
            if (!stations.ContainsKey(recipe.requiredStation))
                return false;

            var station = stations[recipe.requiredStation];
            if (!station.IsUnlocked) return false;

            // Check mastery requirement
            if (masteryLevels[recipe.requiredStation] < recipe.requiredMasteryLevel)
                return false;

            // Check materials
            return HasRequiredMaterials(recipe, quantity);
        }

        public bool StartCrafting(string recipeId, int quantity = 1)
        {
            if (!CanCraftRecipe(recipeId, quantity))
                return false;

            var recipe = config.GetRecipe(recipeId);
            if (recipe == null) return false;

            // Check queue capacity
            if (activeQueue.Count >= config.maxQueueSize)
                return false;

            // Consume materials
            if (!ConsumeMaterials(recipe, quantity))
                return false;

            // Create queue item
            var queueItem = new CraftingQueueItem
            {
                recipeId = recipeId,
                quantity = quantity,
                stationType = recipe.requiredStation,
                startTime = Time.time,
                completionTime = Time.time + CalculateCraftingTime(recipe, quantity),
                isCompleted = false
            };

            activeQueue.Add(queueItem);
            OnQueueItemStarted?.Invoke(queueItem);

            Debug.Log($"[CraftingSystem] Started crafting {quantity}x {recipe.recipeName}");
            return true;
        }

        public bool CancelCrafting(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= activeQueue.Count)
                return false;

            var queueItem = activeQueue[queueIndex];
            if (queueItem.isCompleted) return false;

            // Refund materials (if desired - can be configurable)
            var recipe = config.GetRecipe(queueItem.recipeId);
            if (recipe != null)
            {
                RefundMaterials(recipe, queueItem.quantity);
            }

            activeQueue.RemoveAt(queueIndex);
            Debug.Log($"[CraftingSystem] Cancelled crafting: {queueItem.recipeId}");
            return true;
        }

        public void CompleteAllCrafting()
        {
            if (!config.enableDebugMode) return;

            foreach (var queueItem in activeQueue.ToList())
            {
                if (!queueItem.isCompleted)
                {
                    CompleteCraftingItem(queueItem);
                }
            }
        }
        #endregion

        #region Public API - Materials
        public void AddMaterial(string materialId, int amount)
        {
            if (amount <= 0) return;

            int oldAmount = GetMaterialAmount(materialId);
            int newAmount = oldAmount + amount;

            var material = config.GetMaterial(materialId);
            if (material != null)
            {
                newAmount = Mathf.Min(newAmount, material.maxStackSize);
            }

            materialInventory[materialId] = newAmount;
            OnMaterialChanged?.Invoke(materialId, oldAmount, newAmount);
        }

        public bool RemoveMaterial(string materialId, int amount)
        {
            if (amount <= 0) return false;

            int currentAmount = GetMaterialAmount(materialId);
            if (currentAmount < amount) return false;

            int newAmount = currentAmount - amount;
            materialInventory[materialId] = newAmount;
            OnMaterialChanged?.Invoke(materialId, currentAmount, newAmount);
            return true;
        }

        public int GetMaterialAmount(string materialId)
        {
            return materialInventory.GetValueOrDefault(materialId, 0);
        }

        public bool HasMaterial(string materialId, int amount = 1)
        {
            return GetMaterialAmount(materialId) >= amount;
        }
        #endregion

        #region Public API - Recipes
        public void UnlockRecipe(string recipeId)
        {
            if (knownRecipes.ContainsKey(recipeId) && !knownRecipes[recipeId])
            {
                knownRecipes[recipeId] = true;
                OnRecipeUnlocked?.Invoke(recipeId);
                Debug.Log($"[CraftingSystem] Recipe unlocked: {recipeId}");
            }
        }

        public bool IsRecipeKnown(string recipeId)
        {
            return knownRecipes.GetValueOrDefault(recipeId, false);
        }

        public List<CraftingRecipe> GetAvailableRecipes(CraftingStationType stationType)
        {
            if (config.recipes == null) return new List<CraftingRecipe>();

            return config.recipes.Where(r =>
                r.requiredStation == stationType &&
                IsRecipeKnown(r.recipeId) &&
                masteryLevels[stationType] >= r.requiredMasteryLevel
            ).ToList();
        }
        #endregion

        #region Public API - Mastery
        public void AddMasteryExperience(CraftingStationType stationType, long experience)
        {
            if (!masteryExperience.ContainsKey(stationType)) return;

            long oldExp = masteryExperience[stationType];
            masteryExperience[stationType] += experience;

            // Check for level up
            CheckMasteryLevelUp(stationType);
        }

        public int GetMasteryLevel(CraftingStationType stationType)
        {
            return masteryLevels.GetValueOrDefault(stationType, 1);
        }

        public long GetMasteryExperience(CraftingStationType stationType)
        {
            return masteryExperience.GetValueOrDefault(stationType, 0);
        }

        public long GetMasteryExperienceToNextLevel(CraftingStationType stationType)
        {
            int currentLevel = GetMasteryLevel(stationType);
            long currentExp = GetMasteryExperience(stationType);
            long requiredExp = config.GetMasteryRequirement(currentLevel + 1);

            return Math.Max(0, requiredExp - currentExp);
        }
        #endregion

        #region Private Methods - Crafting Logic
        private bool HasRequiredMaterials(CraftingRecipe recipe, int quantity)
        {
            if (recipe.ingredients == null) return true;

            foreach (var ingredient in recipe.ingredients)
            {
                int required = ingredient.amount * quantity;
                if (GetMaterialAmount(ingredient.materialId) < required)
                    return false;
            }

            return true;
        }

        private bool ConsumeMaterials(CraftingRecipe recipe, int quantity)
        {
            if (recipe.ingredients == null) return true;

            // First check if we have all materials
            if (!HasRequiredMaterials(recipe, quantity))
                return false;

            // Consume materials
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient.consumed)
                {
                    int toConsume = ingredient.amount * quantity;
                    RemoveMaterial(ingredient.materialId, toConsume);
                }
            }

            return true;
        }

        private void RefundMaterials(CraftingRecipe recipe, int quantity)
        {
            if (recipe.ingredients == null) return;

            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient.consumed)
                {
                    int toRefund = ingredient.amount * quantity;
                    AddMaterial(ingredient.materialId, toRefund);
                }
            }
        }

        private float CalculateCraftingTime(CraftingRecipe recipe, int quantity)
        {
            if (config.enableDebugMode && config.instantCraftingInDebug)
                return 0.1f;

            float baseTime = recipe.baseCraftingTime * config.baseCraftingTimeMultiplier;

            // Apply station speed bonus
            if (stations.ContainsKey(recipe.requiredStation))
            {
                var station = stations[recipe.requiredStation];
                baseTime *= station.GetSpeedMultiplier();
            }

            // Apply mastery bonus
            int masteryLevel = GetMasteryLevel(recipe.requiredStation);
            float masteryBonus = 1f - (masteryLevel * config.masterySpeedBonusPerLevel);
            baseTime *= Mathf.Max(0.1f, masteryBonus);

            // Batch crafting efficiency
            if (quantity > 1 && recipe.allowBatchCrafting)
            {
                float batchEfficiency = 1f - Mathf.Min(0.5f, (quantity - 1) * 0.05f);
                baseTime *= batchEfficiency;
            }

            return baseTime * quantity;
        }

        private void UpdateCraftingQueue()
        {
            for (int i = activeQueue.Count - 1; i >= 0; i--)
            {
                var queueItem = activeQueue[i];
                if (!queueItem.isCompleted && Time.time >= queueItem.completionTime)
                {
                    CompleteCraftingItem(queueItem);
                }
            }

            // Remove completed items
            activeQueue.RemoveAll(item => item.isCompleted);
        }

        private void CompleteCraftingItem(CraftingQueueItem queueItem)
        {
            var recipe = config.GetRecipe(queueItem.recipeId);
            if (recipe == null) return;

            // Produce results
            ProduceResults(recipe, queueItem.quantity);

            // Award mastery experience
            AddMasteryExperience(queueItem.stationType, recipe.experienceReward * queueItem.quantity);

            // Mark as completed
            queueItem.isCompleted = true;
            OnQueueItemCompleted?.Invoke(queueItem);

            totalItemsCrafted += queueItem.quantity;
            Debug.Log($"[CraftingSystem] Completed crafting {queueItem.quantity}x {recipe.recipeName}");
        }

        private void ProduceResults(CraftingRecipe recipe, int quantity)
        {
            if (recipe.results == null) return;

            foreach (var result in recipe.results)
            {
                // Calculate quantity with chance
                for (int i = 0; i < quantity; i++)
                {
                    if (UnityEngine.Random.value <= result.chance)
                    {
                        ProduceSingleResult(result, recipe.requiredStation);
                    }
                }
            }
        }

        private void ProduceSingleResult(CraftingResult result, CraftingStationType stationType)
        {
            CraftingQuality finalQuality = DetermineResultQuality(result.quality, stationType);

            switch (result.resultType)
            {
                case CraftingResultType.Material:
                    AddMaterial(result.materialId, result.amount);
                    break;

                case CraftingResultType.Equipment:
                    // Integration with equipment system would go here
                    OnItemCrafted?.Invoke(result.itemId, result.amount, finalQuality);
                    break;

                case CraftingResultType.Currency:
                    // Integration with currency system would go here
                    break;
            }
        }

        private CraftingQuality DetermineResultQuality(CraftingQuality baseQuality, CraftingStationType stationType)
        {
            // Base quality upgrade chance
            float upgradeChance = config.baseQualityUpgradeChance;

            // Add mastery bonus
            int masteryLevel = GetMasteryLevel(stationType);
            upgradeChance += masteryLevel * config.masteryQualityBonusPerLevel;

            // Try to upgrade quality
            if (UnityEngine.Random.value < upgradeChance && baseQuality < CraftingQuality.Mythic)
            {
                return baseQuality + 1;
            }

            return baseQuality;
        }

        private void CheckMasteryLevelUp(CraftingStationType stationType)
        {
            int currentLevel = masteryLevels[stationType];
            long currentExp = masteryExperience[stationType];
            long requiredExp = config.GetMasteryRequirement(currentLevel + 1);

            if (currentExp >= requiredExp && currentLevel < 100)
            {
                masteryLevels[stationType] = currentLevel + 1;
                OnMasteryLevelUp?.Invoke(stationType, currentLevel, currentLevel + 1);
                Debug.Log($"[CraftingSystem] Mastery level up! {stationType}: {currentLevel} -> {currentLevel + 1}");
            }
        }

        private void UpdateDebugInfo()
        {
            totalRecipesKnown = knownRecipes.Values.Count(known => known);

            // Update station levels display
            stationLevels.Clear();
            foreach (var kvp in stations)
            {
                stationLevels[kvp.Key] = kvp.Value.Level;
            }
        }
        #endregion

        #region Save/Load System Integration
        public CraftingSaveData GetSaveData()
        {
            return new CraftingSaveData
            {
                stationLevels = new Dictionary<CraftingStationType, int>(stationLevels),
                masteryLevels = new Dictionary<CraftingStationType, int>(masteryLevels),
                masteryExperience = new Dictionary<CraftingStationType, long>(masteryExperience),
                knownRecipes = new Dictionary<string, bool>(knownRecipes),
                materialInventory = new Dictionary<string, int>(materialInventory),
                totalItemsCrafted = totalItemsCrafted
            };
        }

        public void LoadFromSaveData(CraftingSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.Log("[CraftingSystem] No save data found, using defaults");
                return;
            }

            if (saveData.stationLevels != null)
                stationLevels = new Dictionary<CraftingStationType, int>(saveData.stationLevels);

            if (saveData.masteryLevels != null)
                masteryLevels = new Dictionary<CraftingStationType, int>(saveData.masteryLevels);

            if (saveData.masteryExperience != null)
                masteryExperience = new Dictionary<CraftingStationType, long>(saveData.masteryExperience);

            if (saveData.knownRecipes != null)
                knownRecipes = new Dictionary<string, bool>(saveData.knownRecipes);

            if (saveData.materialInventory != null)
                materialInventory = new Dictionary<string, int>(saveData.materialInventory);

            totalItemsCrafted = saveData.totalItemsCrafted;

            Debug.Log("[CraftingSystem] Save data loaded successfully");
        }
        #endregion
    }

    [System.Serializable]
    public class CraftingQueueItem
    {
        public string recipeId;
        public int quantity;
        public CraftingStationType stationType;
        public float startTime;
        public float completionTime;
        public bool isCompleted;

        public float Progress => isCompleted ? 1f : Mathf.Clamp01((Time.time - startTime) / (completionTime - startTime));
        public float TimeRemaining => isCompleted ? 0f : Mathf.Max(0f, completionTime - Time.time);
    }

    [System.Serializable]
    public class CraftingSaveData
    {
        public Dictionary<CraftingStationType, int> stationLevels;
        public Dictionary<CraftingStationType, int> masteryLevels;
        public Dictionary<CraftingStationType, long> masteryExperience;
        public Dictionary<string, bool> knownRecipes;
        public Dictionary<string, int> materialInventory;
        public int totalItemsCrafted;
    }
}