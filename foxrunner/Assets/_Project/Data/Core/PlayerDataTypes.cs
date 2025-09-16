using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoxRunner.Data
{
    /// <summary>
    /// Core data types and structures for the player data system
    /// All serializable classes for save/load functionality
    /// </summary>

    #region Save Data Structure
    [Serializable]
    public class PlayerSaveData
    {
        public int version;
        public DateTime lastSaveTime;
        public PlayerStatistics statistics;
        public ExperienceSaveData experienceData;
        public CurrencySaveData currencyData;
        public EquipmentSaveData equipmentData;
        public CompanionSaveData companionData;
        public VillageSaveData villageData;
        public AscensionSaveData ascensionData;
    }
    #endregion

    #region Player Statistics
    [Serializable]
    public class PlayerStatistics
    {
        [Header("=== BASIC STATS ===")]
        public int totalGamesPlayed = 0;
        public float totalPlayTime = 0f;
        public float longestSession = 0f;
        public DateTime firstPlayDate = DateTime.Now;
        public DateTime lastPlayDate = DateTime.Now;

        [Header("=== PROGRESSION STATS ===")]
        public int highestLevel = 1;
        public long totalExperienceEarned = 0;
        public int totalLevelUps = 0;
        public int totalAscensions = 0;

        [Header("=== CURRENCY STATS ===")]
        public Dictionary<CurrencyType, long> currencyEarned = new Dictionary<CurrencyType, long>();
        public Dictionary<CurrencyType, long> currencySpent = new Dictionary<CurrencyType, long>();
        public long totalCoinsEarned = 0;
        public long totalSpiritPointsEarned = 0;

        [Header("=== GAMEPLAY STATS ===")]
        public long totalDistanceTraveled = 0;
        public int totalJumps = 0;
        public int totalDashes = 0;
        public int totalWallJumps = 0;
        public int totalCollectiblesGathered = 0;
        public int totalFruitsCollected = 0;

        [Header("=== COMBAT STATS ===")]
        public int totalEnemiesDefeated = 0;
        public int totalBossesDefeated = 0;
        public int totalDeaths = 0;
        public int bestComboCount = 0;

        [Header("=== EQUIPMENT STATS ===")]
        public int totalEquipmentUpgrades = 0;
        public int totalItemsCrafted = 0;
        public Dictionary<EquipmentSlot, int> equipmentUpgradesBySlot = new Dictionary<EquipmentSlot, int>();

        [Header("=== COMPANION STATS ===")]
        public int totalCompanionsUnlocked = 0;
        public int totalCompanionMissionsCompleted = 0;
        public Dictionary<string, int> companionInteractions = new Dictionary<string, int>();

        [Header("=== VILLAGE STATS ===")]
        public int totalBuildingsConstructed = 0;
        public int totalBuildingUpgrades = 0;
        public int totalQuestsCompleted = 0;
        public int totalEventsParticipated = 0;

        [Header("=== SEASONAL STATS ===")]
        public Dictionary<SeasonType, float> timeSpentInSeasons = new Dictionary<SeasonType, float>();
        public Dictionary<SeasonType, int> seasonalBossesDefeated = new Dictionary<SeasonType, int>();

        [Header("=== TRACKING MAPS ===")]
        public Dictionary<string, long> experienceSources = new Dictionary<string, long>();
        public Dictionary<string, long> earningReasons = new Dictionary<string, long>();
        public Dictionary<string, long> spendingReasons = new Dictionary<string, long>();
        public Dictionary<string, int> achievementsUnlocked = new Dictionary<string, int>();

        public PlayerStatistics()
        {
            InitializeDictionaries();
        }

        private void InitializeDictionaries()
        {
            // Initialize currency dictionaries
            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                currencyEarned[currency] = 0;
                currencySpent[currency] = 0;
            }

            // Initialize equipment dictionaries
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equipmentUpgradesBySlot[slot] = 0;
            }

            // Initialize seasonal dictionaries
            foreach (SeasonType season in Enum.GetValues(typeof(SeasonType)))
            {
                timeSpentInSeasons[season] = 0f;
                seasonalBossesDefeated[season] = 0;
            }
        }
    }
    #endregion

    #region Game Session Data
    [Serializable]
    public class GameSessionData
    {
        public string sessionId;
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public float totalPlayTime;
        public int levelAtStart;
        public int levelAtEnd;
        public long coinsAtStart;
        public long coinsAtEnd;
        public long experienceGained;
        public int collectiblesGathered;
        public float distanceTraveled;
        public Dictionary<string, object> customData = new Dictionary<string, object>();
    }
    #endregion

    #region Currency System
    public enum CurrencyType
    {
        Coins,                  // Basic currency earned through gameplay
        SpiritPoints,          // Advanced currency from ascension
        SpringEssence,         // Seasonal essence from Spring dimension
        SummerEssence,         // Seasonal essence from Summer dimension
        AutumnEssence,         // Seasonal essence from Autumn dimension
        WinterEssence,         // Seasonal essence from Winter dimension
        Materials,             // Crafting materials
        FoxGems,              // Premium currency
        DivinityPoints,       // Ultra-rare currency from divine content
        UltraSpiritPoints,    // Ultra ascension currency
        VillageTokens,        // Village-specific currency
        CompanionBonds        // Companion relationship currency
    }

    [Serializable]
    public class CurrencySaveData
    {
        public Dictionary<CurrencyType, long> currencies = new Dictionary<CurrencyType, long>();
        public Dictionary<CurrencyType, long> lifetimeEarned = new Dictionary<CurrencyType, long>();
        public Dictionary<CurrencyType, long> lifetimeSpent = new Dictionary<CurrencyType, long>();
    }
    #endregion

    #region Equipment System
    public enum EquipmentSlot
    {
        TailBrush,           // Affects movement speed and trail effects
        PawGuards,           // Affects jump height and landing stability
        MysticCollar,        // Affects magic resistance and mana
        SeasonalScarf,       // Seasonal bonuses and weather resistance
        SpiritBell,          // Spirit point generation and collection radius
        AncientMask,         // Experience bonuses and wisdom effects
        CompanionCharm       // Companion effectiveness and bond growth
    }

    public enum EquipmentTier
    {
        Common,              // White - Basic equipment
        Uncommon,            // Green - Slightly improved stats
        Rare,                // Blue - Good stat bonuses
        Epic,                // Purple - Significant improvements
        Legendary,           // Orange - Major stat bonuses
        Eternal              // Gold - Maximum tier with special effects
    }

    [Serializable]
    public class EquipmentData
    {
        public string id;
        public string name;
        public string description;
        public EquipmentSlot slot;
        public EquipmentTier tier;
        public int level;
        public Dictionary<StatType, float> baseStats;
        public Dictionary<StatType, float> bonusStats;
        public List<string> specialEffects;
        public bool isEquipped;
        public DateTime acquiredDate;
        public int upgradeCount;
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public Dictionary<EquipmentSlot, EquipmentData> equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
        public List<EquipmentData> inventory = new List<EquipmentData>();
        public Dictionary<string, int> upgradeProgress = new Dictionary<string, int>();
    }

    public enum StatType
    {
        MovementSpeed,
        JumpHeight,
        DashDistance,
        ExperienceMultiplier,
        CoinMultiplier,
        SpiritPointMultiplier,
        CollectionRadius,
        Health,
        Mana,
        CriticalChance,
        DamageReduction,
        SeasonalBonus
    }
    #endregion

    #region Experience System
    [Serializable]
    public class ExperienceSaveData
    {
        public int currentLevel = 1;
        public long currentExperience = 0;
        public long lifetimeExperience = 0;
        public int totalLevelUps = 0;
        public Dictionary<string, long> experienceBySource = new Dictionary<string, long>();
    }
    #endregion

    #region Companion System
    [Serializable]
    public class CompanionSaveData
    {
        public List<CompanionData> ownedCompanions = new List<CompanionData>();
        public string activeCompanionId;
        public Dictionary<string, int> companionLevels = new Dictionary<string, int>();
        public Dictionary<string, float> companionBonds = new Dictionary<string, float>();
        public Dictionary<string, List<string>> completedMissions = new Dictionary<string, List<string>>();
    }

    [Serializable]
    public class CompanionData
    {
        public string id;
        public string name;
        public CompanionType type;
        public int level;
        public float bondLevel;
        public int evolutionStage;
        public bool isActive;
        public DateTime unlockDate;
        public List<string> completedMissions;
        public Dictionary<StatType, float> bonusStats;
    }

    public enum CompanionType
    {
        SpringSprite,        // Spring companion - growth and healing
        SummerPhoenix,       // Summer companion - fire and energy
        AutumnWisp,          // Autumn companion - wind and collection
        WinterFrost,         // Winter companion - ice and preservation
        SpiritGuardian,      // Spirit companion - protection and wisdom
        CosmicEntity,        // Cosmic companion - space and time
        PrimordialBeast      // Ancient companion - raw power
    }
    #endregion

    #region Village System
    [Serializable]
    public class VillageSaveData
    {
        public int villageLevel = 1;
        public List<BuildingData> buildings = new List<BuildingData>();
        public List<string> completedQuests = new List<string>();
        public Dictionary<string, int> buildingLevels = new Dictionary<string, int>();
        public List<string> activeEvents = new List<string>();
        public long villagePopulation = 100;
    }

    [Serializable]
    public class BuildingData
    {
        public string id;
        public string name;
        public BuildingType type;
        public int level;
        public bool isConstructed;
        public DateTime constructionDate;
        public Dictionary<string, float> productionRates;
        public List<string> availableUpgrades;
    }

    public enum BuildingType
    {
        CompanionHouse,      // Houses for companions
        CraftingWorkshop,    // Crafting stations
        ResourceGenerator,   // Generates materials over time
        TrainingGrounds,     // Companion training facility
        SpiritShrine,        // Spirit point generation
        MarketStall,         // Trading and commerce
        WisdomLibrary       // Knowledge and research
    }
    #endregion

    #region Ascension System
    [Serializable]
    public class AscensionSaveData
    {
        public int totalAscensions = 0;
        public long spiritPoints = 0;
        public long lifetimeSpiritPoints = 0;
        public Dictionary<string, int> spiritTreeUpgrades = new Dictionary<string, int>();
        public Dictionary<string, bool> divinityUnlocks = new Dictionary<string, bool>();
        public int ultraAscensions = 0;
        public Dictionary<StoneOfTimeType, int> stonesOfTime = new Dictionary<StoneOfTimeType, int>();
    }

    public enum StoneOfTimeType
    {
        Swift,               // Movement speed bonuses
        Wise,                // Experience multipliers
        Wealthy,             // Currency multipliers
        Mighty,              // Combat bonuses
        Eternal,             // Prestige and permanence
        Cosmic,              // Universal bonuses
        Primordial          // Ultimate power
    }
    #endregion

    #region Seasonal System
    public enum SeasonType
    {
        Spring,              // Growth, renewal, life energy
        Summer,              // Heat, energy, activity
        Autumn,              // Harvest, change, wisdom
        Winter               // Rest, preservation, endurance
    }

    [Serializable]
    public class SeasonalData
    {
        public SeasonType currentSeason = SeasonType.Spring;
        public float timeInCurrentSeason = 0f;
        public Dictionary<SeasonType, int> seasonalLevels = new Dictionary<SeasonType, int>();
        public Dictionary<SeasonType, bool> bossesDefeated = new Dictionary<SeasonType, bool>();
        public Dictionary<SeasonType, List<string>> unlockedAreas = new Dictionary<SeasonType, List<string>>();
    }
    #endregion

}