using UnityEngine;

public enum PerkEffect
{
    DamageBonus,
    StaminaMaxBonus,
    MoneyMultiplierBonus,
    SwingIntervalReduction,
    ExtraCoconutNextCycle
}

[System.Serializable]
public class PerkOption
{
    public string perkName = "Perk";

    [TextArea]
    public string description = "";

    public PerkEffect effect = PerkEffect.DamageBonus;
    public float value = 1f;
}