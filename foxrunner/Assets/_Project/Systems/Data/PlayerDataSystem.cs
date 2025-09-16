using System;
using System.Collections.Generic;
using UnityEngine;
using FoxRunner.Progression;
using FoxRunner.Equipment;
using FoxRunner.Currency;

namespace FoxRunner.Data
{
    /// <summary>
    /// Central player data system managing all progression, equipment, currencies, and statistics
    /// Implements save/load functionality with data validation and event notifications
    /// Serves as the single source of truth for all player progress
    /// </summary>
    public class PlayerDataSystem : MonoBehaviour
    {
        #region Configuration
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private PlayerDataConfiguration config;

        [Header("=== DEBUG INFO (READ ONLY) ===")]
        [SerializeField] private int currentLevel;
        [SerializeField] private long currentExperience;
        [SerializeField] private long totalCoins;
        [SerializeField] private long spiritPoints;
        [SerializeField] private bool isInitialized;
        #endregion

        #region Core Data Systems
        private PlayerStatistics statistics;
        private ExperienceSystem experienceSystem;
        private CurrencySystem currencySystem;
        private EquipmentSystem equipmentSystem;
        #endregion

        #region Game Session Data
        private GameSessionData currentSession;
        private DateTime sessionStartTime;
        private float sessionPlayTime;
        #endregion

        #region Events
        public static Action<int, int> OnLevelChanged; // old level, new level
        public static Action<long, long> OnExperienceChanged; // old exp, new exp
        public static Action<CurrencyType, long, long> OnCurrencyChanged; // type, old amount, new amount
        public static Action<EquipmentSlot, EquipmentData> OnEquipmentChanged;
        public static Action<PlayerStatistics> OnStatisticsUpdated;
        public static Action OnDataSaved;
        public static Action OnDataLoaded;
        #endregion

        #region Properties
        public bool IsInitialized => isInitialized;
        public PlayerStatistics Statistics => statistics;
        public ExperienceSystem Experience => experienceSystem;
        public CurrencySystem Currency => currencySystem;
        public EquipmentSystem Equipment => equipmentSystem;
        public GameSessionData CurrentSession => currentSession;
        public float SessionPlayTime => sessionPlayTime;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            ValidateConfiguration();
        }

        void Update()
        {
            if (!isInitialized) return;

            UpdateSessionTime();
            UpdateSystems();
        }
        #endregion

