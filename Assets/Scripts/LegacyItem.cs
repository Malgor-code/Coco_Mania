using UnityEngine;

public enum LegacyEffect
{
    PermanentDamageBonus,
    PermanentStaminaMaxBonus,
    PermanentMoneyMultiplierBonus,
    PermanentStartingCoconutBonus,
    PermanentLegacyMultiplierBonus,
    PermanentCritChanceBonus,
    PermanentCoinChanceBonus
}

[System.Serializable]
public class LegacyItem
{
    public string itemName = "Anillo";

    [TextArea]
    public string description = "";

    [Tooltip("Costo en Puntos de Legado")]
    public int cost = 1;

    public LegacyEffect effect = LegacyEffect.PermanentDamageBonus;
    public float value = 1f;
}