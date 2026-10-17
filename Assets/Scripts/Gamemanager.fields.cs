using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static MacheteData;
public partial class GameManager
{
    [Header("Fase actual")]
    public Phase currentPhase = Phase.MainMenu;

    [Header("Machete (se ACTIVA recien al empezar a golpear, se apaga en menus/paneles)")]
    [Tooltip("El GameObject raiz del machete (el que tiene MacheteController). Se activa en Play y en Siguiente Dia, y se desactiva en cualquier otra fase (tienda, pedido, arbol, perks, entre-partidas).")]
    public GameObject machete;
    public int money = 0;

    [Header("Agua de coco (UNICO recurso del juego: sirve para el pedido Y para pagar mejoras/machetes)")]
    [Tooltip("Cuanta agua de coco (en mL) tienes acumulada ahora mismo. Se gasta tanto al entregar el pedido como al comprar mejoras en la tienda/arbol - por eso hay que elegir entre progresar o guardar para el pedido.")]
    public float waterCurrentML = 0f;
    [Tooltip("Cuanta agua de coco (en mL) pide el cliente este ciclo")]
    public float waterTargetML = 250f;
    public float waterTargetStart = 250f;
    [Tooltip("Cuanto crece el pedido de agua en cada ciclo nuevo (2 = se duplica)")]
    public float waterTargetGrowth = 1.8f;

    [Header("Cuanta agua suelta cada coco al morir")]
    public float baseWaterPerKill = 40f;
    public float waterVariance = 90f;

    [Header("Skill Tree - Critico")]
    public float skillCritChanceBonus = 0f;

    [Header("Dias / ciclos")]
    public int daysLeft = 6;
    public int dayLimitBase = 6;
    public int billCycle = 1;

    [Header("UI - Panel de la Maquina de Recompensas (gacha)")]
    [Tooltip("El mismo GameObject que le pasas a RewardMachinePanel.panelRoot.")]
    public GameObject rewardMachinePanel;
    public int coconutHpMaxBase = 20;

    [Header("Progreso permanente (para desbloquear tipos de coco) - se resetea en bancarrota")]
    public int totalCoconutsKilled = 0;

    private int skillStreakForgives = 0;
    private int streakForgivesLeft = 0;
    private float skillLastBreathBonus = 0f;
    private float skillChainSplashPercent = 0f;
    private float skillLastBreathSwingReduction = 0f;
    private int skillStaminaRecoveryLevel = 0;
    static readonly float[] StaminaRecoveryCaps = { 4f, 6f, 9f, 12f, 16f };
    const float StaminaRecoveryBase = 1f;
    private int recordAtRunStart = 1;
    private bool lastBreathSwingActive = false;
    private bool isApplyingSplash = false;
    public float executeHpThreshold = 0.5f;
    public float earlyDayDuration = 5f;
    private float skillExecuteBonus = 0f;
    private float skillMultiHitBonus = 0f;
    private float skillOrderRefund = 0f;
    private float skillEarlyDayBonus = 0f;
    private float dayElapsed = 0f;

    [Header("Reward Machine - bonos de reliquias (temporales o permanentes del intento)")]
    public float relicDamageBonus = 0f;
    public float relicStaminaMaxBonus = 0f;
    public float relicWaterMultiplierBonus = 0f;
    public float relicSwingIntervalReduction = 0f;
    public float relicHitRadiusBonus = 0f;
    public float relicCritChanceBonus = 0f;

    public event System.Action OnDayStarted;
    public event System.Action OnRunReset;
    public event System.Action OnRunStarted;
    public event System.Action OnRecaudacionShown;

    [Tooltip("Si es true, la PROXIMA llamada a ContinueToNextDay() no descuenta daysLeft (se consume solo).")]
    public bool skipNextDayDecrement = false;

    [HideInInspector] public List<MacheteData> macheteOptions = new List<MacheteData>();
    [HideInInspector] public List<SkillNode> skillTree = new List<SkillNode>();
    [HideInInspector] public List<PerkOption> perkPool = new List<PerkOption>();
    [HideInInspector] public List<LegacyItem> legacyShop = new List<LegacyItem>();

    [Header("BETA - Datos")]
    [Tooltip("Si es true, EnsureDefaultData SOBREESCRIBE las listas (machetes, arbol, perks, legado) en cada arranque. Asi todos tus amigos juegan con el mismo balance.")]
    public bool forceDefaultData = true;

    [Header("BETA - Ritmo del pedido")]
    [Tooltip("Crecimiento del pedido hasta el ciclo lateGrowthFromCycle.")]
    public float lateGrowthFromCycle = 8f;
    [Tooltip("Crecimiento del pedido a partir del ciclo siguiente. Mas bajo = el late game se puede superar.")]
    public float waterTargetGrowthLate = 1.45f;