        #region Initialization
        public void Initialize()
        {
            try
            {
                Debug.Log("[PlayerDataSystem] Initializing player data systems...");

                InitializeDataStructures();
                InitializeSystems();
                LoadPlayerData();

                isInitialized = true;
                Debug.Log("[PlayerDataSystem] Initialization complete");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataSystem] Initialization failed: {e.Message}");
                isInitialized = false;
            }
        }

        private void ValidateConfiguration()
        {
            if (!config)
            {
                Debug.LogError("[PlayerDataSystem] Configuration missing! Creating default configuration.");
                config = ScriptableObject.CreateInstance<PlayerDataConfiguration>();
            }
        }

        private void InitializeDataStructures()
        {
            statistics = new PlayerStatistics();
            currentSession = new GameSessionData();
        }

        private void InitializeSystems()
        {
            // Initialize existing subsystems only
            experienceSystem = new ExperienceSystem(config.experienceConfig);
            currencySystem = new CurrencySystem(config.currencyConfig);
            equipmentSystem = new EquipmentSystem(config.equipmentConfig);

            // Subscribe to system events
            SubscribeToSystemEvents();
        }

        private void SubscribeToSystemEvents()
        {
            experienceSystem.OnLevelUp += HandleLevelUp;
            experienceSystem.OnExperienceGained += HandleExperienceGained;
            currencySystem.OnCurrencyChanged += HandleCurrencyChanged;
            equipmentSystem.OnEquipmentChanged += HandleEquipmentChanged;
        }
        #endregion

        #region System Updates
        public void UpdateSystem()
        {
            if (!isInitialized) return;

            experienceSystem?.Update();
            currencySystem?.Update();
            equipmentSystem?.Update();
        }

        private void UpdateSessionTime()
        {
            sessionPlayTime += Time.deltaTime;
            currentSession.totalPlayTime += Time.deltaTime;
        }

        private void UpdateSystems()
        {
            // Update debug display values
            currentLevel = experienceSystem.CurrentLevel;
            currentExperience = experienceSystem.CurrentExperience;
            totalCoins = currencySystem.GetCurrency(CurrencyType.Coins);
            spiritPoints = currencySystem.GetCurrency(CurrencyType.SpiritPoints);
        }
        #endregion

        #region Game Session Management
        public void StartGameSession()
        {
            sessionStartTime = DateTime.Now;
            sessionPlayTime = 0f;

            currentSession = new GameSessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = sessionStartTime,
                levelAtStart = experienceSystem.CurrentLevel,
                coinsAtStart = currencySystem.GetCurrency(CurrencyType.Coins)
            };

            statistics.totalGamesPlayed++;
            Debug.Log($"[PlayerDataSystem] Game session started: {currentSession.sessionId}");
        }

        public void EndGameSession()
        {
            if (currentSession == null) return;

            currentSession.endTime = DateTime.Now;
            currentSession.duration = (float)(currentSession.endTime - currentSession.startTime).TotalSeconds;
            currentSession.levelAtEnd = experienceSystem.CurrentLevel;
            currentSession.coinsAtEnd = currencySystem.GetCurrency(CurrencyType.Coins);
            currentSession.totalPlayTime = sessionPlayTime;

            // Update statistics
            statistics.totalPlayTime += currentSession.totalPlayTime;
            statistics.longestSession = Mathf.Max(statistics.longestSession, currentSession.totalPlayTime);

            // Save session data
            SavePlayerData();

            Debug.Log($"[PlayerDataSystem] Game session ended: {currentSession.sessionId} (Duration: {currentSession.duration:F1}s)");
        }
        #endregion

        #region Event Handlers
        private void HandleLevelUp(int oldLevel, int newLevel)
        {
            statistics.highestLevel = Mathf.Max(statistics.highestLevel, newLevel);
            OnLevelChanged?.Invoke(oldLevel, newLevel);
        }

        private void HandleExperienceGained(long oldExp, long newExp)
        {
            long expGained = newExp - oldExp;
            statistics.totalExperienceEarned += expGained;
            OnExperienceChanged?.Invoke(oldExp, newExp);
        }

        private void HandleCurrencyChanged(CurrencyType type, long oldAmount, long newAmount)
        {
            switch (type)
            {
                case CurrencyType.Coins:
                    if (newAmount > oldAmount)
                        statistics.totalCoinsEarned += (newAmount - oldAmount);
                    break;
                case CurrencyType.SpiritPoints:
                    if (newAmount > oldAmount)
                        statistics.totalSpiritPointsEarned += (newAmount - oldAmount);
                    break;
            }

            OnCurrencyChanged?.Invoke(type, oldAmount, newAmount);
        }

        private void HandleEquipmentChanged(EquipmentSlot slot, EquipmentData equipment)
        {
            OnEquipmentChanged?.Invoke(slot, equipment);
        }
        #endregion

        #region Public API - Experience
        public void GainExperience(long amount, string source = "Unknown")
        {
            experienceSystem.GainExperience(amount);
            statistics.experienceSources[source] = statistics.experienceSources.GetValueOrDefault(source, 0) + amount;
        }

        public bool CanLevelUp()
        {
            return experienceSystem.CanLevelUp();
        }

        public long GetExperienceToNextLevel()
        {
            return experienceSystem.GetExperienceToNextLevel();
        }
        #endregion

        #region Public API - Currency
        public bool CanAfford(CurrencyType type, long amount)
        {
            return currencySystem.CanAfford(type, amount);
        }

        public bool SpendCurrency(CurrencyType type, long amount, string reason = "Unknown")
        {
            bool success = currencySystem.SpendCurrency(type, amount);
            if (success)
            {
                statistics.currencySpent[type] = statistics.currencySpent.GetValueOrDefault(type, 0) + amount;
                statistics.spendingReasons[reason] = statistics.spendingReasons.GetValueOrDefault(reason, 0) + amount;
            }
            return success;
        }

        public void EarnCurrency(CurrencyType type, long amount, string source = "Unknown")
        {
            currencySystem.EarnCurrency(type, amount);
            statistics.currencyEarned[type] = statistics.currencyEarned.GetValueOrDefault(type, 0) + amount;
            statistics.earningReasons[source] = statistics.earningReasons.GetValueOrDefault(source, 0) + amount;
        }

        public long GetCurrency(CurrencyType type)
        {
            return currencySystem.GetCurrency(type);
        }
        #endregion

        #region Public API - Equipment
        public bool EquipItem(EquipmentData equipment)
        {
            return equipmentSystem.EquipItem(equipment);
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            return equipmentSystem.UnequipItem(slot);
        }

        public EquipmentData GetEquippedItem(EquipmentSlot slot)
        {
            return equipmentSystem.GetEquippedItem(slot);
        }

        public Dictionary<EquipmentSlot, EquipmentData> GetAllEquippedItems()
        {
            return equipmentSystem.GetAllEquippedItems();
        }
        #endregion

        #region Save/Load System
        public void SavePlayerData()
        {
            try
            {
                PlayerSaveData saveData = new PlayerSaveData
                {
                    version = config.saveDataVersion,
                    lastSaveTime = DateTime.Now,
                    statistics = statistics,
                    experienceData = experienceSystem.GetSaveData(),
                    currencyData = currencySystem.GetSaveData(),
                    equipmentData = equipmentSystem.GetSaveData()
                };

                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString("FoxRunner_SaveData", json);
                PlayerPrefs.Save();

                OnDataSaved?.Invoke();
                Debug.Log("[PlayerDataSystem] Player data saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataSystem] Failed to save player data: {e.Message}");
            }
        }

        public void LoadPlayerData()
        {
            try
            {
                if (!PlayerPrefs.HasKey("FoxRunner_SaveData"))
                {
                    Debug.Log("[PlayerDataSystem] No save data found, starting with defaults");
                    InitializeDefaultData();
                    return;
                }

                string json = PlayerPrefs.GetString("FoxRunner_SaveData");
                PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);

                if (saveData.version != config.saveDataVersion)
                {
                    Debug.LogWarning($"[PlayerDataSystem] Save data version mismatch. Expected: {config.saveDataVersion}, Found: {saveData.version}");
                    MigrateSaveData(saveData);
                }

                LoadDataFromSave(saveData);
                OnDataLoaded?.Invoke();
                Debug.Log("[PlayerDataSystem] Player data loaded successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDataSystem] Failed to load player data: {e.Message}");
                InitializeDefaultData();
            }
        }

        private void InitializeDefaultData()
        {
            statistics = new PlayerStatistics();
            experienceSystem.LoadFromSaveData(null);
            currencySystem.LoadFromSaveData(null);
            equipmentSystem.LoadFromSaveData(null);
        }

        private void LoadDataFromSave(PlayerSaveData saveData)
        {
            statistics = saveData.statistics ?? new PlayerStatistics();
            experienceSystem.LoadFromSaveData(saveData.experienceData);
            currencySystem.LoadFromSaveData(saveData.currencyData);
            equipmentSystem.LoadFromSaveData(saveData.equipmentData);
        }

        private void MigrateSaveData(PlayerSaveData saveData)
        {
            // TODO: Implement save data migration for version compatibility
            Debug.LogWarning("[PlayerDataSystem] Save data migration not implemented yet");
        }

        public void ResetPlayerData()
        {
            if (PlayerPrefs.HasKey("FoxRunner_SaveData"))
            {
                PlayerPrefs.DeleteKey("FoxRunner_SaveData");
            }

            InitializeDefaultData();
            SavePlayerData();

            Debug.Log("[PlayerDataSystem] Player data reset to defaults");
        }
        #endregion
    }
}