public enum RewardRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum RewardCategory
{
    Temporary,
    PermanentRelic,
    PhysicalItem
}

public enum PhysicalTriggerMode
{
    RealTimeInterval,
    EveryNewDay
}

public enum RewardEffect
{
    None,
    DamageBonus,
    CritChanceBonus,
    WaterMultiplierBonus,
    StaminaMaxBonus,
    HitRadiusBonus,
    SwingIntervalReduction,
    CoinDropChanceBonus,
    InstantWaterBurst,
    DestroyRandomCoconuts,
    SkipNextDayCounter,
    TempCoinMagnet,
    TempWaterBasket
}