    [Header("UI - Decision forzada del ultimo dia")]
    [Tooltip("Boton 'Bancarrota' dentro del panel de Deuda. Solo se muestra cuando la decision es forzada.")]
    public Button bankruptcyButton;
    private bool forcedOrderDecision = false;

    [Header("BETA - Meta de la partida (condicion de victoria)")]
    public int victoryCycle = 16;
    public int victoryLegacyBonus = 10;
    private bool runVictoryAwarded = false;

    [Header("Desbloqueo de cocos")]
    [Tooltip("Arrastra aqui el objeto con el script CoconutUnlockPanel.")]
    public CoconutUnlockPanel unlockPanel;
    [Tooltip("true = el panel solo sale la primera vez que desbloqueas ese coco (se recuerda entre partidas). false = sale en cada partida.")]
    public bool showUnlockOnlyFirstTime = true;
    [HideInInspector] public int killsAtDayStart = 0;
    private HashSet<int> announcedUnlocks = new HashSet<int>();
    private float runStartTime = 0f;

    private int legacyStartingCoconuts = 0;
    private float legacyMultiplierBonus = 0f;
    private float legacyCoinChanceBonus = 0f;

    [Header("UI - Menu de pausa (Esc)")]
    public GameObject pauseMenuPanel;
    public bool isPaused = false;

    [Header("Dificultad por Legado (los cocos escalan segun cuanto legado llevas)")]
    [Tooltip("Cuanto HP extra por 'paso' de legado. Sube = mas dificil.")]
    public float legacyHpPerStep = 0.03f;
    [Tooltip("Exponente de la curva. Menor a 1 = crece cada vez mas despacio.")]
    public float legacyHpExponent = 0.7f;
    [Tooltip("Multiplicador maximo de HP por legado.")]
    public float legacyHpCap = 3f;
    [Tooltip("Ciclos que tarda en aplicarse al 100% (arranca en el ciclo 1).")]
    public int legacyHpRampCycles = 6;
    [Range(0f, 1f)]
    [Tooltip("Que parte del HP extra se devuelve como agua extra (0 = nada, 1 = proporcional).")]
    public float legacyLootCompensation = 0.4f;

    [Header("Transicion entre Mejoras/Tienda")]
    [Tooltip("Pausa entre que se cierra un panel y se abre el otro, para que no se sienta como un swap instantaneo.")]
    public float panelSwitchBreak = 0.12f;
    private Coroutine panelSwitchRoutine;
    private float skillExtraCoconutChance = 0f;

    [Header("Umbrales de desbloqueo de tipos de coco (15 tiers, mismo orden que CoconutSpawner.coconutTypes)")]
    public List<CoconutUnlockThreshold> coconutUnlocks = new List<CoconutUnlockThreshold>();

    [Header("Estadisticas de ESTA partida (se resetean al empezar de nuevo)")]
    public int runCoconutsKilled = 0;
    [Tooltip("Ahora acumula mL de agua ganados en la partida (antes era dinero). Se deja el nombre del campo para no romper otros scripts.")]
    public int runMoneyEarned = 0;

    [Header("Estadisticas de ESTE DIA/ronda (se reinician cada vez que vuelves a golpear)")]
    public int dayCoconutsKilled = 0;
    [Tooltip("Ahora acumula mL de agua ganados en el dia (antes era dinero). Se deja el nombre del campo para no romper otros scripts.")]
    public int dayMoneyEarned = 0;
    [Tooltip("Cuantas veces golpeaste (conectes o no) durante el dia. Lo suma MacheteController via RegisterSwingResult().")]
    public int daySwings = 0;
    [Tooltip("De esos golpes, cuantos conectaron con al menos un coco.")]
    public int dayHits = 0;
    [Tooltip("Cuantas monedas encontraste durante el dia (se resetea cada dia; el total de toda la partida sigue siendo coconutCoins).")]
    public int dayCoinsFound = 0;

    [Header("Energia (stamina) - se mide en SEGUNDOS, se gasta por tiempo, no por golpe")]
    public float stamina = 15f;
    public float staminaMaxBase = 15f;
    public float staminaStartDelay = 2f;
    public int coconutCoins = 0;
    [HideInInspector] public bool lastKillFoundCoin = false;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que un coco suelte 1 moneda al morir. Empieza en 0.5% (0.005) y sube con los nodos 'Suerte de Moneda' del arbol de mejoras.")]
    public float coinDropChance = 0.005f;

    [Header("Progreso permanente (guardado)")]
    public int bestRunKills = 0;
    public int bestCycleReached = 1;
    public int equippedMacheteIndex = 0;

    private int skillStreakStackBonus = 0;
    private float skillStreakPowerBonus = 0f;
    private HashSet<string> unlockedSkillIds = new HashSet<string>();

