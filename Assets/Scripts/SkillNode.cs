using UnityEngine;
public enum SkillEffect
{
    None,
    DamageFlatBonus,
    StaminaMaxFlatBonus,
    SwingIntervalReduction,
    HitRadiusBonus,
    MoneyMultiplierBonus,
    StaminaCostReduction,
    ExtraStartingCoconut,
    SpawnIntervalReduction
}

[System.Serializable]
public class SkillNode
{
    [Tooltip("Identificador unico, ej. 'fuerza_1'. Se usa para las dependencias.")]
    public string id;

    public string nodeName = "Nodo";

    [TextArea]
    public string description = "";

    public int cost = 1;

    [Tooltip("IDs de los nodos que hay que desbloquear ANTES que este (vacio = disponible desde el inicio)")]
    public string[] prerequisiteIds;

    public SkillEffect effect = SkillEffect.None;
    public float effectValue = 1f;
}