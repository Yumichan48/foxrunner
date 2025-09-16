using UnityEngine;
using FoxRunner.Progression;
using FoxRunner.Currency;
using FoxRunner.Equipment;
using FoxRunner.Crafting;

namespace FoxRunner.Data
{
    /// <summary>
    /// Central configuration ScriptableObject for all player data systems
    /// Contains references to all subsystem configurations
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerDataConfig", menuName = "FoxRunner/Data/Player Data Configuration")]
    public class PlayerDataConfiguration : ScriptableObject
    {
        [Header("=== SAVE SYSTEM ===")]
        [Tooltip("Save data version for migration compatibility")]
        public int saveDataVersion = 1;

        [Tooltip("Auto-save interval in seconds")]
        [Range(30f, 300f)]
        public float autoSaveInterval = 60f;

        [Tooltip("Enable data encryption")]
        public bool encryptSaveData = true;

        [Header("=== SUBSYSTEM CONFIGURATIONS ===")]
        [Tooltip("Experience and leveling configuration")]
        public ExperienceConfiguration experienceConfig;

        [Tooltip("Currency system configuration")]
        public CurrencyConfiguration currencyConfig;

        [Tooltip("Equipment system configuration")]
        public EquipmentConfiguration equipmentConfig;

        [Tooltip("Crafting system configuration")]
        public CraftingConfiguration craftingConfig;

        // NOTE: Additional system configurations will be added as systems are implemented
        // [Tooltip("Companion system configuration")]
        // public CompanionConfiguration companionConfig;

        // [Tooltip("Village system configuration")]
        // public VillageConfiguration villageConfig;

        // [Tooltip("Ascension system configuration")]
        // public AscensionConfiguration ascensionConfig;

        [Header("=== ANALYTICS ===")]
        [Tooltip("Enable analytics tracking")]
        public bool enableAnalytics = true;

        [Tooltip("Analytics event batching size")]
        [Range(1, 100)]
        public int analyticsBatchSize = 10;

        [Header("=== DEBUG ===")]
        [Tooltip("Enable detailed logging")]
        public bool enableDebugLogging = false;

        [Tooltip("Show debug UI overlay")]
        public bool showDebugOverlay = false;

        void OnValidate()
        {
            // Validate configurations exist
            if (!experienceConfig)
                Debug.LogWarning("[PlayerDataConfiguration] Experience configuration missing!");

            if (!currencyConfig)
                Debug.LogWarning("[PlayerDataConfiguration] Currency configuration missing!");

            if (!equipmentConfig)
                Debug.LogWarning("[PlayerDataConfiguration] Equipment configuration missing!");

            if (!craftingConfig)
                Debug.LogWarning("[PlayerDataConfiguration] Crafting configuration missing!");

            // NOTE: Validation for future systems will be re-enabled as they are implemented
            // if (!companionConfig)
            //     Debug.LogWarning("[PlayerDataConfiguration] Companion configuration missing!");

            // if (!villageConfig)
            //     Debug.LogWarning("[PlayerDataConfiguration] Village configuration missing!");

            // if (!ascensionConfig)
            //     Debug.LogWarning("[PlayerDataConfiguration] Ascension configuration missing!");
        }
    }
}