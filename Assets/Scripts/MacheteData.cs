using UnityEngine;

[System.Serializable]
public class MacheteData
{
    public string macheteName = "Machete de palma";
    public enum MachetePassive { Ninguna, WaterBonus, CritChance, StaminaMax, ChainSplash, CritDamage, JackpotChance, MultiHit }
    [TextArea]
    public string description = "Equilibrado. Bueno para empezar.";
    public MachetePassive passive = MachetePassive.Ninguna;
    public float passiveValue = 0f;
    public string passiveDescription = "";
    public float baseDamage = 3f;
    public float baseSwingInterval = 1.2f;
    public float baseHitRadius = 1.5f;

    [Tooltip("1 = normal. Menor a 1 = gasta menos stamina por golpe, mayor a 1 = gasta mas.")]
    public float staminaCostMultiplier = 1f;

    [Tooltip("Costo en dinero para desbloquearlo UNA vez. 0 = ya viene desbloqueado.")]
    public int unlockCost = 0;
}