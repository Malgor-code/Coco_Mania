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
    public RewardCategory category;
    public RewardEffect effect;

    [Header("Magnitud del efecto")]
    [Tooltip("Valor generico segun 'effect': +dano (2), +% critico (0.05 = 5%), +% agua (0.15 = 15%), +stamina (3), +radio (0.1), mL de agua instantanea (150), cantidad de cocos a destruir (3), etc.")]
    public float value;

    [Header("Duracion (solo categoria Temporary)")]
    [Tooltip("Cuantos DIAS dura el efecto antes de revertirse solo. Se descuenta 1 cada vez que arranca un nuevo dia.")]
    public int durationDays = 1;

    [Header("Duracion en tiempo real (solo efectos TempCoinMagnet / TempWaterBasket)")]
    [Tooltip("Segundos reales que dura el efecto (no dias de juego). Se usa para objetos fisicos tipo 'buff temporal en tiempo real'.")]
    public float durationSeconds = 10f;

    [Header("Activacion automatica (solo categoria PhysicalItem)")]
    [Tooltip("RealTimeInterval: se dispara solo cada 'Interval Seconds'. EveryNewDay: se dispara solo una vez cada vez que arranca un dia nuevo (sin importar cuanto tiempo real pase).")]
    public PhysicalTriggerMode triggerMode = PhysicalTriggerMode.RealTimeInterval;
    [Tooltip("Solo si Trigger Mode = RealTimeInterval. Segundos reales entre cada activacion automatica.")]
    public float intervalSeconds = 20f;
    [Tooltip("Cuantas veces en total se puede disparar automaticamente durante el intento. -1 = ilimitado (se sigue activando solo, cada intervalo o cada dia, mientras dure el intento).")]
    public int usesPerRun = -1;

    [Header("Duplicados")]
    [Tooltip("Si esta marcado, el jugador puede tener varias copias activas a la vez (ej. dos reliquias identicas sumando su bono). Si NO esta marcado, obtener otra copia mientras ya tenes una activa la convierte en Fragmentos de Cobrador en vez de aplicarla de nuevo.")]
    public bool canRepeat = false;
    [Tooltip("Fragmentos que da si sale como duplicada. -1 = usar la tabla por rareza configurada en RewardMachineManager.")]
    public int fragmentOverride = -1;
}