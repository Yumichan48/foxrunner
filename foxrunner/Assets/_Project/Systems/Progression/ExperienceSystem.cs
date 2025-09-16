using System;
using System.Collections.Generic;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Progression
{
    /// <summary>
    /// Experience and leveling system for Fox Runner
    /// Handles 1-500 level progression with exponential curve scaling
    /// Supports multiple XP sources, level rewards, and prestige mechanics
    /// </summary>
    public class ExperienceSystem : MonoBehaviour
    {
        #region Configuration
        private ExperienceConfiguration config;
        #endregion

        #region Core Data
        private int currentLevel = 1;
        private long currentExperience = 0;
        private long lifetimeExperience = 0;
        private int totalLevelUps = 0;
        private Dictionary<string, long> experienceBySource = new Dictionary<string, long>();
        #endregion

        #region Level Curve Cache
        private static readonly Dictionary<int, long> levelExperienceCache = new Dictionary<int, long>();
        private static readonly Dictionary<int, long> cumulativeExperienceCache = new Dictionary<int, long>();
        private static bool cacheInitialized = false;
        #endregion

        #region Events
        public Action<int, int> OnLevelUp; // old level, new level
        public Action<long, long> OnExperienceGained; // old exp, new exp
        public Action<int, long> OnRewardEarned; // level, reward amount
        #endregion

        #region Properties
        public int CurrentLevel => currentLevel;
        public long CurrentExperience => currentExperience;
        public long LifetimeExperience => lifetimeExperience;
        public int TotalLevelUps => totalLevelUps;
        public bool IsMaxLevel => currentLevel >= GetMaxLevel();
        public float LevelProgress => GetLevelProgress();
        #endregion

        #region Constructor
        public ExperienceSystem(ExperienceConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            InitializeLevelCache();
        }
        #endregion

        #region Level Curve Calculations
        private static void InitializeLevelCache()
        {
            if (cacheInitialized) return;

            // Pre-calculate experience requirements for all 500 levels
            for (int level = 1; level <= 500; level++)
            {
                long expForLevel = CalculateExperienceForLevel(level);
                levelExperienceCache[level] = expForLevel;

                long cumulativeExp = 0;
                for (int i = 2; i <= level; i++)
                {
                    cumulativeExp += levelExperienceCache[i];
                }
                cumulativeExperienceCache[level] = cumulativeExp;
            }

            cacheInitialized = true;
            Debug.Log("[ExperienceSystem] Level cache initialized for 500 levels");
        }

        private static long CalculateExperienceForLevel(int level)
        {
            if (level <= 1) return 0;

            // Multi-stage experience curve for 1-500 levels
            if (level <= 10)
            {
                // Early game: Linear progression (100-900 XP per level)
                return 100 * level;
            }
            else if (level <= 50)
            {
                // Early-mid game: Gentle exponential (1K-25K XP per level)
                return (long)(1000 * Math.Pow(level - 9, 1.1));
            }
            else if (level <= 100)
            {
                // Mid game: Moderate exponential (25K-150K XP per level)
                return (long)(25000 * Math.Pow(level - 49, 1.05));
            }
            else if (level <= 200)
            {
                // Mid-late game: Steeper curve (150K-800K XP per level)
                return (long)(150000 * Math.Pow(level - 99, 1.02));
            }
            else if (level <= 350)
            {
                // Late game: High requirements (800K-5M XP per level)
                return (long)(800000 * Math.Pow(level - 199, 1.015));
            }
            else if (level <= 450)
            {
                // End game: Very high requirements (5M-20M XP per level)
                return (long)(5000000 * Math.Pow(level - 349, 1.01));
            }
            else
            {
                // Ultra end game: Extreme requirements (20M-100M+ XP per level)
                return (long)(20000000 * Math.Pow(level - 449, 1.005));
            }
        }

        public static long GetExperienceRequiredForLevel(int level)
        {
            if (!cacheInitialized) InitializeLevelCache();
            return levelExperienceCache.GetValueOrDefault(level, 0);
        }

        public static long GetCumulativeExperienceForLevel(int level)
        {
            if (!cacheInitialized) InitializeLevelCache();
            return cumulativeExperienceCache.GetValueOrDefault(level, 0);
        }
        #endregion

        #region Public API
        public void GainExperience(long amount, string source = "Unknown")
        {
            if (amount <= 0 || IsMaxLevel) return;

            long oldExperience = currentExperience;
            currentExperience += amount;
            lifetimeExperience += amount;

            // Track experience by source
            experienceBySource[source] = experienceBySource.GetValueOrDefault(source, 0) + amount;

            OnExperienceGained?.Invoke(oldExperience, currentExperience);

            // Check for level ups
            CheckForLevelUp();
        }

        public bool CanLevelUp()
        {
            return !IsMaxLevel && currentExperience >= GetExperienceToNextLevel();
        }

        public long GetExperienceToNextLevel()
        {
            if (IsMaxLevel) return 0;
            return GetExperienceRequiredForLevel(currentLevel + 1) - currentExperience;
        }

        public float GetLevelProgress()
        {
            if (IsMaxLevel) return 1f;

            long expForCurrentLevel = GetCumulativeExperienceForLevel(currentLevel);
            long expForNextLevel = GetCumulativeExperienceForLevel(currentLevel + 1);
            long expInLevel = currentExperience - expForCurrentLevel;
            long expRequiredForLevel = expForNextLevel - expForCurrentLevel;

            return expRequiredForLevel > 0 ? (float)expInLevel / expRequiredForLevel : 0f;
        }

        public int GetMaxLevel()
        {
            return config?.maxLevel ?? 500;
        }

        public LevelRewards GetRewardsForLevel(int level)
        {
            return CalculateLevelRewards(level);
        }
        #endregion

        #region Level Up System
        private void CheckForLevelUp()
        {
            while (CanLevelUp())
            {
                PerformLevelUp();
            }
        }

        private void PerformLevelUp()
        {
            int oldLevel = currentLevel;
            currentLevel++;
            totalLevelUps++;

            OnLevelUp?.Invoke(oldLevel, currentLevel);

            // Calculate and award level rewards
            LevelRewards rewards = CalculateLevelRewards(currentLevel);
            AwardLevelRewards(rewards);

            Debug.Log($"[ExperienceSystem] Level up! {oldLevel} -> {currentLevel}");
        }

        private LevelRewards CalculateLevelRewards(int level)
        {
            LevelRewards rewards = new LevelRewards { level = level };

            // Coin rewards scale with level
            if (level <= 50)
            {
                rewards.coins = level * 100; // 100-5,000 coins
            }
            else if (level <= 100)
            {
                rewards.coins = (level - 50) * 500 + 5000; // 5,500-30,000 coins
            }
            else if (level <= 200)
            {
                rewards.coins = (level - 100) * 1000 + 30000; // 31,000-130,000 coins
            }
            else
            {
                rewards.coins = (level - 200) * 2500 + 130000; // 132,500+ coins
            }

            // Spirit Points awarded at milestone levels
            if (level % 10 == 0) // Every 10 levels
            {
                rewards.spiritPoints = level / 10; // 1, 2, 3... spirit points
            }

            // Special milestone rewards
            if (level % 25 == 0) // Every 25 levels
            {
                rewards.specialReward = true;
                rewards.specialRewardDescription = GetMilestoneReward(level);
            }

            // Fox Gems (premium currency) for major milestones
            if (level % 50 == 0) // Every 50 levels
            {
                rewards.foxGems = 1 + (level / 100); // 1-6 gems depending on level
            }

            return rewards;
        }

        private string GetMilestoneReward(int level)
        {
            return level switch
            {
                25 => "Unlocked: Double Jump Ability",
                50 => "Unlocked: Dash Ability",
                75 => "Unlocked: Wall Slide Ability",
                100 => "Unlocked: First Ascension",
                125 => "Unlocked: Companion System",
                150 => "Unlocked: Crafting System",
                175 => "Unlocked: Village System",
                200 => "Unlocked: Advanced Ascension",
                250 => "Unlocked: Divinity System",
                300 => "Unlocked: Ultra Ascension",
                350 => "Unlocked: Cosmic Abilities",
                400 => "Unlocked: Primordial Powers",
                450 => "Unlocked: Eternal Mastery",
                500 => "Achieved: Maximum Level Mastery",
                _ => $"Level {level} Milestone Achieved"
            };
        }

        private void AwardLevelRewards(LevelRewards rewards)
        {
            if (rewards.coins > 0)
            {
                // Award coins through currency system
                OnRewardEarned?.Invoke(rewards.level, rewards.coins);
            }

            if (rewards.spiritPoints > 0)
            {
                // Award spirit points through currency system
                OnRewardEarned?.Invoke(rewards.level, rewards.spiritPoints);
            }

            if (rewards.foxGems > 0)
            {
                // Award fox gems through currency system
                OnRewardEarned?.Invoke(rewards.level, rewards.foxGems);
            }

            if (rewards.specialReward)
            {
                Debug.Log($"[ExperienceSystem] Special reward at level {rewards.level}: {rewards.specialRewardDescription}");
            }
        }
        #endregion

        #region Statistics & Analytics
        public Dictionary<string, long> GetExperienceBreakdown()
        {
            return new Dictionary<string, long>(experienceBySource);
        }

        public float GetAverageExperiencePerSource(string source)
        {
            if (!experienceBySource.ContainsKey(source)) return 0f;
            return experienceBySource[source];
        }

        public ExperienceStats GetDetailedStats()
        {
            return new ExperienceStats
            {
                currentLevel = currentLevel,
                currentExperience = currentExperience,
                lifetimeExperience = lifetimeExperience,
                totalLevelUps = totalLevelUps,
                experienceToNextLevel = GetExperienceToNextLevel(),
                levelProgress = GetLevelProgress(),
                averageExpPerLevel = lifetimeExperience / Math.Max(1, currentLevel - 1),
                experienceBreakdown = new Dictionary<string, long>(experienceBySource)
            };
        }
        #endregion

        #region Save/Load
        public ExperienceSaveData GetSaveData()
        {
            return new ExperienceSaveData
            {
                currentLevel = currentLevel,
                currentExperience = currentExperience,
                lifetimeExperience = lifetimeExperience,
                totalLevelUps = totalLevelUps,
                experienceBySource = new Dictionary<string, long>(experienceBySource)
            };
        }

        public void LoadFromSaveData(ExperienceSaveData saveData)
        {
            if (saveData == null)
            {
                // Initialize with defaults
                currentLevel = 1;
                currentExperience = 0;
                lifetimeExperience = 0;
                totalLevelUps = 0;
                experienceBySource.Clear();
                return;
            }

            currentLevel = Math.Max(1, saveData.currentLevel);
            currentExperience = Math.Max(0, saveData.currentExperience);
            lifetimeExperience = Math.Max(0, saveData.lifetimeExperience);
            totalLevelUps = Math.Max(0, saveData.totalLevelUps);
            experienceBySource = saveData.experienceBySource ?? new Dictionary<string, long>();
        }
        #endregion

        #region Update Loop
        public void Update()
        {
            // Experience system doesn't need per-frame updates
            // All operations are event-driven
        }
        #endregion
    }

    #region Supporting Data Structures
    [Serializable]
    public class LevelRewards
    {
        public int level;
        public long coins;
        public long spiritPoints;
        public int foxGems;
        public bool specialReward;
        public string specialRewardDescription;
    }

    [Serializable]
    public class ExperienceStats
    {
        public int currentLevel;
        public long currentExperience;
        public long lifetimeExperience;
        public int totalLevelUps;
        public long experienceToNextLevel;
        public float levelProgress;
        public long averageExpPerLevel;
        public Dictionary<string, long> experienceBreakdown;
    }
    #endregion
}