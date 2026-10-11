using UnityEngine;

public enum PerkEffect
{
    DamageBonus,
    StaminaMaxBonus,
    MoneyMultiplierBonus,
    SwingIntervalReduction,
    ExtraCoconutNextCycle,
    CritChanceBonus,
    WaterMultiplierBonus,
    WaterForSwingPenalty, 
    HitRadiusBonus, 
    JackpotChanceBonus, 
    StreakMaxStacksBonus, 
    StreakPowerBonus, 
    OrderDiscount,
    CoinChanceBonus, 
    ExecuteDamageBonus, 
    ChainSplash, 
    LastBreathBonus, 
    EarlyDayWaterBonus,
    MultiHitDamageBonus, 
    DamageForStaminaPenalty
}

[System.Serializable]
public class PerkOption
{
    public string perkName = "Perk";
    public Sprite icon;
    [TextArea]
    public string description = "";

    public PerkEffect effect = PerkEffect.DamageBonus;
    public float value = 1f;
}