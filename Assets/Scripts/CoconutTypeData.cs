using UnityEngine;

[System.Serializable]
public class CoconutTypeData
{
    public string typeName = "Coco Comun";

    [Tooltip("Muertes acumuladas totales necesarias para que este tipo empiece a aparecer. 0 = disponible desde el inicio.")]
    public int killsToUnlock = 0;

    [Tooltip("Multiplica el HP base que calcula el GameManager.")]
    public float hpMultiplier = 1f;

    [Tooltip("Multiplica el dinero que suelta al morir.")]
    public float lootMultiplier = 1f;

    [Header("Placeholder para habilidades especiales futuras")]
    public string specialAbilityId = "";
}