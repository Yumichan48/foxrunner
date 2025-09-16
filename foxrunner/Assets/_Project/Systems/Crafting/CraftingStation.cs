using System;
using UnityEngine;

namespace FoxRunner.Crafting
{
    /// <summary>
    /// Individual crafting station with upgrade mechanics and specializations
    /// Handles station-specific functionality and upgrade progression
    /// </summary>
    [System.Serializable]
    public class CraftingStation
    {
        #region Configuration
        private CraftingStationData stationData;
        private CraftingSystem craftingSystem;
        #endregion

        #region Properties
        public CraftingStationType StationType { get; private set; }
        public string StationName => stationData?.stationName ?? "Unknown Station";
        public string Description => stationData?.description ?? "";
        public int Level { get; private set; } = 1;
        public int MaxLevel => stationData?.maxLevel ?? 50;
        public bool IsUnlocked { get; private set; } = false;
        public bool IsMaxLevel => Level >= MaxLevel;
        #endregion

        #region Events
        public static Action<CraftingStationType, int, int> OnStationLevelUp;
        public static Action<CraftingStationType> OnStationUnlocked;
        #endregion

        #region Constructor
        public CraftingStation(CraftingStationData data, CraftingSystem system)
        {
            stationData = data;
            craftingSystem = system;
            StationType = data.stationType;
            Level = 1;

            // Check if station should be unlocked by default
            CheckInitialUnlock();
        }
        #endregion

        #region Public Methods
        public bool CanUpgrade()
        {
            if (!IsUnlocked || IsMaxLevel) return false;

            var upgradeCost = GetUpgradeCost(Level + 1);
            if (upgradeCost == null) return false;

            // Check currency and material requirements
            return HasUpgradeRequirements(upgradeCost);
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade()) return false;

            var upgradeCost = GetUpgradeCost(Level + 1);
            if (upgradeCost == null) return false;

            // Consume upgrade requirements
            if (!ConsumeUpgradeRequirements(upgradeCost)) return false;

            // Upgrade the station
            int oldLevel = Level;
            Level++;

            OnStationLevelUp?.Invoke(StationType, oldLevel, Level);
            Debug.Log($"[CraftingStation] {StationName} upgraded to level {Level}");

            return true;
        }

        public void Unlock()
        {
            if (IsUnlocked) return;

            IsUnlocked = true;
            OnStationUnlocked?.Invoke(StationType);
            Debug.Log($"[CraftingStation] {StationName} unlocked!");
        }

        public float GetSpeedMultiplier()
        {
            if (!IsUnlocked) return 0f;

            float baseMultiplier = stationData?.baseCraftingSpeedMultiplier ?? 1f;
            float levelBonus = 1f + ((Level - 1) * 0.1f); // 10% bonus per level

            return baseMultiplier * levelBonus;
        }

        public float GetQualityBonus()
        {
            if (!IsUnlocked) return 0f;

            return (Level - 1) * 0.02f; // 2% quality bonus per level above 1
        }

        public StationUpgradeCost GetUpgradeCost(int targetLevel)
        {
            if (stationData?.upgradeCosts == null) return null;

            foreach (var cost in stationData.upgradeCosts)
            {
                if (cost.level == targetLevel)
                    return cost;
            }

            return null;
        }

        public StationUpgradeCost GetCurrentUpgradeCost()
        {
            return GetUpgradeCost(Level + 1);
        }
        #endregion

        #region Private Methods
        private void CheckInitialUnlock()
        {
            // Basic stations (Forge, Workshop) are unlocked by default
            if (StationType == CraftingStationType.Forge || StationType == CraftingStationType.Workshop)
            {
                Unlock();
            }
        }

        private bool HasUpgradeRequirements(StationUpgradeCost upgradeCost)
        {
            if (upgradeCost == null) return false;

            // Check currency requirements
            if (upgradeCost.currencyCosts != null)
            {
                foreach (var currencyCost in upgradeCost.currencyCosts)
                {
                    // Integration with currency system would check here
                    // For now, assume we have enough
                }
            }

            // Check material requirements
            if (upgradeCost.materialRequirements != null)
            {
                foreach (var materialReq in upgradeCost.materialRequirements)
                {
                    if (!craftingSystem.HasMaterial(materialReq.materialId, materialReq.amount))
                        return false;
                }
            }

            return true;
        }

