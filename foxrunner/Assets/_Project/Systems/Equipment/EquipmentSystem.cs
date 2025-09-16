using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Equipment
{
    /// <summary>
    /// Comprehensive equipment system for Fox Runner
    /// Manages 7 equipment slots with 6 tiers each (42 total equipment pieces)
    /// Handles stat bonuses, upgrades, special effects, and equipment combinations
    /// </summary>
    public class EquipmentSystem : MonoBehaviour
    {
        #region Configuration
        private EquipmentConfiguration config;
        #endregion

        #region Core Data
        private Dictionary<EquipmentSlot, EquipmentData> equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
        private List<EquipmentData> inventory = new List<EquipmentData>();
        private Dictionary<string, int> upgradeProgress = new Dictionary<string, int>();
        private Dictionary<StatType, float> cachedStatBonuses = new Dictionary<StatType, float>();
        private bool statsCacheDirty = true;
        #endregion

        #region Equipment Database
        private Dictionary<string, EquipmentData> equipmentDatabase = new Dictionary<string, EquipmentData>();
        private Dictionary<EquipmentSlot, List<EquipmentData>> equipmentBySlot = new Dictionary<EquipmentSlot, List<EquipmentData>>();
        #endregion

        #region Events
        public Action<EquipmentSlot, EquipmentData> OnEquipmentChanged;
        public Action<EquipmentData, int> OnEquipmentUpgraded; // equipment, new level
        public Action<EquipmentData> OnEquipmentAcquired;
        public Action<Dictionary<StatType, float>> OnStatsRecalculated;
        public Action<string> OnSetBonusActivated;
        #endregion

        #region Properties
        public Dictionary<EquipmentSlot, EquipmentData> EquippedItems => new Dictionary<EquipmentSlot, EquipmentData>(equippedItems);
        public List<EquipmentData> Inventory => new List<EquipmentData>(inventory);
        public Dictionary<StatType, float> TotalStatBonuses => GetTotalStatBonuses();
        public List<string> ActiveSetBonuses => GetActiveSetBonuses();
        #endregion

        #region Constructor
        public EquipmentSystem(EquipmentConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            InitializeEquipmentDatabase();
            InitializeEquippedItems();
        }
        #endregion

        #region Initialization
        private void InitializeEquipmentDatabase()
        {
            // Generate all 42 equipment pieces (7 slots × 6 tiers)
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equipmentBySlot[slot] = new List<EquipmentData>();

                foreach (EquipmentTier tier in Enum.GetValues(typeof(EquipmentTier)))
                {
                    EquipmentData equipment = GenerateEquipmentData(slot, tier);
                    equipmentDatabase[equipment.id] = equipment;
                    equipmentBySlot[slot].Add(equipment);
                }
            }

            Debug.Log($"[EquipmentSystem] Initialized equipment database with {equipmentDatabase.Count} items");
        }

        private void InitializeEquippedItems()
        {
            // Initialize all slots as empty
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                equippedItems[slot] = null;
            }
        }

        private EquipmentData GenerateEquipmentData(EquipmentSlot slot, EquipmentTier tier)
        {
            string equipmentId = $"{slot}_{tier}";

            EquipmentData equipment = new EquipmentData
            {
                id = equipmentId,
                name = GetEquipmentName(slot, tier),
                description = GetEquipmentDescription(slot, tier),
                slot = slot,
                tier = tier,
                level = 1,
                baseStats = GenerateBaseStats(slot, tier),
                bonusStats = new Dictionary<StatType, float>(),
                specialEffects = GenerateSpecialEffects(slot, tier),
                isEquipped = false,
                acquiredDate = DateTime.MinValue,
                upgradeCount = 0
            };

            return equipment;
        }
        #endregion

        #region Equipment Generation
        private string GetEquipmentName(EquipmentSlot slot, EquipmentTier tier)
        {
            Dictionary<EquipmentSlot, string[]> slotNames = new Dictionary<EquipmentSlot, string[]>
            {
                [EquipmentSlot.TailBrush] = new[] { "Basic Brush", "Swift Brush", "Enchanted Brush", "Mystic Brush", "Legendary Brush", "Eternal Comet Brush" },
                [EquipmentSlot.PawGuards] = new[] { "Leather Guards", "Iron Guards", "Steel Guards", "Mithril Guards", "Dragon Guards", "Eternal Sky Guards" },
                [EquipmentSlot.MysticCollar] = new[] { "Simple Collar", "Warded Collar", "Runic Collar", "Arcane Collar", "Divine Collar", "Eternal Void Collar" },
                [EquipmentSlot.SeasonalScarf] = new[] { "Cotton Scarf", "Silk Scarf", "Magical Scarf", "Elemental Scarf", "Cosmic Scarf", "Eternal Seasons Scarf" },
                [EquipmentSlot.SpiritBell] = new[] { "Tin Bell", "Silver Bell", "Crystal Bell", "Astral Bell", "Celestial Bell", "Eternal Harmony Bell" },
                [EquipmentSlot.AncientMask] = new[] { "Wooden Mask", "Bone Mask", "Jade Mask", "Obsidian Mask", "Starlight Mask", "Eternal Wisdom Mask" },
                [EquipmentSlot.CompanionCharm] = new[] { "Friendship Charm", "Bond Charm", "Unity Charm", "Harmony Charm", "Soul Charm", "Eternal Bond Charm" }
            };

            return slotNames[slot][(int)tier];
        }

        private string GetEquipmentDescription(EquipmentSlot slot, EquipmentTier tier)
        {
            Dictionary<EquipmentSlot, string> baseDescriptions = new Dictionary<EquipmentSlot, string>
            {
                [EquipmentSlot.TailBrush] = "Enhances movement speed and creates beautiful trail effects while running.",
                [EquipmentSlot.PawGuards] = "Protects paws and improves jumping ability and landing stability.",
                [EquipmentSlot.MysticCollar] = "Channels mystical energies to enhance magical resistance and mana flow.",
                [EquipmentSlot.SeasonalScarf] = "Adapts to seasonal changes, providing weather resistance and elemental bonuses.",
                [EquipmentSlot.SpiritBell] = "Resonates with spiritual energy, increasing spirit point generation and collection radius.",
                [EquipmentSlot.AncientMask] = "Contains ancient wisdom, boosting experience gain and unlocking hidden knowledge.",
                [EquipmentSlot.CompanionCharm] = "Strengthens the bond with companions, improving their effectiveness and loyalty."
            };

            string tierSuffix = tier switch
            {
                EquipmentTier.Common => " (Basic quality)",
                EquipmentTier.Uncommon => " (Improved effectiveness)",
                EquipmentTier.Rare => " (Significantly enhanced)",
                EquipmentTier.Epic => " (Powerful bonuses)",
                EquipmentTier.Legendary => " (Exceptional power)",
                EquipmentTier.Eternal => " (Ultimate perfection)",
                _ => ""
            };

            return baseDescriptions[slot] + tierSuffix;
        }

        private Dictionary<StatType, float> GenerateBaseStats(EquipmentSlot slot, EquipmentTier tier)
        {
            var stats = new Dictionary<StatType, float>();
            float tierMultiplier = GetTierMultiplier(tier);

            // Each slot provides different primary stats
            switch (slot)
            {
                case EquipmentSlot.TailBrush:
                    stats[StatType.MovementSpeed] = 0.05f * tierMultiplier;
                    stats[StatType.DashDistance] = 0.1f * tierMultiplier;
                    break;

                case EquipmentSlot.PawGuards:
                    stats[StatType.JumpHeight] = 0.08f * tierMultiplier;
                    stats[StatType.Health] = 10f * tierMultiplier;
                    break;

                case EquipmentSlot.MysticCollar:
                    stats[StatType.Mana] = 15f * tierMultiplier;
                    stats[StatType.DamageReduction] = 0.03f * tierMultiplier;
                    break;

                case EquipmentSlot.SeasonalScarf:
                    stats[StatType.SeasonalBonus] = 0.1f * tierMultiplier;
                    stats[StatType.DamageReduction] = 0.02f * tierMultiplier;
                    break;

                case EquipmentSlot.SpiritBell:
                    stats[StatType.SpiritPointMultiplier] = 0.05f * tierMultiplier;
                    stats[StatType.CollectionRadius] = 0.2f * tierMultiplier;
                    break;

                case EquipmentSlot.AncientMask:
                    stats[StatType.ExperienceMultiplier] = 0.1f * tierMultiplier;
                    stats[StatType.CriticalChance] = 0.02f * tierMultiplier;
                    break;

                case EquipmentSlot.CompanionCharm:
                    stats[StatType.CoinMultiplier] = 0.05f * tierMultiplier;
                    stats[StatType.ExperienceMultiplier] = 0.03f * tierMultiplier;
                    break;
            }

            return stats;
        }

        private List<string> GenerateSpecialEffects(EquipmentSlot slot, EquipmentTier tier)
        {
            var effects = new List<string>();

            // Higher tiers get more special effects
            if (tier >= EquipmentTier.Rare)
            {
                switch (slot)
                {
                    case EquipmentSlot.TailBrush:
                        effects.Add("Leaves shimmering trail particles");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Trail damages enemies");
                        break;

                    case EquipmentSlot.PawGuards:
                        effects.Add("Reduces fall damage");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Ground slam ability");
                        break;

                    case EquipmentSlot.MysticCollar:
                        effects.Add("Mana regeneration boost");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Magical shield activation");
                        break;

                    case EquipmentSlot.SeasonalScarf:
                        effects.Add("Adapts to current season");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Weather immunity");
                        break;

                    case EquipmentSlot.SpiritBell:
                        effects.Add("Attracts nearby collectibles");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Spirit point aura");
                        break;

                    case EquipmentSlot.AncientMask:
                        effects.Add("Reveals hidden secrets");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Wisdom burst ability");
                        break;

                    case EquipmentSlot.CompanionCharm:
                        effects.Add("Companion bond boost");
                        if (tier >= EquipmentTier.Legendary) effects.Add("Companion sync abilities");
                        break;
                }
            }

            return effects;
        }

        private float GetTierMultiplier(EquipmentTier tier)
        {
            return tier switch
            {
                EquipmentTier.Common => 1.0f,
                EquipmentTier.Uncommon => 1.5f,
                EquipmentTier.Rare => 2.2f,
                EquipmentTier.Epic => 3.0f,
                EquipmentTier.Legendary => 4.5f,
                EquipmentTier.Eternal => 6.0f,
                _ => 1.0f
            };
        }
        #endregion

        #region Public API - Equipment Management
        public bool EquipItem(string equipmentId)
        {
            if (!equipmentDatabase.TryGetValue(equipmentId, out EquipmentData equipment))
            {
                Debug.LogWarning($"[EquipmentSystem] Equipment not found: {equipmentId}");
                return false;
            }

            return EquipItem(equipment);
        }

        public bool EquipItem(EquipmentData equipment)
        {
            if (equipment == null) return false;

            // Unequip current item in the slot
            EquipmentData currentItem = equippedItems[equipment.slot];
            if (currentItem != null)
            {
                currentItem.isEquipped = false;
            }

            // Equip new item
            equipment.isEquipped = true;
            equippedItems[equipment.slot] = equipment;
            statsCacheDirty = true;

            OnEquipmentChanged?.Invoke(equipment.slot, equipment);
            RecalculateStats();

            Debug.Log($"[EquipmentSystem] Equipped {equipment.name} in {equipment.slot} slot");
            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.TryGetValue(slot, out EquipmentData equipment) || equipment == null)
            {
                return false;
            }

            equipment.isEquipped = false;
            equippedItems[slot] = null;
            statsCacheDirty = true;

            OnEquipmentChanged?.Invoke(slot, null);
            RecalculateStats();

            Debug.Log($"[EquipmentSystem] Unequipped item from {slot} slot");
            return true;
        }

        public EquipmentData GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.GetValueOrDefault(slot);
        }

        public Dictionary<EquipmentSlot, EquipmentData> GetAllEquippedItems()
        {
            return new Dictionary<EquipmentSlot, EquipmentData>(equippedItems);
        }
        #endregion

        #region Public API - Equipment Acquisition
        public bool AcquireEquipment(string equipmentId)
        {
            if (!equipmentDatabase.TryGetValue(equipmentId, out EquipmentData equipmentTemplate))
            {
                Debug.LogWarning($"[EquipmentSystem] Equipment template not found: {equipmentId}");
                return false;
            }

            // Create a copy for the player's inventory
            EquipmentData playerEquipment = CloneEquipment(equipmentTemplate);
            playerEquipment.acquiredDate = DateTime.Now;

            inventory.Add(playerEquipment);
            OnEquipmentAcquired?.Invoke(playerEquipment);

            Debug.Log($"[EquipmentSystem] Acquired {playerEquipment.name}");
            return true;
        }

        public bool AcquireEquipment(EquipmentSlot slot, EquipmentTier tier)
        {
            string equipmentId = $"{slot}_{tier}";
            return AcquireEquipment(equipmentId);
        }

        private EquipmentData CloneEquipment(EquipmentData original)
        {
            return new EquipmentData
            {
                id = original.id,
                name = original.name,
                description = original.description,
                slot = original.slot,
                tier = original.tier,
                level = original.level,
                baseStats = new Dictionary<StatType, float>(original.baseStats),
                bonusStats = new Dictionary<StatType, float>(original.bonusStats),
                specialEffects = new List<string>(original.specialEffects),
                isEquipped = false,
                acquiredDate = DateTime.Now,
                upgradeCount = 0
            };
        }
        #endregion

        #region Equipment Upgrading
        public bool UpgradeEquipment(EquipmentData equipment, int levels = 1)
        {
            if (equipment == null || levels <= 0) return false;

            // Check upgrade costs and requirements
            UpgradeCost cost = CalculateUpgradeCost(equipment, levels);
            if (!CanAffordUpgrade(cost))
            {
                Debug.LogWarning($"[EquipmentSystem] Cannot afford upgrade for {equipment.name}");
                return false;
            }

            // Apply upgrade
            int oldLevel = equipment.level;
            equipment.level += levels;
            equipment.upgradeCount += levels;

            // Add bonus stats for each level
            for (int i = 0; i < levels; i++)
            {
                AddUpgradeBonuses(equipment);
            }

            statsCacheDirty = true;
            OnEquipmentUpgraded?.Invoke(equipment, equipment.level);

            if (equipment.isEquipped)
            {
                RecalculateStats();
            }

            Debug.Log($"[EquipmentSystem] Upgraded {equipment.name} from level {oldLevel} to {equipment.level}");
            return true;
        }

        private UpgradeCost CalculateUpgradeCost(EquipmentData equipment, int levels)
        {
            float tierCostMultiplier = GetTierMultiplier(equipment.tier);
            long baseCost = 100 * (equipment.level + 1);

            return new UpgradeCost
            {
                coins = (long)(baseCost * tierCostMultiplier * levels),
                materials = (int)(10 * tierCostMultiplier * levels),
                spiritPoints = equipment.tier >= EquipmentTier.Epic ? levels : 0
            };
        }

        private bool CanAffordUpgrade(UpgradeCost cost)
        {
            // This would check against the currency system
            // For now, assume player can afford
            return true;
        }

        private void AddUpgradeBonuses(EquipmentData equipment)
        {
            float bonusMultiplier = 0.1f; // 10% bonus per level

            foreach (var baseStat in equipment.baseStats)
            {
                StatType statType = baseStat.Key;
                float baseValue = baseStat.Value;
                float bonus = baseValue * bonusMultiplier;

                if (!equipment.bonusStats.ContainsKey(statType))
                {
                    equipment.bonusStats[statType] = 0f;
                }

                equipment.bonusStats[statType] += bonus;
            }
        }
        #endregion

        #region Stat Calculations
        public Dictionary<StatType, float> GetTotalStatBonuses()
        {
            if (statsCacheDirty)
            {
                RecalculateStats();
            }

            return new Dictionary<StatType, float>(cachedStatBonuses);
        }

        private void RecalculateStats()
        {
            cachedStatBonuses.Clear();

            // Sum stats from all equipped items
            foreach (var equippedItem in equippedItems.Values)
            {
                if (equippedItem == null) continue;

                // Add base stats
                foreach (var stat in equippedItem.baseStats)
                {
                    cachedStatBonuses[stat.Key] = cachedStatBonuses.GetValueOrDefault(stat.Key, 0f) + stat.Value;
                }

                // Add bonus stats from upgrades
                foreach (var stat in equippedItem.bonusStats)
                {
                    cachedStatBonuses[stat.Key] = cachedStatBonuses.GetValueOrDefault(stat.Key, 0f) + stat.Value;
                }
            }

            // Apply set bonuses
            ApplySetBonuses();

            statsCacheDirty = false;
            OnStatsRecalculated?.Invoke(cachedStatBonuses);
        }

        private void ApplySetBonuses()
        {
            List<string> activeSets = GetActiveSetBonuses();

            foreach (string setBonus in activeSets)
            {
                ApplySetBonus(setBonus);
                OnSetBonusActivated?.Invoke(setBonus);
            }
        }

        private List<string> GetActiveSetBonuses()
        {
            var setBonuses = new List<string>();

            // Check for tier-based set bonuses
            var tierCounts = new Dictionary<EquipmentTier, int>();

            foreach (var equippedItem in equippedItems.Values)
            {
                if (equippedItem != null)
                {
                    tierCounts[equippedItem.tier] = tierCounts.GetValueOrDefault(equippedItem.tier, 0) + 1;
                }
            }

            // Activate set bonuses based on equipped items of same tier
            foreach (var tierCount in tierCounts)
            {
                if (tierCount.Value >= 3) // 3-piece set bonus
                {
                    setBonuses.Add($"{tierCount.Key}_3Set");
                }
                if (tierCount.Value >= 5) // 5-piece set bonus
                {
                    setBonuses.Add($"{tierCount.Key}_5Set");
                }
                if (tierCount.Value >= 7) // Full set bonus
                {
                    setBonuses.Add($"{tierCount.Key}_FullSet");
                }
            }

            return setBonuses;
        }

        private void ApplySetBonus(string setBonusId)
        {
            // Apply bonuses based on set bonus type
            if (setBonusId.Contains("3Set"))
            {
                cachedStatBonuses[StatType.ExperienceMultiplier] = cachedStatBonuses.GetValueOrDefault(StatType.ExperienceMultiplier, 0f) + 0.1f;
            }
            else if (setBonusId.Contains("5Set"))
            {
                cachedStatBonuses[StatType.CoinMultiplier] = cachedStatBonuses.GetValueOrDefault(StatType.CoinMultiplier, 0f) + 0.15f;
            }
            else if (setBonusId.Contains("FullSet"))
            {
                cachedStatBonuses[StatType.SpiritPointMultiplier] = cachedStatBonuses.GetValueOrDefault(StatType.SpiritPointMultiplier, 0f) + 0.25f;
            }
        }
        #endregion

        #region Equipment Database Queries
        public List<EquipmentData> GetAvailableEquipmentForSlot(EquipmentSlot slot)
        {
            return equipmentBySlot.GetValueOrDefault(slot, new List<EquipmentData>());
        }

        public List<EquipmentData> GetEquipmentByTier(EquipmentTier tier)
        {
            return equipmentDatabase.Values.Where(e => e.tier == tier).ToList();
        }

        public EquipmentData GetEquipmentById(string id)
        {
            return equipmentDatabase.GetValueOrDefault(id);
        }

        public List<EquipmentData> GetOwnedEquipment()
        {
            return new List<EquipmentData>(inventory);
        }

        public List<EquipmentData> GetOwnedEquipmentForSlot(EquipmentSlot slot)
        {
            return inventory.Where(e => e.slot == slot).ToList();
        }
        #endregion

        #region Save/Load
        public EquipmentSaveData GetSaveData()
        {
            return new EquipmentSaveData
            {
                equippedItems = new Dictionary<EquipmentSlot, EquipmentData>(equippedItems),
                inventory = new List<EquipmentData>(inventory),
                upgradeProgress = new Dictionary<string, int>(upgradeProgress)
            };
        }

        public void LoadFromSaveData(EquipmentSaveData saveData)
        {
            if (saveData == null)
            {
                // Initialize with defaults
                equippedItems.Clear();
                inventory.Clear();
                upgradeProgress.Clear();
                InitializeEquippedItems();
                return;
            }

            equippedItems = saveData.equippedItems ?? new Dictionary<EquipmentSlot, EquipmentData>();
            inventory = saveData.inventory ?? new List<EquipmentData>();
            upgradeProgress = saveData.upgradeProgress ?? new Dictionary<string, int>();

            statsCacheDirty = true;
        }
        #endregion

        #region Update Loop
        public void Update()
        {
            // Equipment system is mostly event-driven
            // Could add time-based effects here if needed
        }
        #endregion
    }

    #region Supporting Data Structures
    [Serializable]
    public class UpgradeCost
    {
        public long coins;
        public int materials;
        public int spiritPoints;
    }
    #endregion
}