    [Header("UI - Indicaciones (activo junto con Mejoras y Tienda)")]
    public GameObject indicacionesPanel;

    private float skillDamageBonus = 0f;
    private float skillStaminaBonus = 0f;
    private float skillSwingIntervalReduction = 0f;
    private float skillHitRadiusBonus = 0f;
    [Tooltip("Bonus de agua extra por golpe. Alimentado tanto por nodos WaterMultiplierBonus (ej. 'Coco Lechero') como por nodos MoneyMultiplierBonus reciclados (ej. 'Buen Ojo'), ya que ahora todo bonus de 'ingreso' es agua.")]
    private float skillWaterMultiplierBonus = 0f;

    [Header("Golpe Eficiente -> ahora recupera resistencia al destruir un coco (4 niveles, tope 35%)")]
    private float skillStaminaRecoveryChance = 0f;
    private float skillStaminaRecoveryAmount = 0f;

    [Header("Jackpot de Agua (reemplaza 'moneda' en el arbol; las monedas de verdad siguen existiendo via coinDropChance)")]
    private float skillJackpotChance = 0f;
    [HideInInspector] public bool lastKillHadJackpot = false;

    [Header("Multiplicador de dano critico (separado de la PROBABILIDAD de critico)")]
    private float skillCritDamageBonus = 0f;

    [Header("Racha de aciertos (golpes seguidos sin fallar)")]
    [Tooltip("A partir de cuantos golpes seguidos conectados empieza a sumar bono.")]
    public int streakThreshold = 3;
    public float streakDamageBonusPerHit = 0.05f;
    public float streakWaterBonusPerHit = 0.05f;
    public int streakMaxStacks = 6;
    private int currentHitStreak = 0;
    private PerkOption[] currentPerkChoices = new PerkOption[3];
    [Tooltip("Cuantas elecciones de mejora quedan pendientes por mostrar (subio por pagos de pedido seguidos).")]
    private int pendingPerkOffers = 0;

    [Header("Perks")]
    public float perkDamageBonus = 0f;
    public float perkStaminaMaxBonus = 0f;
    public float perkWaterMultiplierBonus = 0f;
    public float perkSwingIntervalReduction = 0f;
    public float perkCritChanceBonus = 0f;
    private float curseSwingPenalty = 0f;
    private float curseWaterPenalty = 0f;
    private float curseStaminaPenalty = 0f;
    private float curseSpawnPenalty = 0f;
    private int curseGeneration = 0;
    private float perkWaterMultiplicative = 1f;

    [Header("Legado (permanente, solo se gasta tras una bancarrota)")]
    public int legacyPoints = 0;
    public int totalLegacyPointsEarned = 0;
    public float legacyMultiplier = 1f;
    private HashSet<string> purchasedLegacyItems = new HashSet<string>();

    [Header("Legacy")]
    public float legacyDamageBonus = 0f;
    public float legacyStaminaBonus = 0f;
    public float legacyWaterMultiplierBonus = 0f;
    public float legacyCritChanceBonus = 0f;
    private int legacyExtraDays = 0;
    private float legacyStartingWater = 0f;
    private float legacyJackpotChance = 0f;
    private int legacyStreakStacks = 0;
    private float legacyHitRadiusBonus = 0f;
    private float legacyOrderDiscount = 0f;
    private int legacyStartingMachete = 0;
    const int StreakHardCap = 12;
    const float StreakPowerCap = 0.10f;

    [Header("UI - Gameplay (solo stamina)")]
    public GameObject gameplayUIPanel;
    public Slider staminaSlider;

    [Header("UI - Agua actual (opcional, siempre visible en pantalla; antes mostraba dinero)")]
    public TMP_Text moneyText;
    public TMP_Text moneyText2;
    public TMP_Text moneyText3;

    [Header("UI - Recaudacion (hub: estadisticas + 3 botones)")]
    public GameObject recaudacionPanel;
    public TMP_Text runKillsText;
    public TMP_Text runMoneyText;
    [Tooltip("Muestra el porcentaje de golpes que conectaron con al menos un coco, sobre el total de golpes del dia.")]
    public TMP_Text accuracyText;
    [Tooltip("Muestra cuantas monedas encontraste en el dia. Si no encontraste ninguna, se desactiva solo.")]
    public TMP_Text dayCoinsText;
    public Button continueButton;
    public float maxStaminaRecoveryPerDay = 12f;
    private float dayStaminaRecovered = 0f;

    [Header("UI - Recaudacion: progreso de desbloqueo del proximo tipo de coco")]
    [Tooltip("Imagen del proximo coco a desbloquear. Se muestra con alpha bajo y se va poniendo mas opaca a medida que te acercas al 100%.")]
    public Image nextUnlockImage;
    public TMP_Text nextUnlockPercentText;
    [Range(0f, 1f)] public float lockedImageMinAlpha = 0.15f;

