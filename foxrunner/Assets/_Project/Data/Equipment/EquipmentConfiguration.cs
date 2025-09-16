using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Equipment
{
    /// <summary>
    /// Configuration for the equipment system
    /// Defines upgrade costs, stat ranges, special effects, and equipment balance
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentConfig", menuName = "FoxRunner/Equipment/Equipment Configuration")]
    public class EquipmentConfiguration : ScriptableObject
    {
        [Header("=== EQUIPMENT BALANCE ===")]
        [Tooltip("Base stat multiplier for all equipment")]
        [Range(0.1f, 10f)]
        public float baseStatMultiplier = 1f;

        [Tooltip("Stat scaling per tier")]
        public TierStatMultiplier[] tierMultipliers;

        [Tooltip("Maximum equipment level")]
        [Range(1, 1000)]
        public int maxEquipmentLevel = 100;

        [Header("=== UPGRADE COSTS ===")]
        [Tooltip("Base upgrade cost configurations")]
        public UpgradeCostConfiguration[] upgradeCosts;

        [Tooltip("Cost scaling factor per level")]
        [Range(1f, 3f)]
        public float costScalingFactor = 1.1f;

        [Tooltip("Premium currency required for high-tier upgrades")]
        public bool requirePremiumForHighTier = true;

        [Header("=== STAT RANGES ===")]
        [Tooltip("Stat value ranges for different equipment types")]
        public StatRangeConfiguration[] statRanges;

        [Header("=== SET BONUSES ===")]
        [Tooltip("Set bonus configurations")]
        public SetBonusConfiguration[] setBonuses;

        [Tooltip("Pieces required for set bonuses")]
        public int[] setBonusThresholds = { 3, 5, 7 };

        [Header("=== SPECIAL EFFECTS ===")]
        [Tooltip("Special effect configurations")]
        public SpecialEffectConfiguration[] specialEffects;

        [Tooltip("Chance for special effects on high-tier equipment")]
        [Range(0f, 1f)]
        public float specialEffectChance = 0.8f;

        [Header("=== ACQUISITION ===")]
        [Tooltip("Equipment drop rates by tier")]
        public EquipmentDropRate[] dropRates;

        [Tooltip("Crafting unlock requirements")]
        public CraftingUnlockRequirement[] craftingUnlocks;

        [Header("=== VISUAL CUSTOMIZATION ===")]
        [Tooltip("Equipment visual configurations")]
        public EquipmentVisualConfiguration[] visualConfigurations;

        [Header("=== DEBUG ===")]
        [Tooltip("Enable debug mode for equipment system")]
        public bool enableDebugMode = false;

        [Tooltip("Free upgrades in debug mode")]
        public bool freeUpgradesInDebug = false;

        void OnValidate()
        {
            // Validate tier multipliers
            if (tierMultipliers == null || tierMultipliers.Length != 6)
            {
                Debug.LogWarning("[EquipmentConfiguration] Tier multipliers should have 6 entries (one for each tier)!");
            }

            // Validate upgrade costs
            if (upgradeCosts == null || upgradeCosts.Length == 0)
            {
                Debug.LogWarning("[EquipmentConfiguration] Upgrade costs configuration is missing!");
            }

            // Validate set bonus thresholds
            if (setBonusThresholds == null || setBonusThresholds.Length == 0)
            {
                Debug.LogWarning("[EquipmentConfiguration] Set bonus thresholds are missing!");
                setBonusThresholds = new int[] { 3, 5, 7 };
            }
        }

        public float GetTierMultiplier(EquipmentTier tier)
        {
            if (tierMultipliers == null || tierMultipliers.Length <= (int)tier)
                return 1f;

            return tierMultipliers[(int)tier].multiplier;
        }

        public UpgradeCostConfiguration GetUpgradeCost(EquipmentTier tier)
        {
            if (upgradeCosts == null) return null;

            foreach (var cost in upgradeCosts)
            {
                if (cost.tier == tier)
                    return cost;
            }

            return null;
        }

        public StatRangeConfiguration GetStatRange(StatType statType)
        {
            if (statRanges == null) return null;

            foreach (var range in statRanges)
            {
                if (range.statType == statType)
                    return range;
            }

            return null;
        }
    }

    [System.Serializable]
    public class TierStatMultiplier
    {
        [Tooltip("Equipment tier")]
        public EquipmentTier tier;

        [Tooltip("Stat multiplier for this tier")]
        [Range(0.1f, 10f)]
        public float multiplier = 1f;

        [Tooltip("Visual effect intensity for this tier")]
        [Range(0f, 2f)]
        public float effectIntensity = 1f;
    }

    [System.Serializable]
    public class UpgradeCostConfiguration
    {
        [Tooltip("Equipment tier")]
        public EquipmentTier tier;

        [Tooltip("Base coin cost per upgrade")]
        public long baseCoinCost = 100;

        [Tooltip("Base material cost per upgrade")]
        public int baseMaterialCost = 10;

        [Tooltip("Spirit points required for upgrades")]
        public int spiritPointCost = 0;

        [Tooltip("Premium currency required")]
        public int premiumCurrencyCost = 0;

        [Tooltip("Level at which premium currency is required")]
        public int premiumRequiredLevel = 50;
    }

    [System.Serializable]
    public class StatRangeConfiguration
    {
        [Tooltip("Stat type")]
        public StatType statType;

        [Tooltip("Minimum stat value")]
        public float minValue = 0f;

        [Tooltip("Maximum stat value")]
        public float maxValue = 1f;

        [Tooltip("Stat scaling type")]
        public StatScalingType scalingType = StatScalingType.Linear;

        [Tooltip("Is this stat a percentage?")]
        public bool isPercentage = false;
    }

    [System.Serializable]
    public class SetBonusConfiguration
    {
        [Tooltip("Set name")]
        public string setName;

        [Tooltip("Equipment tier for this set")]
        public EquipmentTier tier;

        [Tooltip("Number of pieces required")]
        [Range(2, 7)]
        public int piecesRequired = 3;

        [Tooltip("Stat bonuses granted by this set")]
        public StatBonus[] statBonuses;

        [Tooltip("Special effects granted by this set")]
        public string[] specialEffects;

        [Tooltip("Set bonus description")]
        [TextArea(2, 4)]
        public string description;
    }

    [System.Serializable]
    public class StatBonus
    {
        public StatType statType;
        public float value;
        public bool isPercentage;
    }

    [System.Serializable]
    public class SpecialEffectConfiguration
    {
        [Tooltip("Effect name")]
        public string effectName;

        [Tooltip("Effect description")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Equipment slots that can have this effect")]
        public EquipmentSlot[] applicableSlots;

        [Tooltip("Minimum tier required for this effect")]
        public EquipmentTier minimumTier = EquipmentTier.Rare;

        [Tooltip("Effect parameters")]
        public EffectParameter[] parameters;
    }

    [System.Serializable]
    public class EffectParameter
    {
        public string parameterName;
        public float value;
        public string description;
    }

    [System.Serializable]
    public class EquipmentDropRate
    {
        [Tooltip("Equipment tier")]
        public EquipmentTier tier;

        [Tooltip("Drop rate percentage")]
        [Range(0f, 100f)]
        public float dropRate = 10f;

        [Tooltip("Source of this equipment (crafting, drops, etc.)")]
        public EquipmentSource source = EquipmentSource.Drops;
    }

    [System.Serializable]
    public class CraftingUnlockRequirement
    {
        [Tooltip("Equipment tier")]
        public EquipmentTier tier;

        [Tooltip("Player level required")]
        public int requiredLevel = 1;

        [Tooltip("Previous equipment required")]
        public EquipmentTier requiredPreviousTier = EquipmentTier.Common;

        [Tooltip("Materials required to unlock crafting")]
        public MaterialRequirement[] materials;
    }

    [System.Serializable]
    public class MaterialRequirement
    {
        public string materialName;
        public int amount;
    }

    [System.Serializable]
    public class EquipmentVisualConfiguration
    {
        [Tooltip("Equipment slot")]
        public EquipmentSlot slot;

        [Tooltip("Equipment tier")]
        public EquipmentTier tier;

        [Tooltip("Visual effects")]
        public VisualEffect[] effects;

        [Tooltip("Color scheme")]
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;

        [Tooltip("Particle effects")]
        public ParticleEffectConfiguration particleEffects;
    }

    [System.Serializable]
    public class VisualEffect
    {
        public string effectName;
        public float intensity = 1f;
        public Color color = Color.white;
    }

    [System.Serializable]
    public class ParticleEffectConfiguration
    {
        public bool enableParticles = false;
        public Color particleColor = Color.white;
        public float particleIntensity = 1f;
        public string particleSystemPrefab;
    }

    public enum StatScalingType
    {
        Linear,      // Direct linear scaling
        Exponential, // Exponential scaling for high-end equipment
        Logarithmic, // Diminishing returns
        Custom       // Custom curve defined elsewhere
    }

    public enum EquipmentSource
    {
        Drops,       // Found as loot
        Crafting,    // Created through crafting
        Shop,        // Purchased from shops
        Rewards,     // Quest/achievement rewards
        Special      // Special events or unique acquisition
    }
}