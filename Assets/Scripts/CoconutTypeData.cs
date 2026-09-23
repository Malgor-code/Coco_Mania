using UnityEngine;


[System.Serializable]
public class CoconutTypeData
{
    public string typeName = "Coco Comun";

    [Tooltip("Muertes acumuladas totales necesarias para que este tipo empiece a aparecer. 0 = disponible desde el inicio.")]
    public int killsToUnlock = 0;

    [Tooltip("Multiplica el HP base que calcula el GameManager.")]
    public float hpMultiplier = 1f;

    [Tooltip("Multiplica el dinero Y el agua de coco que suelta al morir.")]
    public float lootMultiplier = 1f;

    [Tooltip("Prefab COMPLETO de este tier (como el Coconut Prefab base). Si lo dejas vacio, el spawner usa el 'Fallback Prefab' del CoconutSpawner mientras tanto.")]
    public GameObject prefab;

    [Header("Placeholder para habilidades especiales futuras")]
    public string specialAbilityId = "";
}