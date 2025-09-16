using System;
using System.Collections.Generic;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Crafting
{
    /// <summary>
    /// Configuration for the crafting system
    /// Defines all recipes, stations, materials, and crafting balance parameters
    /// Integrates with equipment, currency, and progression systems
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingConfig", menuName = "FoxRunner/Crafting/Crafting Configuration")]
    public class CraftingConfiguration : ScriptableObject
    {
        [Header("=== CRAFTING STATIONS ===")]
        [Tooltip("All available crafting stations")]
        public CraftingStationData[] craftingStations;

        [Header("=== RECIPES ===")]
        [Tooltip("All available crafting recipes")]
        public CraftingRecipe[] recipes;

        [Header("=== MATERIALS ===")]
        [Tooltip("All crafting materials and their properties")]
        public CraftingMaterial[] materials;

        [Header("=== MASTERY SYSTEM ===")]
        [Tooltip("Experience required for each mastery level")]
        public long[] masteryLevelRequirements = new long[100];

        [Tooltip("Crafting speed bonus per mastery level (percentage)")]
        [Range(0f, 5f)]
        public float masterySpeedBonusPerLevel = 0.02f;

        [Tooltip("Quality bonus chance per mastery level (percentage)")]
        [Range(0f, 1f)]
        public float masteryQualityBonusPerLevel = 0.01f;

        [Header("=== CRAFTING MECHANICS ===")]
        [Tooltip("Base crafting time multiplier")]
        [Range(0.1f, 10f)]
        public float baseCraftingTimeMultiplier = 1f;

        [Tooltip("Maximum queue size per station")]
        [Range(1, 50)]
        public int maxQueueSize = 10;

        [Tooltip("Enable batch crafting")]
        public bool enableBatchCrafting = true;

        [Tooltip("Maximum batch size")]
        [Range(1, 100)]
        public int maxBatchSize = 20;

        [Header("=== QUALITY SYSTEM ===")]
        [Tooltip("Quality tiers and their stat multipliers")]
        public QualityTier[] qualityTiers;

        [Tooltip("Base quality upgrade chance")]
        [Range(0f, 1f)]
        public float baseQualityUpgradeChance = 0.05f;

        [Header("=== PROGRESSION UNLOCKS ===")]
        [Tooltip("Station unlock requirements")]
        public StationUnlockRequirement[] stationUnlocks;

        [Tooltip("Recipe unlock requirements")]
        public RecipeUnlockRequirement[] recipeUnlocks;

        [Header("=== ECONOMICS ===")]
        [Tooltip("Material cost scaling factor")]
        [Range(0.1f, 5f)]
        public float materialCostScaling = 1.2f;

        [Tooltip("Experience reward per crafting action")]
        [Range(1, 1000)]
        public int baseExperienceReward = 10;

        [Header("=== DEBUG ===")]
        [Tooltip("Enable debug mode for crafting")]
        public bool enableDebugMode = false;

        [Tooltip("Instant crafting in debug mode")]
        public bool instantCraftingInDebug = false;

        void OnValidate()
        {
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            // Validate stations
            if (craftingStations == null || craftingStations.Length != 5)
            {
                Debug.LogWarning("[CraftingConfiguration] Should have exactly 5 crafting stations!");
            }

            // Validate mastery level requirements
            if (masteryLevelRequirements == null || masteryLevelRequirements.Length == 0)
            {
                Debug.LogWarning("[CraftingConfiguration] Mastery level requirements not configured!");
                InitializeDefaultMasteryRequirements();
            }

            // Validate quality tiers
            if (qualityTiers == null || qualityTiers.Length == 0)
            {
                Debug.LogWarning("[CraftingConfiguration] Quality tiers not configured!");
            }

            // Validate recipes
            if (recipes != null)
            {
                foreach (var recipe in recipes)
                {
                    if (recipe.ingredients == null || recipe.ingredients.Length == 0)
                    {
                        Debug.LogWarning($"[CraftingConfiguration] Recipe '{recipe.recipeName}' has no ingredients!");
                    }
                }
            }
        }

        private void InitializeDefaultMasteryRequirements()
        {
            masteryLevelRequirements = new long[100];
            for (int i = 0; i < masteryLevelRequirements.Length; i++)
            {
                masteryLevelRequirements[i] = (long)(100 * Math.Pow(1.5, i));
            }
        }

        public CraftingStationData GetStationData(CraftingStationType stationType)
        {
            if (craftingStations == null) return null;

            foreach (var station in craftingStations)
            {
                if (station.stationType == stationType)
                    return station;
            }

            return null;
        }

        public CraftingRecipe GetRecipe(string recipeId)
        {
            if (recipes == null) return null;

            foreach (var recipe in recipes)
            {
                if (recipe.recipeId == recipeId)
                    return recipe;
            }

            return null;
        }

        public CraftingMaterial GetMaterial(string materialId)
        {
            if (materials == null) return null;

            foreach (var material in materials)
            {
                if (material.materialId == materialId)
                    return material;
            }

            return null;
        }

        public long GetMasteryRequirement(int level)
        {
            if (masteryLevelRequirements == null || level <= 0 || level > masteryLevelRequirements.Length)
                return long.MaxValue;

            return masteryLevelRequirements[level - 1];
        }

        public QualityTier GetQualityTier(CraftingQuality quality)
        {
            if (qualityTiers == null) return null;

            foreach (var tier in qualityTiers)
            {
                if (tier.quality == quality)
                    return tier;
            }

            return null;
        }
    }

    [System.Serializable]
    public class CraftingStationData
    {
        [Tooltip("Type of crafting station")]
        public CraftingStationType stationType;

        [Tooltip("Display name of the station")]
        public string stationName;

        [Tooltip("Description of what this station crafts")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Specializations this station can craft")]
        public CraftingSpecialization[] specializations;

        [Tooltip("Base crafting speed multiplier")]
        [Range(0.1f, 5f)]
        public float baseCraftingSpeedMultiplier = 1f;

        [Tooltip("Maximum level for this station")]
        [Range(1, 100)]
        public int maxLevel = 50;

        [Tooltip("Station upgrade costs")]
        public StationUpgradeCost[] upgradeCosts;
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        [Tooltip("Unique identifier for this recipe")]
        public string recipeId;

        [Tooltip("Display name of the recipe")]
        public string recipeName;

        [Tooltip("Description of the crafted item")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Station required for this recipe")]
        public CraftingStationType requiredStation;

        [Tooltip("Specialization required")]
        public CraftingSpecialization requiredSpecialization;

        [Tooltip("Ingredients required")]
        public CraftingIngredient[] ingredients;

        [Tooltip("Results produced by this recipe")]
        public CraftingResult[] results;

        [Tooltip("Base crafting time in seconds")]
        [Range(1f, 3600f)]
        public float baseCraftingTime = 60f;

        [Tooltip("Minimum mastery level required")]
        [Range(1, 100)]
        public int requiredMasteryLevel = 1;

        [Tooltip("Experience reward for crafting")]
        [Range(1, 1000)]
        public int experienceReward = 10;

        [Tooltip("Can this recipe be batch crafted?")]
        public bool allowBatchCrafting = true;

        [Tooltip("Recipe category for organization")]
        public RecipeCategory category;
    }

    [System.Serializable]
    public class CraftingMaterial
    {
        [Tooltip("Unique identifier for this material")]
        public string materialId;

        [Tooltip("Display name of the material")]
        public string materialName;

        [Tooltip("Description of the material")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Material rarity")]
        public MaterialRarity rarity;

        [Tooltip("Sources where this material can be obtained")]
        public MaterialSource[] sources;

        [Tooltip("Stack size for inventory")]
        [Range(1, 10000)]
        public int maxStackSize = 999;

        [Tooltip("Can this material be traded?")]
        public bool tradeable = true;
    }

    [System.Serializable]
    public class CraftingIngredient
    {
        [Tooltip("Material ID required")]
        public string materialId;

        [Tooltip("Amount required")]
        [Range(1, 1000)]
        public int amount = 1;

        [Tooltip("Is this ingredient consumed on crafting?")]
        public bool consumed = true;

        [Tooltip("Quality requirement for this ingredient")]
        public CraftingQuality minimumQuality = CraftingQuality.Common;
    }

    [System.Serializable]
    public class CraftingResult
    {
        [Tooltip("Type of result")]
        public CraftingResultType resultType;

        [Tooltip("Item ID for equipment results")]
        public string itemId;

        [Tooltip("Material ID for material results")]
        public string materialId;

        [Tooltip("Currency type for currency results")]
        public CurrencyType currencyType;

        [Tooltip("Amount produced")]
        [Range(1, 1000)]
        public int amount = 1;

        [Tooltip("Quality of the result")]
        public CraftingQuality quality = CraftingQuality.Common;

        [Tooltip("Chance to produce this result (0-1)")]
        [Range(0f, 1f)]
        public float chance = 1f;
    }

    [System.Serializable]
    public class QualityTier
    {
        [Tooltip("Quality level")]
        public CraftingQuality quality;

        [Tooltip("Display name")]
        public string qualityName;

        [Tooltip("Stat multiplier for this quality")]
        [Range(0.1f, 10f)]
        public float statMultiplier = 1f;

        [Tooltip("Color for UI display")]
        public Color qualityColor = Color.white;
    }

    [System.Serializable]
    public class StationUnlockRequirement
    {
        [Tooltip("Station to unlock")]
        public CraftingStationType stationType;

        [Tooltip("Player level required")]
        [Range(1, 500)]
        public int requiredPlayerLevel = 1;

        [Tooltip("Currency costs")]
        public CurrencyCost[] currencyCosts;

        [Tooltip("Material requirements")]
        public MaterialRequirement[] materialRequirements;

        [Tooltip("Previous station required")]
        public CraftingStationType requiredPreviousStation = CraftingStationType.None;
    }

    [System.Serializable]
    public class RecipeUnlockRequirement
    {
        [Tooltip("Recipe to unlock")]
        public string recipeId;

        [Tooltip("Player level required")]
        [Range(1, 500)]
        public int requiredPlayerLevel = 1;

        [Tooltip("Station mastery level required")]
        [Range(1, 100)]
        public int requiredMasteryLevel = 1;

        [Tooltip("Previous recipes that must be learned")]
        public string[] prerequisiteRecipes;

        [Tooltip("Quest completion requirement")]
        public string requiredQuestId;
    }

    [System.Serializable]
    public class StationUpgradeCost
    {
        [Tooltip("Station level")]
        [Range(1, 100)]
        public int level;

        [Tooltip("Currency costs")]
        public CurrencyCost[] currencyCosts;

        [Tooltip("Material requirements")]
        public MaterialRequirement[] materialRequirements;

        [Tooltip("Time to upgrade (seconds)")]
        [Range(1f, 86400f)]
        public float upgradeTime = 3600f;
    }

    [System.Serializable]
    public class CurrencyCost
    {
        [Tooltip("Currency type")]
        public CurrencyType currencyType;

        [Tooltip("Amount required")]
        [Range(1, 1000000)]
        public long amount = 100;
    }

    [System.Serializable]
    public class MaterialRequirement
    {
        [Tooltip("Material ID")]
        public string materialId;

        [Tooltip("Amount required")]
        [Range(1, 1000)]
        public int amount = 1;

        [Tooltip("Quality requirement")]
        public CraftingQuality minimumQuality = CraftingQuality.Common;
    }

    [System.Serializable]
    public class MaterialSource
    {
        [Tooltip("Source type")]
        public MaterialSourceType sourceType;

        [Tooltip("Source description")]
        public string sourceDescription;

        [Tooltip("Drop rate or availability")]
        [Range(0f, 1f)]
        public float availability = 1f;
    }

    [System.Serializable]
    public class CraftingSpecialization
    {
        [Tooltip("Specialization name")]
        public string specializationName;

        [Tooltip("Description")]
        [TextArea(2, 3)]
        public string description;

        [Tooltip("Bonus effects")]
        public string[] bonusEffects;
    }

    public enum CraftingStationType
    {
        None,
        Forge,           // Weapons, armor, tools
        AlchemyLab,      // Potions, enchantments, transmutation
        EnchantingTable, // Magical enhancements, runes
        Workshop,        // Accessories, mechanical items
        SacredAltar      // Divine items, ultimate equipment
    }

    public enum CraftingQuality
    {
        Common,    // White - Basic quality
        Uncommon,  // Green - Slightly improved
        Rare,      // Blue - Good quality
        Epic,      // Purple - High quality
        Legendary, // Orange - Exceptional quality
        Mythic     // Red - Ultimate quality
    }

    public enum CraftingResultType
    {
        Equipment,  // Produces equipment item
        Material,   // Produces crafting material
        Currency,   // Produces currency
        Experience  // Produces experience points
    }

    public enum MaterialRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public enum MaterialSourceType
    {
        Collection,    // From collection system
        Drops,         // From enemies/bosses
        Mining,        // From resource nodes
        Seasonal,      // Season-specific sources
        Shop,          // Purchasable from shops
        Rewards,       // Quest/achievement rewards
        Crafting,      // Produced by other crafting
        Events         // Special events
    }

    public enum RecipeCategory
    {
        Weapons,
        Armor,
        Accessories,
        Consumables,
        Materials,
        Enchantments,
        Tools,
        Special
    }
}