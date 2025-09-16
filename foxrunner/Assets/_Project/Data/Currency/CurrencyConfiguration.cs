using System;
using UnityEngine;
using FoxRunner.Data;

namespace FoxRunner.Currency
{
    /// <summary>
    /// Configuration for the currency system
    /// Defines currency caps, multipliers, conversion rates, and economic balance
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyConfig", menuName = "FoxRunner/Currency/Currency Configuration")]
    public class CurrencyConfiguration : ScriptableObject
    {
        [Header("=== CURRENCY CAPS ===")]
        [Tooltip("Maximum amounts for each currency type")]
        public CurrencyCapConfiguration[] currencyCaps;

        [Header("=== BASE MULTIPLIERS ===")]
        [Tooltip("Base earning multipliers for each currency")]
        public CurrencyMultiplierConfiguration[] baseMultipliers;

        [Header("=== CONVERSION RATES ===")]
        [Tooltip("Currency conversion rate configurations")]
        public ConversionRateConfiguration[] conversionRates;

        [Header("=== CONVERSION COOLDOWNS ===")]
        [Tooltip("Cooldown times for currency conversions")]
        public ConversionCooldownConfiguration[] conversionCooldowns;

        [Header("=== EARNING SOURCES ===")]
        [Tooltip("Configurations for different earning sources")]
        public EarningSourceConfiguration[] earningSources;

        [Header("=== PREMIUM CURRENCY ===")]
        [Tooltip("Fox Gems (premium currency) configuration")]
        public PremiumCurrencyConfiguration premiumConfig;

        [Header("=== ECONOMIC BALANCE ===")]
        [Tooltip("Overall economic balance settings")]
        public EconomicBalanceConfiguration economicBalance;

        [Header("=== SEASONAL ECONOMY ===")]
        [Tooltip("Seasonal essence economy settings")]
        public SeasonalEconomyConfiguration seasonalEconomy;

        [Header("=== MONETIZATION ===")]
        [Tooltip("Monetization and IAP integration settings")]
        public MonetizationConfiguration monetization;

        [Header("=== DEBUG ===")]
        [Tooltip("Debug and testing configurations")]
        public bool enableDebugMode = false;
        public bool unlimitedCurrency = false;
        public float debugMultiplier = 1f;

        void OnValidate()
        {
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            // Validate currency caps
            if (currencyCaps == null || currencyCaps.Length == 0)
            {
                Debug.LogWarning("[CurrencyConfiguration] Currency caps not configured!");
            }

            // Validate conversion rates
            if (conversionRates != null)
            {
                foreach (var rate in conversionRates)
                {
                    if (rate.rate <= 0)
                    {
                        Debug.LogWarning($"[CurrencyConfiguration] Invalid conversion rate: {rate.fromCurrency} to {rate.toCurrency}");
                    }
                }
            }

            // Validate premium currency config
            if (premiumConfig != null && premiumConfig.gemToCoinRate <= 0)
            {
                Debug.LogWarning("[CurrencyConfiguration] Invalid gem to coin conversion rate!");
            }
        }

        public long GetCurrencyCap(CurrencyType type)
        {
            if (unlimitedCurrency) return long.MaxValue;

            if (currencyCaps != null)
            {
                foreach (var cap in currencyCaps)
                {
                    if (cap.currencyType == type)
                        return cap.maxAmount;
                }
            }

            return GetDefaultCap(type);
        }

        public float GetCurrencyMultiplier(CurrencyType type)
        {
            float multiplier = 1f;

            if (baseMultipliers != null)
            {
                foreach (var mult in baseMultipliers)
                {
                    if (mult.currencyType == type)
                    {
                        multiplier = mult.multiplier;
                        break;
                    }
                }
            }

            return multiplier * debugMultiplier;
        }

