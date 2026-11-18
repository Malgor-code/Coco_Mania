using UnityEngine;

[CreateAssetMenu(fileName = "Coco", menuName = "CocoMania/Tipo de Coco")]
public class CoconutTypeSO : ScriptableObject
{
    [Header("Identidad")]
    public string typeName = "Coco";
    public Sprite icon;
    [Tooltip("Modelo del coco (su prefab).")]
    public GameObject prefab;
    [TextArea] public string phrase;

    [Header("Desbloqueo")]
    [Min(1)] public int unlockCycle = 1;

    [Header("Estadisticas")]
    [Tooltip("Vida relativa al coco base. El CoconutSpawner la multiplica por su 'hpBoost'; el botin sale de este valor.")]
    public float baseHpMultiplier = 1f;

    [Header("Pasiva (habilidad)")]
    [Tooltip("Vacio = sin habilidad.")]
    public CoconutPassiveSO passive;
    public string abilityName = "Sin habilidad";
    [TextArea] public string abilityDescription = "Un coco normal. Sin trucos.";

    [Header("Movimiento")]
    public CoconutWander.MoveStyle moveStyle = CoconutWander.MoveStyle.Despreocupado;

    public float HpMultiplier(float hpBoost) => baseHpMultiplier * hpBoost;
    public float LootMultiplier(float lootBase, float lootExponent) => lootBase * Mathf.Pow(baseHpMultiplier, lootExponent);
}