using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Currency
{
    /// <summary>
    /// Comprehensive currency management system for Fox Runner
    /// Handles 12 different currency types with conversion, limits, and analytics
    /// Supports both soft and hard currencies with proper monetization integration
    /// </summary>
    public class CurrencySystem : MonoBehaviour
    {
        #region Configuration
        private CurrencyConfiguration config;
        #endregion

        #region Core Data
        private Dictionary<CurrencyType, long> currencies = new Dictionary<CurrencyType, long>();
        private Dictionary<CurrencyType, long> lifetimeEarned = new Dictionary<CurrencyType, long>();
        private Dictionary<CurrencyType, long> lifetimeSpent = new Dictionary<CurrencyType, long>();
        private Dictionary<CurrencyType, long> sessionEarned = new Dictionary<CurrencyType, long>();
        private Dictionary<CurrencyType, long> sessionSpent = new Dictionary<CurrencyType, long>();
        #endregion

        #region Multipliers & Bonuses
        private Dictionary<CurrencyType, float> activeMultipliers = new Dictionary<CurrencyType, float>();
        private Dictionary<CurrencyType, float> temporaryBonuses = new Dictionary<CurrencyType, float>();
        private Dictionary<CurrencyType, DateTime> bonusExpirationTimes = new Dictionary<CurrencyType, DateTime>();
        #endregion

        #region Conversion System
        private Dictionary<(CurrencyType, CurrencyType), float> conversionRates = new Dictionary<(CurrencyType, CurrencyType), float>();
        private Dictionary<CurrencyType, DateTime> lastConversionTimes = new Dictionary<CurrencyType, DateTime>();
        #endregion

        #region Events
        public Action<CurrencyType, long, long> OnCurrencyChanged; // type, old amount, new amount
        public Action<CurrencyType, long, string> OnCurrencyEarned; // type, amount, source
        public Action<CurrencyType, long, string> OnCurrencySpent; // type, amount, reason
        public Action<CurrencyType, float, TimeSpan> OnMultiplierActivated; // type, multiplier, duration
        public Action<CurrencyType, CurrencyType, long, long> OnCurrencyConverted; // from, to, spent, received
        public Action<CurrencyType> OnCurrencyCapReached;
        #endregion

        #region Properties
        public Dictionary<CurrencyType, long> AllCurrencies => new Dictionary<CurrencyType, long>(currencies);
        public Dictionary<CurrencyType, long> LifetimeEarned => new Dictionary<CurrencyType, long>(lifetimeEarned);
        public Dictionary<CurrencyType, long> LifetimeSpent => new Dictionary<CurrencyType, long>(lifetimeSpent);
        public Dictionary<CurrencyType, float> ActiveMultipliers => new Dictionary<CurrencyType, float>(activeMultipliers);
        #endregion

        #region Constructor
        public CurrencySystem(CurrencyConfiguration configuration)
        {
            config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            InitializeCurrencies();
            InitializeConversionRates();
        }
        #endregion

        #region Initialization
        private void InitializeCurrencies()
        {
            // Initialize all currency types with default values
            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                currencies[currency] = GetStartingAmount(currency);
                lifetimeEarned[currency] = 0;
                lifetimeSpent[currency] = 0;
                sessionEarned[currency] = 0;
                sessionSpent[currency] = 0;
                activeMultipliers[currency] = 1.0f;
                temporaryBonuses[currency] = 0.0f;
            }

            Debug.Log("[CurrencySystem] Initialized with 12 currency types");
        }

        private void InitializeConversionRates()
        {
            // Define conversion rates between currencies
            conversionRates[(CurrencyType.Coins, CurrencyType.SpiritPoints)] = 1000f; // 1000 coins = 1 SP
            conversionRates[(CurrencyType.SpiritPoints, CurrencyType.DivinityPoints)] = 100f; // 100 SP = 1 DP
            conversionRates[(CurrencyType.Materials, CurrencyType.Coins)] = 50f; // 1 material = 50 coins

            // Seasonal essence conversions
            conversionRates[(CurrencyType.SpringEssence, CurrencyType.SummerEssence)] = 1f;
            conversionRates[(CurrencyType.SummerEssence, CurrencyType.AutumnEssence)] = 1f;
            conversionRates[(CurrencyType.AutumnEssence, CurrencyType.WinterEssence)] = 1f;
            conversionRates[(CurrencyType.WinterEssence, CurrencyType.SpringEssence)] = 1f;

            // Premium currency conversions
            conversionRates[(CurrencyType.FoxGems, CurrencyType.Coins)] = 0.01f; // 1 gem = 100 coins
            conversionRates[(CurrencyType.FoxGems, CurrencyType.SpiritPoints)] = 0.1f; // 1 gem = 10 SP

            Debug.Log("[CurrencySystem] Conversion rates initialized");
        }

        private long GetStartingAmount(CurrencyType currency)
        {
            return currency switch
            {
                CurrencyType.Coins => 100, // Start with some coins
                CurrencyType.SpiritPoints => 0,
                CurrencyType.Materials => 10, // Start with basic materials
                CurrencyType.VillageTokens => 5, // Start with village tokens
                _ => 0 // All others start at 0
            };
        }
        #endregion

        #region Public API - Currency Management
        public long GetCurrency(CurrencyType type)
        {
            return currencies.GetValueOrDefault(type, 0);
        }

        public bool CanAfford(CurrencyType type, long amount)
        {
            return GetCurrency(type) >= amount;
        }

        public bool CanAfford(Dictionary<CurrencyType, long> costs)
        {
            foreach (var cost in costs)
            {
                if (!CanAfford(cost.Key, cost.Value))
                    return false;
            }
            return true;
        }

        public void EarnCurrency(CurrencyType type, long baseAmount, string source = "Unknown")
        {
            if (baseAmount <= 0) return;

            // Apply multipliers and bonuses
            float totalMultiplier = GetTotalMultiplier(type);
            long finalAmount = (long)(baseAmount * totalMultiplier);

            // Check currency cap
            long currentAmount = GetCurrency(type);
            long maxAmount = GetCurrencyCap(type);

            if (currentAmount >= maxAmount)
            {
                OnCurrencyCapReached?.Invoke(type);
                return;
            }

            if (currentAmount + finalAmount > maxAmount)
            {
                finalAmount = maxAmount - currentAmount;
            }

            // Award currency
            long oldAmount = currencies[type];
            currencies[type] += finalAmount;

            // Track lifetime and session stats
            lifetimeEarned[type] += finalAmount;
            sessionEarned[type] += finalAmount;

            // Fire events
            OnCurrencyChanged?.Invoke(type, oldAmount, currencies[type]);
            OnCurrencyEarned?.Invoke(type, finalAmount, source);

            Debug.Log($"[CurrencySystem] Earned {finalAmount} {type} from {source} (base: {baseAmount}, multiplier: {totalMultiplier:F2})");
        }

        public bool SpendCurrency(CurrencyType type, long amount, string reason = "Unknown")
        {
            if (amount <= 0) return false;

            if (!CanAfford(type, amount))
            {
                Debug.LogWarning($"[CurrencySystem] Cannot afford {amount} {type} (have {GetCurrency(type)})");
                return false;
            }

            // Spend currency
            long oldAmount = currencies[type];
            currencies[type] -= amount;

            // Track lifetime and session stats
            lifetimeSpent[type] += amount;
            sessionSpent[type] += amount;

            // Fire events
            OnCurrencyChanged?.Invoke(type, oldAmount, currencies[type]);
            OnCurrencySpent?.Invoke(type, amount, reason);

            Debug.Log($"[CurrencySystem] Spent {amount} {type} for {reason}");
            return true;
        }

        public bool SpendCurrency(Dictionary<CurrencyType, long> costs, string reason = "Unknown")
        {
            if (!CanAfford(costs))
                return false;

            foreach (var cost in costs)
            {
                SpendCurrency(cost.Key, cost.Value, reason);
            }

            return true;
        }

        public void SetCurrency(CurrencyType type, long amount, bool trackAsEarned = false)
        {
            long oldAmount = currencies[type];
            currencies[type] = Math.Max(0, Math.Min(amount, GetCurrencyCap(type)));

            if (trackAsEarned && currencies[type] > oldAmount)
            {
                long gained = currencies[type] - oldAmount;
                lifetimeEarned[type] += gained;
                sessionEarned[type] += gained;
            }

            OnCurrencyChanged?.Invoke(type, oldAmount, currencies[type]);
        }
        #endregion

        #region Multipliers & Bonuses
        public void SetMultiplier(CurrencyType type, float multiplier, string source = "Unknown")
        {
            activeMultipliers[type] = Math.Max(0.1f, multiplier);
            Debug.Log($"[CurrencySystem] {type} multiplier set to {multiplier:F2} from {source}");
        }

        public void AddTemporaryBonus(CurrencyType type, float bonus, TimeSpan duration, string source = "Unknown")
        {
            temporaryBonuses[type] = Math.Max(temporaryBonuses[type], bonus);
            bonusExpirationTimes[type] = DateTime.Now + duration;

            OnMultiplierActivated?.Invoke(type, bonus, duration);
            Debug.Log($"[CurrencySystem] Added {bonus:F2}x temporary bonus to {type} for {duration.TotalMinutes:F1} minutes from {source}");
        }

        public float GetTotalMultiplier(CurrencyType type)
        {
            float baseMultiplier = activeMultipliers.GetValueOrDefault(type, 1.0f);
            float temporaryBonus = GetActiveTemporaryBonus(type);
            float configMultiplier = config?.GetCurrencyMultiplier(type) ?? 1.0f;

            return baseMultiplier * (1.0f + temporaryBonus) * configMultiplier;
        }

        private float GetActiveTemporaryBonus(CurrencyType type)
        {
            if (!temporaryBonuses.ContainsKey(type) || !bonusExpirationTimes.ContainsKey(type))
                return 0f;

            if (DateTime.Now > bonusExpirationTimes[type])
            {
                temporaryBonuses[type] = 0f;
                return 0f;
            }

            return temporaryBonuses[type];
        }
        #endregion

        #region Currency Conversion
        public bool ConvertCurrency(CurrencyType fromType, CurrencyType toType, long fromAmount)
        {
            if (!CanConvert(fromType, toType, fromAmount))
                return false;

            float conversionRate = GetConversionRate(fromType, toType);
            long toAmount = (long)(fromAmount * conversionRate);

            // Check conversion cooldown
            if (HasConversionCooldown(fromType))
            {
                Debug.LogWarning($"[CurrencySystem] Conversion from {fromType} on cooldown");
                return false;
            }

            // Perform conversion
            if (!SpendCurrency(fromType, fromAmount, $"Conversion to {toType}"))
                return false;

            EarnCurrency(toType, toAmount, $"Conversion from {fromType}");

            // Set cooldown
            lastConversionTimes[fromType] = DateTime.Now;

            OnCurrencyConverted?.Invoke(fromType, toType, fromAmount, toAmount);
            Debug.Log($"[CurrencySystem] Converted {fromAmount} {fromType} to {toAmount} {toType}");
            return true;
        }

        public bool CanConvert(CurrencyType fromType, CurrencyType toType, long amount)
        {
            return CanAfford(fromType, amount) &&
                   conversionRates.ContainsKey((fromType, toType)) &&
                   !HasConversionCooldown(fromType);
        }

        public float GetConversionRate(CurrencyType fromType, CurrencyType toType)
        {
            return conversionRates.GetValueOrDefault((fromType, toType), 0f);
        }

        public long GetConversionResult(CurrencyType fromType, CurrencyType toType, long fromAmount)
        {
            float rate = GetConversionRate(fromType, toType);
            return (long)(fromAmount * rate);
        }

        private bool HasConversionCooldown(CurrencyType type)
        {
            if (!lastConversionTimes.ContainsKey(type))
                return false;

            TimeSpan cooldown = config?.GetConversionCooldown(type) ?? TimeSpan.Zero;
            return DateTime.Now - lastConversionTimes[type] < cooldown;
        }
        #endregion

        #region Currency Caps & Limits
        public long GetCurrencyCap(CurrencyType type)
        {
            return config?.GetCurrencyCap(type) ?? GetDefaultCap(type);
        }

        private long GetDefaultCap(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.Coins => 999_999_999L,
                CurrencyType.SpiritPoints => 99_999_999L,
                CurrencyType.Materials => 999_999L,
                CurrencyType.FoxGems => 99_999L,
                CurrencyType.DivinityPoints => 9_999_999L,
                CurrencyType.UltraSpiritPoints => 999_999L,
                CurrencyType.VillageTokens => 999_999L,
                CurrencyType.CompanionBonds => 999_999L,
                _ => 999_999L // Seasonal essences and others
            };
        }

        public float GetCapUtilization(CurrencyType type)
        {
            long current = GetCurrency(type);
            long cap = GetCurrencyCap(type);
            return cap > 0 ? (float)current / cap : 0f;
        }

        public bool IsNearCap(CurrencyType type, float threshold = 0.9f)
        {
            return GetCapUtilization(type) >= threshold;
        }
        #endregion

        #region Analytics & Statistics
        public CurrencyStats GetCurrencyStats(CurrencyType type)
        {
            return new CurrencyStats
            {
                currencyType = type,
                currentAmount = GetCurrency(type),
                lifetimeEarned = lifetimeEarned.GetValueOrDefault(type, 0),
                lifetimeSpent = lifetimeSpent.GetValueOrDefault(type, 0),
                sessionEarned = sessionEarned.GetValueOrDefault(type, 0),
                sessionSpent = sessionSpent.GetValueOrDefault(type, 0),
                currentMultiplier = GetTotalMultiplier(type),
                capUtilization = GetCapUtilization(type),
                isNearCap = IsNearCap(type)
            };
        }

        public Dictionary<CurrencyType, CurrencyStats> GetAllCurrencyStats()
        {
            var stats = new Dictionary<CurrencyType, CurrencyStats>();
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            {
                stats[type] = GetCurrencyStats(type);
            }
            return stats;
        }

        public long GetNetWorth()
        {
            long netWorth = 0;

            foreach (var currency in currencies)
            {
                // Convert everything to coin equivalent for net worth calculation
                float conversionRate = GetConversionRate(currency.Key, CurrencyType.Coins);
                if (conversionRate > 0)
                {
                    netWorth += (long)(currency.Value / conversionRate);
                }
                else if (currency.Key == CurrencyType.Coins)
                {
                    netWorth += currency.Value;
                }
            }

            return netWorth;
        }

        public void ResetSessionStats()
        {
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            {
                sessionEarned[type] = 0;
                sessionSpent[type] = 0;
            }
        }
        #endregion

        #region Save/Load
        public CurrencySaveData GetSaveData()
        {
            return new CurrencySaveData
            {
                currencies = new Dictionary<CurrencyType, long>(currencies),
                lifetimeEarned = new Dictionary<CurrencyType, long>(lifetimeEarned),
                lifetimeSpent = new Dictionary<CurrencyType, long>(lifetimeSpent)
            };
        }

        public void LoadFromSaveData(CurrencySaveData saveData)
        {
            if (saveData == null)
            {
                InitializeCurrencies();
                return;
            }

            currencies = saveData.currencies ?? new Dictionary<CurrencyType, long>();
            lifetimeEarned = saveData.lifetimeEarned ?? new Dictionary<CurrencyType, long>();
            lifetimeSpent = saveData.lifetimeSpent ?? new Dictionary<CurrencyType, long>();

            // Ensure all currency types are represented
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            {
                if (!currencies.ContainsKey(type))
                    currencies[type] = GetStartingAmount(type);
                if (!lifetimeEarned.ContainsKey(type))
                    lifetimeEarned[type] = 0;
                if (!lifetimeSpent.ContainsKey(type))
                    lifetimeSpent[type] = 0;
            }
        }
        #endregion

        #region Update Loop
        public void Update()
        {
            UpdateTemporaryBonuses();
        }

        private void UpdateTemporaryBonuses()
        {
            var expiredBonuses = new List<CurrencyType>();

            foreach (var bonus in temporaryBonuses)
            {
                if (bonus.Value > 0 && bonusExpirationTimes.ContainsKey(bonus.Key))
                {
                    if (DateTime.Now > bonusExpirationTimes[bonus.Key])
                    {
                        expiredBonuses.Add(bonus.Key);
                    }
                }
            }

            foreach (var type in expiredBonuses)
            {
                temporaryBonuses[type] = 0f;
                Debug.Log($"[CurrencySystem] Temporary bonus for {type} expired");
            }
        }
        #endregion
    }

    #region Supporting Data Structures
    [Serializable]
    public class CurrencyStats
    {
        public CurrencyType currencyType;
        public long currentAmount;
        public long lifetimeEarned;
        public long lifetimeSpent;
        public long sessionEarned;
        public long sessionSpent;
        public float currentMultiplier;
        public float capUtilization;
        public bool isNearCap;
    }
    #endregion
}