    [Header("UI - Panel del Arbol de Mejoras")]
    public GameObject upgradeTreePanel;

    [Header("UI - Pan y Zoom del Arbol de Mejoras")]
    public RectTransform upgradeTreeContent;
    public RectTransform upgradeTreeViewport;
    public int panMouseButton = 2;
    public float panSpeed = 1f;
    public Vector2 panLimit = new Vector2(800f, 800f);
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2f;

    [Tooltip("El Jackpot suelta ESTE multiplo extra del agua que dio el coco (1.5 = +150%).")]
    public float jackpotBaseMultiplier = 1.5f;
    private float skillJackpotMultBonus = 0f;

    private bool isPanningTree = false;
    private Vector2 lastPanMousePos;
    public int maxCopiesPerPerk = 3;
    private Dictionary<string, int> perkCopies = new Dictionary<string, int>();

    [Header("UI - Panel de Tienda (machetes)")]
    public GameObject tiendaPanel;
    public TMP_Text macheteStatusText;
    public TMP_Text macheteUpgradeCostText;
    public Button macheteUpgradeButton;

    [Header("UI - Monedas (coleccionable, solo informativo; la mejora de probabilidad ahora esta en el Arbol de Mejoras)")]
    public TMP_Text coinCountText;

    [Header("UI - Panel del Pedido (antes 'Deuda')")]
    public GameObject deudaPanel;
    [Tooltip("Muestra tu agua acumulada actual (antes mostraba dinero).")]
    public TMP_Text deudaMoneyText;
    [Tooltip("Primer texto: cuantos litros/mL TE FALTAN para completar el pedido.")]
    public TMP_Text deudaBillText;
    [Tooltip("Segundo texto: dias restantes. Si es el ULTIMO dia sin haber entregado, muestra un aviso urgente en vez del numero.")]
    public TMP_Text deudaDaysLeftText;
    [Tooltip("Antes 'Pagar Deuda', ahora entrega el agua acumulada si alcanza el pedido")]
    public Button payDebtButton;
    [Tooltip("Color del texto de dias cuando es urgente (ultimo dia sin entregar).")]
    public Color urgentColor = new Color(1f, 0.25f, 0.25f);
    public Color normalDaysColor = Color.white;
    const float SwingFloor = 0.2f;

    [Header("UI - Eleccion de Perk")]
    public GameObject perkChoiceUIPanel;
    public TMP_Text perkOption1Name, perkOption1Desc;
    public TMP_Text perkOption2Name, perkOption2Desc;
    public TMP_Text perkOption3Name, perkOption3Desc;

    [Header("UI - Entre partidas (SOLO aqui se gasta el Legado)")]
    public GameObject betweenRunsPanel;
    public TMP_Text legacyPointsText;
    public TMP_Text bankruptcyMessageText;

    [Header("UI - Eleccion de Perk: iconos y FX")]
    public Image perkOption1Icon, perkOption2Icon, perkOption3Icon;
    public PerkCardFX perkCard1, perkCard2, perkCard3;
    public float perkCardStagger = 0.12f;
    private bool isChoosingPerk = false;

    [Header("UI - Log")]
    public TMP_Text logText;

    [Header("Gamefeel: contador de agua 'rodando' en vez de saltar de golpe")]
    public float moneyCounterSpeed = 6f;
    private float displayedMoney = 0f;

    [Header("Iconos de perks (por nombre)")]
    public List<PerkIconEntry> perkIcons = new List<PerkIconEntry>();
    static readonly Dictionary<string, int> PerkMinCycle = new Dictionary<string, int>
    {
        { "Madrugador", 2 }, { "Buen Regateo", 3 }, { "Racha Viva", 3 },
        { "Pulso Firme", 4 }, { "Doble Golpe", 4 },
        { "Corazón de Jackpot", 5 }, { "Último Aliento", 5 }, { "Machete Pesado", 5 },
        { "Instinto Cazador", 6 }, { "Chispa en Cadena", 6 },
        { "Cosecha Abundante", 7 }, { "Manos de Hierro", 7 }, { "Pulmones de Acero", 7 },
        { "Pacto del Cobrador", 8 }, { "Ojo de Halcón", 8 },
        { "Racha Imparable", 10 },
        { "Coco Dorado", 11 }, { "Manos de Titán", 11 }, { "Jackpot Mayor", 11 }, { "Cazador Maestro", 11 },
    };

    static readonly Dictionary<string, int> PerkMaxCycle = new Dictionary<string, int>
    {
        { "Manos Firmes", 10 }, { "Segundo Aire", 10 }, { "Buen Trato", 10 },
        { "Golpe Certero", 10 }, { "Golpe de Suerte", 10 }, { "Cosecha Extra", 10 },
    };
}