        private bool ConsumeUpgradeRequirements(StationUpgradeCost upgradeCost)
        {
            if (upgradeCost == null) return false;

            // First check if we have everything
            if (!HasUpgradeRequirements(upgradeCost)) return false;

            // Consume currency (integration with currency system would go here)
            if (upgradeCost.currencyCosts != null)
            {
                foreach (var currencyCost in upgradeCost.currencyCosts)
                {
                    // Currency system integration
                    Debug.Log($"[CraftingStation] Would consume {currencyCost.amount} {currencyCost.currencyType}");
                }
            }

            // Consume materials
            if (upgradeCost.materialRequirements != null)
            {
                foreach (var materialReq in upgradeCost.materialRequirements)
                {
                    craftingSystem.RemoveMaterial(materialReq.materialId, materialReq.amount);
                }
            }

            return true;
        }
        #endregion

        #region Utility Methods
        public override string ToString()
        {
            return $"{StationName} (Level {Level}/{MaxLevel}) - {(IsUnlocked ? "Unlocked" : "Locked")}";
        }

        public string GetStatusText()
        {
            if (!IsUnlocked) return "Locked";
            if (IsMaxLevel) return "Max Level";
            return $"Level {Level}";
        }

        public float GetUpgradeProgress()
        {
            return (float)Level / MaxLevel;
        }
        #endregion
    }

    /// <summary>
    /// Helper class for station management and queries
    /// </summary>
    public static class CraftingStationHelper
    {
        public static string GetStationDisplayName(CraftingStationType stationType)
        {
            return stationType switch
            {
                CraftingStationType.Forge => "Blacksmith Forge",
                CraftingStationType.AlchemyLab => "Alchemy Laboratory",
                CraftingStationType.EnchantingTable => "Enchanting Table",
                CraftingStationType.Workshop => "Artisan Workshop",
                CraftingStationType.SacredAltar => "Sacred Altar",
                _ => "Unknown Station"
            };
        }

        public static string GetStationDescription(CraftingStationType stationType)
        {
            return stationType switch
            {
                CraftingStationType.Forge => "Craft weapons, armor, and metal equipment with superior durability and power.",
                CraftingStationType.AlchemyLab => "Brew potions, transmute materials, and create magical compounds.",
                CraftingStationType.EnchantingTable => "Imbue equipment with magical properties and mystical enhancements.",
                CraftingStationType.Workshop => "Create accessories, tools, and mechanical contraptions.",
                CraftingStationType.SacredAltar => "Forge divine artifacts and ultimate equipment blessed by ancient powers.",
                _ => "Unknown crafting station."
            };
        }

        public static Color GetStationColor(CraftingStationType stationType)
        {
            return stationType switch
            {
                CraftingStationType.Forge => new Color(0.8f, 0.4f, 0.2f), // Orange-brown
                CraftingStationType.AlchemyLab => new Color(0.4f, 0.8f, 0.4f), // Green
                CraftingStationType.EnchantingTable => new Color(0.6f, 0.4f, 0.8f), // Purple
                CraftingStationType.Workshop => new Color(0.8f, 0.8f, 0.4f), // Yellow
                CraftingStationType.SacredAltar => new Color(0.9f, 0.9f, 0.9f), // White/gold
                _ => Color.gray
            };
        }

        public static int GetUnlockLevel(CraftingStationType stationType)
        {
            return stationType switch
            {
                CraftingStationType.Forge => 1,
                CraftingStationType.Workshop => 5,
                CraftingStationType.AlchemyLab => 15,
                CraftingStationType.EnchantingTable => 30,
                CraftingStationType.SacredAltar => 50,
                _ => 1
            };
        }

        public static CraftingStationType[] GetStationUnlockOrder()
        {
            return new CraftingStationType[]
            {
                CraftingStationType.Forge,
                CraftingStationType.Workshop,
                CraftingStationType.AlchemyLab,
                CraftingStationType.EnchantingTable,
                CraftingStationType.SacredAltar
            };
        }

        public static bool IsStationPrerequisiteMet(CraftingStationType stationType, CraftingSystem craftingSystem)
        {
            switch (stationType)
            {
                case CraftingStationType.Forge:
                case CraftingStationType.Workshop:
                    return true; // Always available

                case CraftingStationType.AlchemyLab:
                    return craftingSystem.GetMasteryLevel(CraftingStationType.Forge) >= 10;

                case CraftingStationType.EnchantingTable:
                    return craftingSystem.GetMasteryLevel(CraftingStationType.AlchemyLab) >= 15;

                case CraftingStationType.SacredAltar:
                    return craftingSystem.GetMasteryLevel(CraftingStationType.EnchantingTable) >= 20;

                default:
                    return false;
            }
        }
    }
}