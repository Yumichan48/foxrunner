using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Progression
{
    /// <summary>
    /// Configuration for the experience and leveling system
    /// Defines level curve parameters, rewards, and milestone unlocks
    /// </summary>
    [CreateAssetMenu(fileName = "ExperienceConfig", menuName = "FoxRunner/Progression/Experience Configuration")]
    public class ExperienceConfiguration : ScriptableObject
    {
        [Header("=== LEVEL PROGRESSION ===")]
        [Tooltip("Maximum level achievable")]
        [Range(100, 1000)]
        public int maxLevel = 500;

        [Tooltip("Base experience multiplier")]
        [Range(0.1f, 10f)]
        public float baseExperienceMultiplier = 1f;

        [Tooltip("Experience curve steepness")]
        [Range(1f, 3f)]
        public float experienceCurveSteepness = 1.2f;

        [Header("=== LEVEL REWARDS ===")]
        [Tooltip("Base coin reward per level")]
        [Range(1, 10000)]
        public int baseCoinReward = 100;

        [Tooltip("Coin reward scaling factor")]
        [Range(1f, 5f)]
        public float coinRewardScaling = 1.5f;

        [Tooltip("Spirit point reward frequency (every X levels)")]
        [Range(1, 50)]
        public int spiritPointFrequency = 10;

        [Tooltip("Fox gem reward frequency (every X levels)")]
        [Range(10, 100)]
        public int foxGemFrequency = 50;

        [Header("=== MILESTONE REWARDS ===")]
        [Tooltip("Levels that grant special milestone rewards")]
        public int[] milestoneRewardLevels = { 25, 50, 75, 100, 125, 150, 175, 200, 250, 300, 350, 400, 450, 500 };

        [Tooltip("Special abilities unlocked at milestone levels")]
        public MilestoneReward[] milestoneRewards;

        [Header("=== EXPERIENCE SOURCES ===")]
        [Tooltip("Experience multipliers for different sources")]
        public ExperienceSourceMultiplier[] sourceMultipliers;

        [Header("=== PRESTIGE SYSTEM ===")]
        [Tooltip("Enable prestige/ascension at max level")]
        public bool enablePrestige = true;

        [Tooltip("Spirit points gained per prestige")]
        [Range(1, 100)]
        public int spiritPointsPerPrestige = 10;

        [Tooltip("Experience bonus per prestige level")]
        [Range(0.01f, 1f)]
        public float experienceBonusPerPrestige = 0.05f;

        [Header("=== DEBUG ===")]
        [Tooltip("Enable debug experience gains")]
        public bool enableDebugMode = false;

        [Tooltip("Debug experience multiplier")]
        [Range(1f, 1000f)]
        public float debugExperienceMultiplier = 1f;

        void OnValidate()
        {
            // Validate milestone rewards array
            if (milestoneRewards != null && milestoneRewards.Length != milestoneRewardLevels.Length)
            {
                Debug.LogWarning("[ExperienceConfiguration] Milestone rewards array length doesn't match milestone levels array length!");
            }

            // Ensure max level is reasonable
            if (maxLevel < 100)
            {
                Debug.LogWarning("[ExperienceConfiguration] Max level seems too low for a long-term progression game!");
            }

            // Validate experience curve parameters
            if (experienceCurveSteepness < 1f)
            {
                Debug.LogWarning("[ExperienceConfiguration] Experience curve steepness should be >= 1.0 for proper progression!");
                experienceCurveSteepness = 1f;
            }
        }

        public float GetExperienceMultiplier(string source)
        {
            if (sourceMultipliers == null) return 1f;

            foreach (var multiplier in sourceMultipliers)
            {
                if (multiplier.sourceName == source)
                {
                    return multiplier.multiplier;
                }
            }

            return 1f; // Default multiplier
        }

        public MilestoneReward GetMilestoneReward(int level)
        {
            if (milestoneRewards == null) return null;

            for (int i = 0; i < milestoneRewardLevels.Length && i < milestoneRewards.Length; i++)
            {
                if (milestoneRewardLevels[i] == level)
                {
                    return milestoneRewards[i];
                }
            }

            return null;
        }
    }

    [System.Serializable]
    public class MilestoneReward
    {
        [Tooltip("Level at which this reward is granted")]
        public int level;

        [Tooltip("Name of the reward/unlock")]
        public string rewardName;

        [Tooltip("Description of what is unlocked")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Type of unlock")]
        public MilestoneType type;

        [Tooltip("Bonus currency awarded")]
        public CurrencyReward[] currencyRewards;

        [Tooltip("Abilities or features unlocked")]
        public string[] unlockedFeatures;

        [Tooltip("Achievement ID associated with this milestone")]
        public string achievementId;
    }

    [System.Serializable]
    public class CurrencyReward
    {
        public CurrencyType currencyType;
        public long amount;
    }

    [System.Serializable]
    public class ExperienceSourceMultiplier
    {
        [Tooltip("Name of the experience source")]
        public string sourceName;

        [Tooltip("Experience multiplier for this source")]
        [Range(0.1f, 10f)]
        public float multiplier = 1f;

        [Tooltip("Description of this source")]
        public string description;
    }

    public enum MilestoneType
    {
        AbilityUnlock,       // Unlocks new character abilities
        SystemUnlock,        // Unlocks new game systems
        PrestigeUnlock,      // Unlocks prestige/ascension features
        CurrencyUnlock,      // Unlocks new currency types
        ContentUnlock,       // Unlocks new content areas
        BonusReward,         // Pure reward milestone
        AchievementUnlock    // Unlocks achievement categories
    }
}