        public TimeSpan GetConversionCooldown(CurrencyType type)
        {
            if (conversionCooldowns != null)
            {
                foreach (var cooldown in conversionCooldowns)
                {
                    if (cooldown.currencyType == type)
                        return TimeSpan.FromSeconds(cooldown.cooldownSeconds);
                }
            }

            return TimeSpan.Zero;
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
                _ => 999_999L
            };
        }
    }

    [System.Serializable]
    public class CurrencyCapConfiguration
    {
        [Tooltip("Currency type")]
        public CurrencyType currencyType;

        [Tooltip("Maximum amount that can be held")]
        public long maxAmount = 999999999;

        [Tooltip("Warning threshold (percentage of cap)")]
        [Range(0f, 1f)]
        public float warningThreshold = 0.9f;

        [Tooltip("Cap increase per player level")]
        public long capIncreasePerLevel = 0;
    }

    [System.Serializable]
    public class CurrencyMultiplierConfiguration
    {
        [Tooltip("Currency type")]
        public CurrencyType currencyType;

        [Tooltip("Base earning multiplier")]
        [Range(0.1f, 10f)]
        public float multiplier = 1f;

        [Tooltip("Description of this multiplier")]
        public string description;
    }

    [System.Serializable]
    public class ConversionRateConfiguration
    {
        [Tooltip("Source currency")]
        public CurrencyType fromCurrency;

        [Tooltip("Target currency")]
        public CurrencyType toCurrency;

        [Tooltip("Conversion rate (how much target currency per source currency)")]
        public float rate = 1f;

        [Tooltip("Minimum conversion amount")]
        public long minimumAmount = 1;

        [Tooltip("Conversion fee percentage")]
        [Range(0f, 0.5f)]
        public float feePercentage = 0f;

        [Tooltip("Is this conversion available to players?")]
        public bool isAvailable = true;
    }

    [System.Serializable]
    public class ConversionCooldownConfiguration
    {
        [Tooltip("Currency type")]
        public CurrencyType currencyType;

        [Tooltip("Cooldown time in seconds")]
        public float cooldownSeconds = 0f;

        [Tooltip("Cooldown applies to all conversions from this currency")]
        public bool appliesToAllConversions = true;
    }

    [System.Serializable]
    public class EarningSourceConfiguration
    {
        [Tooltip("Source name (e.g., 'Fruit Collection', 'Quest Completion')")]
        public string sourceName;

        [Tooltip("Currency earned")]
        public CurrencyType currencyType;

        [Tooltip("Base amount earned")]
        public long baseAmount = 1;

        [Tooltip("Level scaling factor")]
        [Range(0f, 2f)]
        public float levelScaling = 0f;

        [Tooltip("Randomization range (±percentage)")]
        [Range(0f, 0.5f)]
        public float randomization = 0f;

        [Tooltip("Minimum player level required")]
        public int requiredLevel = 1;
    }

    [System.Serializable]
    public class PremiumCurrencyConfiguration
    {
        [Header("Fox Gems Configuration")]
        [Tooltip("Conversion rate: 1 Gem = X Coins")]
        public float gemToCoinRate = 100f;

        [Tooltip("Conversion rate: 1 Gem = X Spirit Points")]
        public float gemToSpiritPointRate = 10f;

        [Tooltip("Daily free gems")]
        public int dailyFreeGems = 1;

        [Tooltip("Gems from watching ads")]
        public int gemsPerAd = 1;

        [Tooltip("Maximum gems from ads per day")]
        public int maxGemsFromAdsPerDay = 5;

        [Header("IAP Packages")]
        [Tooltip("Gem package configurations")]
        public GemPackage[] gemPackages;

        [Header("Premium Benefits")]
        [Tooltip("Premium player currency multipliers")]
        public PremiumBenefit[] premiumBenefits;
    }

    [System.Serializable]
    public class GemPackage
    {
        public string packageId;
        public string displayName;
        public int gemAmount;
        public float realMoneyPrice;
        public int bonusGems;
        public bool isPopular;
        public bool isBestValue;
    }

    [System.Serializable]
    public class PremiumBenefit
    {
        public CurrencyType currencyType;
        public float multiplier = 1.5f;
        public string description;
    }

    [System.Serializable]
    public class EconomicBalanceConfiguration
    {
        [Header("Inflation Control")]
        [Tooltip("Maximum currency growth rate per day (percentage)")]
        [Range(0f, 2f)]
        public float maxDailyGrowthRate = 0.5f;

        [Tooltip("Currency sink effectiveness")]
        [Range(0f, 2f)]
        public float sinkEffectiveness = 1f;

        [Header("Player Progression")]
        [Tooltip("Currency scaling with player level")]
        public AnimationCurve levelScalingCurve = AnimationCurve.Linear(1, 1, 500, 10);

        [Tooltip("Prestige currency bonus")]
        [Range(0f, 2f)]
        public float prestigeBonus = 0.1f;

        [Header("Difficulty Scaling")]
        [Tooltip("Currency earning difficulty curve")]
        public AnimationCurve difficultyScalingCurve = AnimationCurve.Linear(1, 1, 500, 5);
    }

    [System.Serializable]
    public class SeasonalEconomyConfiguration
    {
        [Header("Seasonal Essences")]
        [Tooltip("Base essence earning rate")]
        public float baseEssenceRate = 1f;

        [Tooltip("Seasonal bonus multiplier")]
        [Range(1f, 3f)]
        public float seasonalBonusMultiplier = 1.5f;

        [Tooltip("Cross-season conversion penalty")]
        [Range(0f, 0.5f)]
        public float conversionPenalty = 0.1f;

        [Header("Seasonal Events")]
        [Tooltip("Event currency multipliers")]
        public SeasonalEventMultiplier[] eventMultipliers;
    }

    [System.Serializable]
    public class SeasonalEventMultiplier
    {
        public SeasonType season;
        public CurrencyType currencyType;
        public float multiplier = 2f;
        public int durationDays = 7;
    }

    [System.Serializable]
    public class MonetizationConfiguration
    {
        [Header("Purchase Incentives")]
        [Tooltip("First purchase bonus multiplier")]
        [Range(1f, 3f)]
        public float firstPurchaseBonus = 2f;

        [Tooltip("Bulk purchase discount")]
        [Range(0f, 0.5f)]
        public float bulkPurchaseDiscount = 0.2f;

        [Header("Soft Currency Pressure")]
        [Tooltip("Soft currency earning reduction to encourage spending")]
        [Range(0f, 0.5f)]
        public float softCurrencyPressure = 0.1f;

        [Tooltip("Premium currency earning boost")]
        [Range(1f, 3f)]
        public float premiumCurrencyBoost = 1.5f;

        [Header("Retention Features")]
        [Tooltip("Login bonus scaling")]
        public LoginBonusConfiguration[] loginBonuses;

        [Tooltip("Comeback bonus for returning players")]
        public ComebackBonusConfiguration comebackBonus;
    }

    [System.Serializable]
    public class LoginBonusConfiguration
    {
        public int day;
        public CurrencyType currencyType;
        public long amount;
        public bool isPremium;
    }

    [System.Serializable]
    public class ComebackBonusConfiguration
    {
        public int daysAway;
        public CurrencyType currencyType;
        public long bonusAmount;
        public float multiplier = 2f;
    }
}