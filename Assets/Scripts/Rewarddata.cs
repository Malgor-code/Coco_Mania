using UnityEngine;

[CreateAssetMenu(fileName = "NewReward", menuName = "RewardMachine/Reward Data")]
public class RewardData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("ID unico y estable (ej: 'filo_improvisado'). No lo cambies despues de usarlo en una partida guardada/activa.")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Clasificacion")]
    public RewardRarity rarity;
    [Tooltip("Temporary: se pueden tener varias copias, ocupa ranura y se desgasta. PermanentRelic: una sola copia por intento.")]
    public RewardCategory category;
    public RewardEffect effect;

    [Header("Magnitud del efecto")]
    [Tooltip("Valor generico segun 'effect': +dano (2), +% critico (0.05 = 5%), +% agua (0.15 = 15%), +stamina (3), +radio (0.1), etc.")]
    public float value;

    [Header("Duracion (solo categoria Temporary)")]
    [Tooltip("Cuantos DIAS dura el efecto una vez colocado en una ranura. Se descuenta 1 cada vez que arranca un nuevo dia.")]
    public int durationDays = 1;
}