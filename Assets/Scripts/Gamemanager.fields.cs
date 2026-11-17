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

    [Header("UI - Panel de la Maquina de Recompensas (gacha)")]
    [Tooltip("El mismo GameObject que le pasas a RewardMachinePanel.panelRoot.")]
    public GameObject rewardMachinePanel;
    public int coconutHpMaxBase = 20;

    private int recordAtRunStart = 1;

    public event System.Action OnDayStarted;
    public event System.Action OnRunReset;
    public event System.Action OnRunStarted;
    public event System.Action OnRecaudacionShown;

    [Tooltip("Si es true, la PROXIMA llamada a ContinueToNextDay() no descuenta daysLeft (se consume solo).")]
    public bool skipNextDayDecrement = false;

    [Header("UI - Decision forzada del ultimo dia")]
    [Tooltip("Boton 'Bancarrota' dentro del panel de Deuda. Solo se muestra cuando la decision es forzada.")]
    public Button bankruptcyButton;
    private bool forcedOrderDecision = false;

    [Header("Desbloqueo de cocos")]
    [Tooltip("Arrastra aqui el objeto con el script CoconutUnlockPanel.")]
    public CoconutUnlockPanel unlockPanel;
    [Tooltip("true = el panel solo sale la primera vez que desbloqueas ese coco (se recuerda entre partidas). false = sale en cada partida.")]
    public bool showUnlockOnlyFirstTime = true;
    [HideInInspector] public int killsAtDayStart = 0;
    private HashSet<int> announcedUnlocks = new HashSet<int>();
    private float runStartTime = 0f;

    public bool isPaused => GameStateManager.IsPaused;

    [Header("Transicion entre Mejoras/Tienda")]
    [Tooltip("Pausa entre que se cierra un panel y se abre el otro, para que no se sienta como un swap instantaneo.")]
    public float panelSwitchBreak = 0.12f;
    private Coroutine panelSwitchRoutine;

    [Header("Umbrales de desbloqueo de tipos de coco (15 tiers, mismo orden que CoconutSpawner.coconutTypes)")]
    public List<CoconutUnlockThreshold> coconutUnlocks = new List<CoconutUnlockThreshold>();

    [Header("UI - Indicaciones (activo junto con Mejoras y Tienda)")]
    public GameObject indicacionesPanel;

    private PerkOption[] currentPerkChoices = new PerkOption[3];
    [Tooltip("Cuantas elecciones de mejora quedan pendientes por mostrar (subio por pagos de pedido seguidos).")]
    private int pendingPerkOffers = 0;

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

    private bool isPanningTree = false;
    private Vector2 lastPanMousePos;

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
}