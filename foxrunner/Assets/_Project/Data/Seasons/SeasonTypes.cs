namespace FoxRunner.Seasons
{
    /// <summary>
    /// Core enumeration types for the seasonal dimension system
    /// </summary>

    public enum SeasonType
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rainy,
        Stormy,
        Snowy,
        Foggy,
        Windy,
        Misty
    }

    public enum SeasonalBonusType
    {
        ExperienceMultiplier,
        CoinMultiplier,
        SpiritPointMultiplier,
        CollectibleSpawnRate,
        MovementSpeed,
        JumpHeight,
        DashCooldown,
        AutoCollectionRadius,
        ComboTimeWindow,
        DimensionalEnergyRegeneration
    }

    public enum ModifierType
    {
        MovementSpeed,
        JumpHeight,
        DashCooldown,
        ExperienceGain,
        CoinGain,
        SpiritPointGain,
        CollectibleSpawn,
        ComboMultiplier,
        AutoCollectionRadius,
        DimensionalStability
    }

    public enum RewardType
    {
        Experience,
        Coins,
        SpiritPoints,
        DimensionalEnergy,
        Equipment,
        SpecialItem,
        Companion,
        VillageUpgrade
    }

    public enum DimensionalState
    {
        Stable,
        Unstable,
        Collapsing,
        Transitioning,
        Locked,
        Unlocked
    }

    public enum SeasonTransitionType
    {
        Gradual,
        Instant,
        Dramatic,
        Smooth
    }

    public enum DimensionalEventType
    {
        StabilityFluctuation,
        EnergyBoost,
        CollectibleStorm,
        TimeDistortion,
        GravityShift,
        ElementalSurge
    }
}