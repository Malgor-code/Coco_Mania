using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using static MacheteData;
public class GameManager : MonoBehaviour
{
    public enum Phase { MainMenu, Hitting, Shop, PerkChoice, BetweenRuns }
    public enum CoconutAbility
    {
        Ninguna,
        Generoso,
        Resistente,
        FibraDura,
        Pesado,
        Tenaz,
        Armadura,
        Rebote,
        Fortificado,
        Implacable,
        FragilValioso,
        Regeneracion,
        Camuflaje,
        Maldicion,
        Supremo
    }

    public static GameManager Instance;

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
    public float GetCritChance() => Mathf.Clamp01(GetRawCritChance());
    float GetRawCritChance() => skillCritChanceBonus + perkCritChanceBonus + legacyCritChanceBonus + relicCritChanceBonus + PassiveValue(MachetePassive.CritChance);
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
    [System.Serializable]
    public class CoconutUnlockThreshold
    {
        [HideInInspector] public string coconutName;
        [HideInInspector] public int cycleRequired;
        [HideInInspector] public CoconutAbility ability = CoconutAbility.Ninguna;

        public Sprite icon;
        public GameObject prefab;
    }
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
    public CoconutAbility GetAbilityForCoconutName(string name)
    {
        if (coconutUnlocks == null || string.IsNullOrEmpty(name)) return CoconutAbility.Ninguna;
        foreach (var u in coconutUnlocks)
        {
            if (u != null && u.coconutName == name) return u.ability;
        }
        return CoconutAbility.Ninguna;
    }
    public void ApplyStaminaPenalty(float amount)
    {
        stamina = Mathf.Max(0f, stamina - amount);
    }
    public void RegisterSwingResult(bool hit)
    {
        daySwings++;
        if (hit)
        {
            dayHits++;
            currentHitStreak++;
        }
        else if (streakForgivesLeft > 0 && currentHitStreak >= streakThreshold)
        {
            streakForgivesLeft--;
        }
        else
        {
            currentHitStreak = 0;
        }
    }

    int CurrentStreakStacks()
    {
        if (!StreakUnlocked) return 0;
        int stacks = currentHitStreak - streakThreshold + 1;
        int max = Mathf.Min(StreakHardCap, skillStreakStackBonus + legacyStreakStacks);
        return Mathf.Clamp(stacks, 0, max);
    }
    public float GetStreakDamageMultiplier() =>
        1f + CurrentStreakStacks() * Mathf.Min(StreakPowerCap, streakDamageBonusPerHit + skillStreakPowerBonus);
    public float GetStreakWaterMultiplier() =>
        1f + CurrentStreakStacks() * Mathf.Min(StreakPowerCap, streakWaterBonusPerHit + skillStreakPowerBonus);
    private int legacyStartingCoconuts = 0;
    private float legacyMultiplierBonus = 0f;
    private float legacyCoinChanceBonus = 0f;
    [Header("UI - Menu de pausa (Esc)")]
    public GameObject pauseMenuPanel;
    public bool isPaused = false;
    void RecalcLegacyMultiplier()
    {
        legacyMultiplier = 1f + totalLegacyPointsEarned * 0.1f * (1f + legacyMultiplierBonus);
    }
    public float GetCritMultiplier() => 1.3f + skillCritDamageBonus + PassiveValue(MachetePassive.CritDamage) + Mathf.Max(0f, GetRawCritChance() - 1f);

    public void ApplyTemporaryCurse(float durationSeconds)
    {
        StartCoroutine(TemporaryCurseRoutine(durationSeconds));
    }
    string PreviousLegacyTier(string n)
    {
        string basePart = null;
        if (n.EndsWith(" IV")) basePart = n.Substring(0, n.Length - 3) + " III";
        else if (n.EndsWith(" III")) basePart = n.Substring(0, n.Length - 4) + " II";
        else if (n.EndsWith(" II")) basePart = n.Substring(0, n.Length - 3);
        if (basePart == null) return null;
        if (GetLegacyItem(basePart) == null && GetLegacyItem(basePart + " I") != null) return basePart + " I";
        return basePart;
    }
    private IEnumerator TemporaryCurseRoutine(float duration)
    {
        int gen = curseGeneration;
        int effect = Random.Range(0, 4);
        float amount = 0f;

        switch (effect)
        {
            case 0:
                amount = 0.15f; curseSwingPenalty += amount;
                RecalculateAllStats();
                Log("Maldicion: golpes mas lentos por un rato...");
                break;
            case 1:
                amount = 0.15f; curseWaterPenalty += amount;
                Log("Maldicion: menos agua por un rato...");
                break;
            case 2:
                amount = staminaMaxBase * 0.2f; curseStaminaPenalty += amount;
                Log("Maldicion: menos resistencia por un rato...");
                break;
            case 3:
                if (CoconutSpawner.Instance == null) yield break;
                amount = 1.5f; curseSpawnPenalty += amount;
                CoconutSpawner.Instance.spawnInterval += amount;
                Log("Maldicion: los cocos tardan mas en aparecer...");
                break;
        }

        yield return new WaitForSeconds(duration);

        if (gen != curseGeneration) yield break;

        switch (effect)
        {
            case 0: curseSwingPenalty -= amount; RecalculateAllStats(); break;
            case 1: curseWaterPenalty -= amount; break;
            case 2: curseStaminaPenalty -= amount; break;
            case 3:
                curseSpawnPenalty -= amount;
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.spawnInterval -= amount;
                break;
        }
    }
    void ClearCurse()
    {
        curseGeneration++;
        if (curseSpawnPenalty > 0f && CoconutSpawner.Instance != null)
            CoconutSpawner.Instance.spawnInterval -= curseSpawnPenalty;
        curseSwingPenalty = curseWaterPenalty = curseStaminaPenalty = curseSpawnPenalty = 0f;
    }
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
    float PassiveValue(MachetePassive p)
    {
        MacheteData m = GetCurrentMachete();
        return (m != null && m.passive == p) ? m.passiveValue : 0f;
    }

    string PassiveLine(MacheteData m)
    {
        return (m != null && m.passive != MachetePassive.Ninguna && !string.IsNullOrEmpty(m.passiveDescription))
            ? "\n" + m.passiveDescription : "";
    }
    string StatsGainLine(MacheteData cur, MacheteData next)
    {
        if (next == null) return "";
        float dDmg = next.baseDamage - (cur != null ? cur.baseDamage : 0f);
        float dSwing = next.baseSwingInterval - (cur != null ? cur.baseSwingInterval : next.baseSwingInterval);
        float dRadius = next.baseHitRadius - (cur != null ? cur.baseHitRadius : 0f);

        return "\nDaño " + Signed(dDmg)
             + "  |  Golpe " + Signed(dSwing) + " s"
             + "  |  Radio " + Signed(dRadius);
    }
    static string Signed(float v) => (v >= 0f ? "+" : "") + v.ToString("0.##");
    int GetDayLimitBase() => dayLimitBase + legacyExtraDays;
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
    void Awake()
    {
        Instance = this;
        earlyDayDuration = 5f;
        EnsureDefaultData();
    }

    void Start()
    {
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (machete != null) machete.SetActive(false);
        if (indicacionesPanel != null) indicacionesPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        SetCursorVisible(true);
        LoadGame();
        RecalculateAllStats();
        UpdateMoneyUI();
        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && currentPhase != Phase.MainMenu && !CoconutUnlockPanel.BlocksPause && !StoryManager.BlocksPause)
        {
            if (isPaused) ResumeGame(); else PauseGame();
        }

        if (currentPhase == Phase.Hitting)
        {
            if (dayElapsed >= staminaStartDelay)
            {
                stamina -= Time.deltaTime;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    EnterRecaudacionPhase();
                }
            }

            bool shouldBeActive = skillLastBreathSwingReduction > 0f && stamina <= GetStaminaMax() * 0.2f;
            if (shouldBeActive != lastBreathSwingActive)
            {
                lastBreathSwingActive = shouldBeActive;
                RecalculateAllStats();
            }
            dayElapsed += Time.deltaTime;
        }

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = GetStaminaMax();
            staminaSlider.value = stamina;
        }

        UpdateMoneyUI();
        HandleUpgradeTreePanZoom();
    }
    void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
    }

    void HandleUpgradeTreePanZoom()
    {
        if (upgradeTreeContent == null) return;

        bool panelOpen = upgradeTreePanel != null && upgradeTreePanel.activeInHierarchy;
        if (!panelOpen)
        {
            isPanningTree = false;
            return;
        }
        if (Input.GetMouseButtonDown(panMouseButton))
        {
            isPanningTree = true;
            lastPanMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(panMouseButton))
        {
            isPanningTree = false;
        }

        if (isPanningTree)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 delta = (currentMousePos - lastPanMousePos) * panSpeed;
            Vector2 newPos = upgradeTreeContent.anchoredPosition + delta;
            ClampContentPosition(ref newPos);
            upgradeTreeContent.anchoredPosition = newPos;
            lastPanMousePos = currentMousePos;
        }
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newScale = Mathf.Clamp(upgradeTreeContent.localScale.x + scroll * zoomSpeed, minZoom, maxZoom);
            upgradeTreeContent.localScale = new Vector3(newScale, newScale, upgradeTreeContent.localScale.z);
            Vector2 clampedPos = upgradeTreeContent.anchoredPosition;
            ClampContentPosition(ref clampedPos);
            upgradeTreeContent.anchoredPosition = clampedPos;
        }
    }

    void ClampContentPosition(ref Vector2 pos)
    {
        if (upgradeTreeViewport != null)
        {
            Vector2 contentSize = Vector2.Scale(upgradeTreeContent.rect.size, upgradeTreeContent.localScale);
            Vector2 viewportSize = upgradeTreeViewport.rect.size;

            float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) / 2f);
            float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) / 2f);

            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        }
        else
        {
            pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);
            pos.y = Mathf.Clamp(pos.y, -panLimit.y, panLimit.y);
        }
    }

    public void RecenterUpgradeTree()
    {
        if (upgradeTreeContent == null) return;
        upgradeTreeContent.anchoredPosition = Vector2.zero;
        upgradeTreeContent.localScale = Vector3.one;
    }

    [Header("Gamefeel: contador de agua 'rodando' en vez de saltar de golpe")]
    public float moneyCounterSpeed = 6f;
    private float displayedMoney = 0f;

    void UpdateMoneyUI()
    {
        if (!Mathf.Approximately(displayedMoney, waterCurrentML))
        {
            float diff = waterCurrentML - displayedMoney;
            float step = diff * moneyCounterSpeed * Time.unscaledDeltaTime;
            if (Mathf.Abs(step) < 1f) step = Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), 1f);
            displayedMoney += step;

            bool overshot = (diff > 0f && displayedMoney > waterCurrentML) || (diff < 0f && displayedMoney < waterCurrentML);
            if (overshot) displayedMoney = waterCurrentML;
        }

        if (moneyText != null) moneyText.text = Loc("collection_current_money", FormatWater(displayedMoney));
        if (moneyText2 != null) moneyText2.text = Loc("collection_current_money", FormatWater(displayedMoney));
        if (moneyText3 != null) moneyText3.text = Loc("collection_current_money", FormatWater(displayedMoney));
        if (coinCountText != null) coinCountText.text = Loc("collection_coins", coconutCoins);

    }
    public bool PlayWaterDropEffect(Vector3 worldPosition, float waterAmountML, GameObject deathFx = null)
    {
        if (WaterJarGroup.Instance == null) return false;
        return WaterJarGroup.Instance.HandleCoconutWater(worldPosition, waterAmountML, deathFx);
    }

    public static string FormatWater(float ml)
    {
        if (ml >= 1000f) return (ml / 1000f).ToString("0.0") + " L";
        return Mathf.RoundToInt(ml) + " mL";
    }

    public bool IsHittingPhase() => currentPhase == Phase.Hitting;
    public bool TimedSpawnUnlocked => unlockedSkillIds.Contains("aparicion_1");
    public bool StreakUnlocked => unlockedSkillIds.Contains("racha_1");
    public int DaysToPlay => Mathf.Max(0, skipNextDayDecrement ? daysLeft : daysLeft - 1);
    public bool IsOrderUrgent => waterCurrentML < waterTargetML && DaysToPlay <= 1;

    string Loc(string key) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
    string Loc(string key, params object[] args) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key, args) : key;

    public void BeginNewGame()
    {
        recordAtRunStart = bestCycleReached;
        stamina = GetStaminaMax();
        daysLeft = GetDayLimitBase();
        waterCurrentML = Mathf.Max(waterCurrentML, legacyStartingWater);
        equippedMacheteIndex = Mathf.Max(equippedMacheteIndex, legacyStartingMachete);
        if (billCycle == 1) waterTargetML = waterTargetStart * (1f - legacyOrderDiscount);
        runCoconutsKilled = 0;
        runMoneyEarned = 0;
        RecalculateAllStats();
        stamina = GetStaminaMax();
        ShowHittingUI();
        RefreshAllUI();
        runStartTime = Time.time;
        BetaLog("run_start");
        OnRunStarted?.Invoke();
    }

    public int GetCoconutHpMax()
    {
        return coconutHpMaxBase;
    }

    void RecalculateAllStats()
    {
        MacheteData m = GetCurrentMachete();
        float baseDmg = m != null ? m.baseDamage : 3f;
        float baseSwing = m != null ? m.baseSwingInterval : 1.2f;
        float baseRadius = m != null ? m.baseHitRadius : .8f;
        int damage = Mathf.RoundToInt(baseDmg + skillDamageBonus + perkDamageBonus + legacyDamageBonus + relicDamageBonus);

        if (MacheteController.Instance != null)
        {
            MacheteController.Instance.damageOverride = damage;
            MacheteController.Instance.hitRadius = baseRadius + skillHitRadiusBonus + relicHitRadiusBonus + legacyHitRadiusBonus;
            MacheteController.Instance.swingInterval = Mathf.Max(SwingFloor, GetRawSwingInterval(true));
        }
    }
    public void RefreshCombatStats() => RecalculateAllStats();
    public float GetStaminaMax()
    {
        return Mathf.Max(1f, staminaMaxBase + skillStaminaBonus + perkStaminaMaxBonus
            + legacyStaminaBonus + relicStaminaMaxBonus + PassiveValue(MachetePassive.StaminaMax) - curseStaminaPenalty);
    }

    [System.Obsolete("Renombrado conceptualmente: ahora devuelve el bonus de AGUA extra (no de dinero, que ya no existe). Se deja el nombre por compatibilidad con UI existente.")]
    public float GetSkillMoneyMultiplierBonusPercent()
    {
        return skillWaterMultiplierBonus * 100f;
    }

    public float GetSkillWaterMultiplierBonusPercent()
    {
        return skillWaterMultiplierBonus * 100f;
    }

    public CoconutUnlockThreshold GetNextLockedCoconut()
    {
        CoconutUnlockThreshold next = null;
        if (coconutUnlocks == null) return null;
        foreach (var u in coconutUnlocks)
        {
            if (u == null) continue;
            if (billCycle < u.cycleRequired)
            {
                if (next == null || u.cycleRequired < next.cycleRequired) next = u;
            }
        }
        return next;
    }

    void AnnounceNewUnlocks()
    {
        if (unlockPanel == null || coconutUnlocks == null) return;
        for (int i = 1; i < coconutUnlocks.Count; i++)
        {
            CoconutUnlockThreshold u = coconutUnlocks[i];
            if (u == null || billCycle < u.cycleRequired) continue;
            if (!announcedUnlocks.Add(i)) continue;
            if (showUnlockOnlyFirstTime)
            {
                string key = "coco_visto_" + i;
                if (PlayerPrefs.GetInt(key, 0) == 1) continue;
                PlayerPrefs.SetInt(key, 1);
            }
            unlockPanel.Enqueue(i);
        }
        PlayerPrefs.Save();
    }

    public float GetNextUnlockProgress01()
    {
        CoconutUnlockThreshold next = GetNextLockedCoconut();
        if (next == null) return 1f;
        int prev = 1;
        foreach (var u in coconutUnlocks)
            if (u != null && u.cycleRequired < next.cycleRequired && u.cycleRequired > prev) prev = u.cycleRequired;
        int span = next.cycleRequired - prev;
        if (span <= 0) return 1f;
        return Mathf.Clamp01((float)(billCycle - prev) / span);
    }

    [ContextMenu("Borrar cocos vistos (para probar el panel)")]
    void ClearSeenCoconuts()
    {
        for (int i = 0; i < 15; i++) PlayerPrefs.DeleteKey("coco_visto_" + i);
        PlayerPrefs.Save();
    }

    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained)
    {
        totalCoconutsKilled++;
        runCoconutsKilled++;
        if (runCoconutsKilled > bestRunKills) bestRunKills = runCoconutsKilled;
        dayCoconutsKilled++;
        float streakBonus = GetStreakWaterMultiplier() - 1f;
        float earlyBonus = GetEarlyDayMultiplier() - 1f;
        float waterExtraMult = Mathf.Max(0.1f, 1f + skillWaterMultiplierBonus + perkWaterMultiplierBonus + legacyWaterMultiplierBonus
            + relicWaterMultiplierBonus + PassiveValue(MachetePassive.WaterBonus) - curseWaterPenalty + streakBonus + earlyBonus);
        waterGained = (baseWaterPerKill + Random.Range(0f, waterVariance)) * lootMultiplier * waterExtraMult
            * legacyMultiplier * GetLastBreathMultiplier() * perkWaterMultiplicative;
        float jackpotChance = skillJackpotChance + legacyJackpotChance + PassiveValue(MachetePassive.JackpotChance);
        lastKillHadJackpot = jackpotChance > 0f && Random.value < jackpotChance;
        if (lastKillHadJackpot)
        {
            float jackpotWater = waterGained * (jackpotBaseMultiplier + skillJackpotMultBonus);
            waterGained += jackpotWater;
            Log("¡JACKPOT DE AGUA! +" + FormatWater(jackpotWater));
        }
        waterCurrentML += waterGained;
        int gainedRounded = Mathf.RoundToInt(waterGained);
        runMoneyEarned += gainedRounded;
        dayMoneyEarned += gainedRounded;
        float recoveryCap = GetStaminaRecoveryCap();
        if (skillStaminaRecoveryChance > 0f && dayStaminaRecovered < recoveryCap
            && Random.value < skillStaminaRecoveryChance)
        {
            float amount = Mathf.Min(StaminaRecoveryBase + skillStaminaRecoveryAmount, recoveryCap - dayStaminaRecovered);
            stamina = Mathf.Min(GetStaminaMax(), stamina + amount);
            dayStaminaRecovered += amount;
            Log("¡Recuperaste " + amount.ToString("0.#") + " de resistencia!");
        }

        bool foundCoin = Random.value < coinDropChance;
        lastKillFoundCoin = foundCoin;
        if (foundCoin)
        {
            coconutCoins++;
            dayCoinsFound++;
            Log(Loc("log_coin_found", coconutCoins));
        }
        else if (!lastKillHadJackpot)
        {
            Log(Loc("log_money_earned", FormatWater(waterGained)));
        }
        if (skillExtraCoconutChance > 0f && currentPhase == Phase.Hitting
    && Random.value < skillExtraCoconutChance && CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.SpawnExtraCoconut();
        }
        return gainedRounded;
    }

    public int OnCoconutDestroyed(float lootMultiplier = 1f)
    {
        return OnCoconutDestroyed(lootMultiplier, out _);
    }

    void EnterRecaudacionPhase()
    {
        currentPhase = Phase.Shop;
        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.ClearAllCoconuts();
        BetaLog("day_end");
        SetCursorVisible(true);
        bool isDeliveryDay = daysLeft <= 1 && !skipNextDayDecrement;
        if (isDeliveryDay)
        {
            OpenForcedOrderPanel();
        }
        else
        {
            if (recaudacionPanel != null) recaudacionPanel.SetActive(true);
            if (deudaPanel != null) deudaPanel.SetActive(false);
            RefreshAllUI();
            OnRecaudacionShown?.Invoke();
        }
        AnnounceNewUnlocks();
        SaveGame();
    }

    void ShowHittingUI()
    {
        dayCoconutsKilled = 0;
        dayMoneyEarned = 0;
        daySwings = 0;
        dayHits = 0;
        dayCoinsFound = 0;
        currentHitStreak = 0;
        streakForgivesLeft = skillStreakForgives;
        dayStaminaRecovered = 0f;
        dayElapsed = 0f;
        lastBreathSwingActive = false;
        killsAtDayStart = totalCoconutsKilled;
        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(true);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);

        SetCursorVisible(false);

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.OnInitialSpawnComplete -= HandleInitialSpawnComplete;
            CoconutSpawner.Instance.OnInitialSpawnComplete += HandleInitialSpawnComplete;
            CoconutSpawner.Instance.StartNewDay();
        }
        else
        {
            ActivateHittingPhase();
        }
    }
    void HandleInitialSpawnComplete()
    {
        if (CoconutSpawner.Instance != null)
            CoconutSpawner.Instance.OnInitialSpawnComplete -= HandleInitialSpawnComplete;

        ActivateHittingPhase();
    }
    public float GetLastBreathMultiplier()
    {
        if (skillLastBreathBonus <= 0f || currentPhase != Phase.Hitting) return 1f;
        return stamina <= GetStaminaMax() * 0.2f ? 1f + skillLastBreathBonus : 1f;
    }
    void ActivateHittingPhase()
    {
        currentPhase = Phase.Hitting;
        if (machete != null) machete.SetActive(true);
        RecalculateAllStats();
        OnDayStarted?.Invoke();
    }
    public void ToggleUpgradeTreePanel()
    {
        if (forcedOrderDecision) return;
        bool willOpen = upgradeTreePanel != null && !upgradeTreePanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(upgradeTreePanel, new GameObject[] { tiendaPanel, rewardMachinePanel }, willOpen));
    }
    float GetRawSwingInterval(bool includeLastBreath)
    {
        MacheteData m = GetCurrentMachete();
        float baseSwing = m != null ? m.baseSwingInterval : 1.2f;
        return baseSwing - skillSwingIntervalReduction - perkSwingIntervalReduction - relicSwingIntervalReduction
             + curseSwingPenalty - (includeLastBreath && lastBreathSwingActive ? skillLastBreathSwingReduction : 0f);
    }
    public void ToggleTiendaPanel()
    {
        if (forcedOrderDecision) return;
        bool willOpen = tiendaPanel != null && !tiendaPanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(tiendaPanel, new GameObject[] { upgradeTreePanel, rewardMachinePanel }, willOpen));
    }
    float GetStaminaRecoveryCap()
    {
        if (skillStaminaRecoveryLevel <= 0) return 0f;
        return StaminaRecoveryCaps[Mathf.Min(skillStaminaRecoveryLevel, StaminaRecoveryCaps.Length) - 1];
    }
    public void ToggleRewardMachinePanel()
    {
        if (forcedOrderDecision) return;
        bool willOpen = rewardMachinePanel != null && !rewardMachinePanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(rewardMachinePanel, new GameObject[] { upgradeTreePanel, tiendaPanel }, willOpen));
    }

    IEnumerator SwitchPanels(GameObject panelToToggle, GameObject[] otherPanels, bool willOpen)
    {
        bool anyOtherWasOpen = false;
        if (otherPanels != null)
        {
            foreach (var p in otherPanels)
            {
                if (p != null && p.activeSelf)
                {
                    anyOtherWasOpen = true;
                    p.SetActive(false);
                }
            }
        }

        if (panelToToggle != null && !willOpen) panelToToggle.SetActive(false);

        if (willOpen && anyOtherWasOpen)
        {
            yield return new WaitForSecondsRealtime(panelSwitchBreak);
        }

        if (willOpen && panelToToggle != null)
        {
            panelToToggle.SetActive(true);
            if (panelToToggle == upgradeTreePanel) RecenterUpgradeTree();
        }

        RefreshAllUI();
    }

    public float GetExecuteDamageMultiplier(float targetHpPercent)
    {
        return (skillExecuteBonus > 0f && targetHpPercent < executeHpThreshold) ? 1f + skillExecuteBonus : 1f;
    }
    public float GetMultiHitMultiplier(int targetsHit)
    {
        float bonus = skillMultiHitBonus + PassiveValue(MachetePassive.MultiHit);
        return (bonus > 0f && targetsHit >= 2) ? 1f + bonus : 1f;
    }

    float GetEarlyDayMultiplier()
    {
        return (skillEarlyDayBonus > 0f && currentPhase == Phase.Hitting && dayElapsed < earlyDayDuration)
            ? 1f + skillEarlyDayBonus : 1f;
    }
    void SaveGame()
    {
        SaveData d = new SaveData
        {
            legacyPoints = legacyPoints,
            totalLegacyPointsEarned = totalLegacyPointsEarned,
            coconutCoins = coconutCoins,
            bestRunKills = bestRunKills,
            bestCycleReached = bestCycleReached,
            purchasedLegacyItems = new List<string>(purchasedLegacyItems)
        };
        SaveSystem.Save(d);
    }

    void LoadGame()
    {
        SaveData d = SaveSystem.Load();
        if (d == null) return;

        legacyPoints = d.legacyPoints;
        totalLegacyPointsEarned = d.totalLegacyPointsEarned;
        coconutCoins = d.coconutCoins;
        bestRunKills = d.bestRunKills;
        bestCycleReached = Mathf.Max(1, d.bestCycleReached);
        legacyDamageBonus = 0f; legacyStaminaBonus = 0f; legacyWaterMultiplierBonus = 0f; legacyCritChanceBonus = 0f; legacyStartingCoconuts = 0; legacyMultiplierBonus = 0f; legacyCoinChanceBonus = 0f; legacyExtraDays = 0; legacyStartingWater = 0f; legacyJackpotChance = 0f; legacyStreakStacks = 0;
        legacyHitRadiusBonus = 0f; legacyOrderDiscount = 0f; legacyStartingMachete = 0;
        equippedMacheteIndex = 0;
        coinDropChance = 0.005f;
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts = 4;

        purchasedLegacyItems.Clear();
        if (d.purchasedLegacyItems != null)
        {
            foreach (string name in d.purchasedLegacyItems)
            {
                LegacyItem item = GetLegacyItem(name);
                if (item == null) continue;
                purchasedLegacyItems.Add(name);
                ApplyLegacyEffect(item);
            }
        }
        RecalcLegacyMultiplier();
        RecalculateAllStats();
    }

    void OnApplicationQuit() { SaveGame(); }
    void OnApplicationPause(bool paused) { if (paused) SaveGame(); }

    [ContextMenu("Borrar guardado")]
    void DeleteSave() { SaveSystem.Delete(); }
    public void ToggleDeudaPanel()
    {
        if (forcedOrderDecision) return;
        if (deudaPanel != null) deudaPanel.SetActive(!deudaPanel.activeSelf);
        RefreshAllUI();
    }
    public void PayDebt()
    {
        if (currentPhase != Phase.Shop) return;
        if (waterCurrentML < waterTargetML) return;
        forcedOrderDecision = false;
        if (deudaPanel != null) deudaPanel.SetActive(false);
        float delivered = waterTargetML;
        waterCurrentML -= delivered;
        float refund = delivered * skillOrderRefund;
        waterCurrentML += refund;
        int deliveredCycle = billCycle;
        billCycle++;
        bestCycleReached = Mathf.Max(bestCycleReached, billCycle);
        float growth = billCycle > lateGrowthFromCycle ? waterTargetGrowthLate : waterTargetGrowth;
        waterTargetML *= growth;
        daysLeft = Mathf.Max(3 + legacyExtraDays, GetDayLimitBase() - ((billCycle - 1) / 2));
        RecalculateAllStats();
        Log("Pedido entregado. Nuevo pedido: " + FormatWater(waterTargetML)
            + (refund > 0f ? " (reembolso +" + FormatWater(refund) + ")" : ""));
        BetaLog("order_paid");
        CheckVictory(deliveredCycle);
        RefreshAllUI();
        QueuePerkOffer();
    }
    void QueuePerkOffer()
    {
        pendingPerkOffers++;

        if (currentPhase != Phase.PerkChoice)
        {
            OfferPerkChoice();
        }
    }

    public void ContinueToNextDay()
    {
        if (skipNextDayDecrement)
        {
            skipNextDayDecrement = false;
        }
        else
        {
            if (daysLeft <= 1)
            {
                OpenForcedOrderPanel();
                return;
            }
            daysLeft--;
        }

        stamina = GetStaminaMax();
        ShowHittingUI();
        RefreshAllUI();
    }

    void OpenForcedOrderPanel()
    {
        forcedOrderDecision = true;
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(true);
        Log("Se acabó el tiempo: paga el pedido o declara bancarrota.");
        RefreshAllUI();
    }

    public void DeclareBankruptcy()
    {
        if (!forcedOrderDecision) return;
        forcedOrderDecision = false;
        if (deudaPanel != null) deudaPanel.SetActive(false);
        ResolveBankruptcy();
        RefreshAllUI();
    }

    void ResolveBankruptcy()
    {
        BetaLog("bankruptcy");
        ClearCurse();
        if (machete != null) machete.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        pendingPerkOffers = 0;

        int gained = Mathf.CeilToInt(billCycle / 2f);
        if (billCycle <= recordAtRunStart)
            gained = totalLegacyPointsEarned < 5 ? 1 : 0;
        legacyPoints += gained;
        totalLegacyPointsEarned += gained;
        RecalcLegacyMultiplier();

        if (bankruptcyMessageText != null) bankruptcyMessageText.text = Loc("log_bankruptcy", gained);
        Log(Loc("log_bankruptcy", gained));

        ResetRunState();
        EnterBetweenRunsPhase();
        SaveGame();
        OnRunReset?.Invoke();
    }

    void EnterBetweenRunsPhase()
    {
        currentPhase = Phase.BetweenRuns;
        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(true);

        SetCursorVisible(true);

        RefreshAllUI();
    }

    public void ContinueAfterBankruptcy()
    {
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        BeginNewGame();
    }
    public MacheteData GetMacheteData(int index)
    {
        if (index < 0 || index >= macheteOptions.Count) return null;
        return macheteOptions[index];
    }

    public MacheteData GetCurrentMachete() => GetMacheteData(equippedMacheteIndex);
    public MacheteData GetNextMachete() => GetMacheteData(equippedMacheteIndex + 1);

    public void BuyNextMachete()
    {
        MacheteData next = GetNextMachete();
        if (next == null) return;

        if (waterCurrentML < next.unlockCost) return;
        waterCurrentML -= next.unlockCost;
        equippedMacheteIndex++;
        BetaLog("machete_" + equippedMacheteIndex);
        RecalculateAllStats();
        Log(Loc("log_new_machete", next.macheteName));
        RefreshAllUI();
    }

    public SkillNode GetSkillNode(string id) => skillTree.Find(n => n.id == id);

    public bool IsSkillUnlocked(string id) => unlockedSkillIds.Contains(id);

    public bool CanUnlockSkillPublic(string id)
    {
        SkillNode node = GetSkillNode(id);
        return node != null && CanUnlockSkill(node);
    }

    bool CanUnlockSkill(SkillNode node)
    {
        if (unlockedSkillIds.Contains(node.id)) return false;
        if (waterCurrentML < node.cost) return false;

        if (node.prerequisiteIds != null)
        {
            foreach (var pre in node.prerequisiteIds)
            {
                string trimmedPre = string.IsNullOrEmpty(pre) ? pre : pre.Trim();
                if (!string.IsNullOrEmpty(trimmedPre) && !unlockedSkillIds.Contains(trimmedPre)) return false;
            }
        }
        return true;
    }

    public void UnlockSkill(string id)
    {
        SkillNode node = GetSkillNode(id);
        if (node == null || !CanUnlockSkill(node)) return;

        waterCurrentML -= node.cost;
        unlockedSkillIds.Add(id);
        ApplySkillEffect(node);
        BetaLog("skill_" + id);
        Log(Loc("log_upgrade_bought", node.nodeName));
        RefreshAllUI();
    }

    void ApplySkillEffect(SkillNode node)
    {
        switch (node.effect)
        {
            case SkillEffect.DamageFlatBonus: skillDamageBonus += node.effectValue; break;
            case SkillEffect.StaminaMaxFlatBonus: skillStaminaBonus += node.effectValue; break;
            case SkillEffect.SwingIntervalReduction: skillSwingIntervalReduction += node.effectValue; break;
            case SkillEffect.HitRadiusBonus: skillHitRadiusBonus += node.effectValue; break;
            case SkillEffect.MoneyMultiplierBonus: skillWaterMultiplierBonus += node.effectValue; break;
            case SkillEffect.WaterMultiplierBonus: skillWaterMultiplierBonus += node.effectValue; break;
            case SkillEffect.ExtraStartingCoconut:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(node.effectValue);
                break;
            case SkillEffect.SpawnIntervalReduction:
                if (CoconutSpawner.Instance != null)
                    CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - node.effectValue); break;
            case SkillEffect.CoinChanceBonus: coinDropChance += node.effectValue; break;
            case SkillEffect.CritChanceBonus: skillCritChanceBonus += node.effectValue; break;
            case SkillEffect.JackpotChanceBonus: skillJackpotChance += node.effectValue; break;
            case SkillEffect.JackpotWaterBonus: skillJackpotMultBonus += node.effectValue; break;
            case SkillEffect.CritDamageBonus: skillCritDamageBonus += node.effectValue; break;
            case SkillEffect.ExtraCoconutChanceBonus: skillExtraCoconutChance = Mathf.Min(0.5f, skillExtraCoconutChance + node.effectValue); break;
            case SkillEffect.StreakForgiveness: skillStreakForgives += Mathf.RoundToInt(node.effectValue); streakForgivesLeft += Mathf.RoundToInt(node.effectValue); break;
            case SkillEffect.OrderDiscount: waterTargetML *= 1f - node.effectValue; break;
            case SkillEffect.LastBreathBonus: skillLastBreathBonus += node.effectValue; break;
            case SkillEffect.ChainSplash: skillChainSplashPercent += node.effectValue; break;
            case SkillEffect.LastBreathSwingBonus: skillLastBreathSwingReduction += node.effectValue; break;
            case SkillEffect.ExecuteDamageBonus: skillExecuteBonus += node.effectValue; break;
            case SkillEffect.OrderRefundBonus: skillOrderRefund += node.effectValue; break;
            case SkillEffect.EarlyDayWaterBonus: skillEarlyDayBonus += node.effectValue; break;
            case SkillEffect.MultiHitDamageBonus: skillMultiHitBonus += node.effectValue; break;
            case SkillEffect.StreakMaxStacksBonus: skillStreakStackBonus += Mathf.RoundToInt(node.effectValue); break;
            case SkillEffect.StreakPowerBonus: skillStreakPowerBonus += node.effectValue; break;
            case SkillEffect.StaminaRecoveryChanceBonus: skillStaminaRecoveryChance = Mathf.Min(0.35f, skillStaminaRecoveryChance + node.effectValue); skillStaminaRecoveryLevel++; break;
            case SkillEffect.StaminaRecoveryAmountBonus: skillStaminaRecoveryAmount += node.effectValue; break;
        }
        RecalculateAllStats();
    }

    void OfferPerkChoice()
    {
        currentPhase = Phase.PerkChoice;
        isChoosingPerk = false;
        if (machete != null) machete.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(true);

        SetCursorVisible(true);

        currentPerkChoices = PickRandomPerks(3);
        if (currentPerkChoices[0] == null)
        {
            pendingPerkOffers = 0;
            if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
            ShowRecaudacionAfterPerk();
            return;
        }
        SetPerkLabel(perkOption1Name, perkOption1Desc, perkOption1Icon, currentPerkChoices[0]);
        SetPerkLabel(perkOption2Name, perkOption2Desc, perkOption2Icon, currentPerkChoices[1]);
        SetPerkLabel(perkOption3Name, perkOption3Desc, perkOption3Icon, currentPerkChoices[2]);
        if (perkCard1 != null) perkCard1.gameObject.SetActive(currentPerkChoices[0] != null);
        if (perkCard2 != null) perkCard2.gameObject.SetActive(currentPerkChoices[1] != null);
        if (perkCard3 != null) perkCard3.gameObject.SetActive(currentPerkChoices[2] != null);
        if (perkCard1 != null) perkCard1.PlayAppear(0f);
        if (perkCard2 != null) perkCard2.PlayAppear(perkCardStagger);
        if (perkCard3 != null) perkCard3.PlayAppear(perkCardStagger * 2f);
    }
    void ShowRecaudacionAfterPerk()
    {
        skipNextDayDecrement = true;
        currentPhase = Phase.Shop;
        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(true);

        SetCursorVisible(true);
        RefreshAllUI();
        OnRecaudacionShown?.Invoke();
        AnnounceNewUnlocks();
        SaveGame();
    }
    void SetPerkLabel(TMP_Text nameLabel, TMP_Text descLabel, Image iconImage, PerkOption perk)
    {
        if (perk == null) return;
        if (nameLabel != null) nameLabel.text = perk.perkName;
        if (descLabel != null) descLabel.text = perk.description;
        if (iconImage != null)
        {
            Sprite s = GetPerkIcon(perk);
            iconImage.sprite = s;
            iconImage.enabled = s != null;
            iconImage.preserveAspect = true;
        }
    }

    PerkOption[] PickRandomPerks(int count)
    {
        List<PerkOption> pool = perkPool.FindAll(p =>
        {
            int min = PerkMinCycle.TryGetValue(p.perkName, out int mn) ? mn : 1;
            int max = PerkMaxCycle.TryGetValue(p.perkName, out int mx) ? mx : int.MaxValue;
            bool cycleOk = billCycle >= min && billCycle < max;
            bool copiesOk = !perkCopies.ContainsKey(p.perkName) || perkCopies[p.perkName] < maxCopiesPerPerk;
            if (p.effect == PerkEffect.SwingIntervalReduction && GetRawSwingInterval(false) - p.value < SwingFloor) return false;
            if (!StreakUnlocked && (p.effect == PerkEffect.StreakMaxStacksBonus || p.effect == PerkEffect.StreakPowerBonus)) return false;
            return cycleOk && copiesOk;
        });
        PerkOption[] result = new PerkOption[count];
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) { result[i] = null; continue; }
            int idx = Random.Range(0, pool.Count);
            result[i] = pool[idx];
            pool.RemoveAt(idx);
        }
        return result;
    }
    public void ApplyChainSplash(Vector3 position, int killingDamage, GameObject source)
    {
        float pct = Mathf.Min(0.5f, skillChainSplashPercent + PassiveValue(MachetePassive.ChainSplash));
        if (pct <= 0f || isApplyingSplash) return;
        isApplyingSplash = true;
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(killingDamage * pct));
        float radius = MacheteController.Instance != null ? MacheteController.Instance.hitRadius : 1.5f;
        var already = new HashSet<CoconutTarget>();
        foreach (var col in Physics.OverlapSphere(position, radius))
        {
            var target = col.GetComponentInParent<CoconutTarget>();
            if (target == null || target.gameObject == source || !already.Add(target)) continue;
            target.TakeDamage(splashDamage);
        }
        isApplyingSplash = false;
    }
    public void ChoosePerk(int index)
    {
        if (isChoosingPerk) return;
        if (index < 0 || index >= currentPerkChoices.Length || currentPerkChoices[index] == null) return;
        StartCoroutine(ChoosePerkRoutine(index));
    }

    IEnumerator ChoosePerkRoutine(int index)
    {
        isChoosingPerk = true;

        PerkCardFX[] cards = { perkCard1, perkCard2, perkCard3 };
        float wait = 0f;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i == index) wait = Mathf.Max(wait, cards[i].PlaySelected());
            else cards[i].PlayDismiss();
        }
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        string pn = currentPerkChoices[index].perkName;
        perkCopies[pn] = perkCopies.ContainsKey(pn) ? perkCopies[pn] + 1 : 1;
        ApplyPerk(currentPerkChoices[index]);
        BetaLog("perk_" + pn);
        Log(Loc("log_perk_chosen", currentPerkChoices[index].perkName));

        if (pendingPerkOffers > 0) pendingPerkOffers--;

        if (pendingPerkOffers > 0)
        {
            OfferPerkChoice();
            yield break;
        }

        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);

        isChoosingPerk = false;
        ShowRecaudacionAfterPerk();
    }
    public void RestartRun()
    {
        BetaLog("run_restart");

        currentPhase = Phase.MainMenu;
        isChoosingPerk = false;
        pendingPerkOffers = 0;
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        skipNextDayDecrement = false;
        ClearCurse();

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.OnInitialSpawnComplete -= HandleInitialSpawnComplete;
            CoconutSpawner.Instance.ClearAllCoconuts();
        }

        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (indicacionesPanel != null) indicacionesPanel.SetActive(false);

        ResetRunState();
        displayedMoney = 0f;

        SaveGame();
        OnRunReset?.Invoke();
        forcedOrderDecision = false;
        GoToMainMenu();
    }
    void ApplyPerk(PerkOption perk)
    {
        switch (perk.effect)
        {
            case PerkEffect.DamageBonus: perkDamageBonus += perk.value; break;
            case PerkEffect.StaminaMaxBonus: perkStaminaMaxBonus += perk.value; break;
            case PerkEffect.MoneyMultiplierBonus: perkWaterMultiplierBonus += perk.value; break;
            case PerkEffect.SwingIntervalReduction: perkSwingIntervalReduction += perk.value; break;
            case PerkEffect.ExtraCoconutNextCycle: if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(perk.value); break;
            case PerkEffect.CritChanceBonus: perkCritChanceBonus += perk.value; break;
            case PerkEffect.WaterMultiplierBonus: perkWaterMultiplierBonus += perk.value; break;
            case PerkEffect.WaterForSwingPenalty: perkWaterMultiplicative *= 1f + perk.value; perkSwingIntervalReduction -= perk.value * 0.48f; break;
            case PerkEffect.HitRadiusBonus: skillHitRadiusBonus += perk.value; break;
            case PerkEffect.JackpotChanceBonus: skillJackpotChance += perk.value; break;
            case PerkEffect.StreakMaxStacksBonus: skillStreakStackBonus += Mathf.RoundToInt(perk.value); break;
            case PerkEffect.StreakPowerBonus: skillStreakPowerBonus += perk.value; break;
            case PerkEffect.OrderDiscount: waterTargetML *= 1f - perk.value; break;
            case PerkEffect.CoinChanceBonus: coinDropChance += perk.value; break;
            case PerkEffect.ExecuteDamageBonus: skillExecuteBonus += perk.value; break;
            case PerkEffect.ChainSplash: skillChainSplashPercent += perk.value; break;
            case PerkEffect.LastBreathBonus: skillLastBreathBonus += perk.value; break;
            case PerkEffect.EarlyDayWaterBonus: skillEarlyDayBonus += perk.value; break;
            case PerkEffect.MultiHitDamageBonus: skillMultiHitBonus += perk.value; break;
            case PerkEffect.DamageForStaminaPenalty: perkDamageBonus += perk.value; perkStaminaMaxBonus -= perk.value * 0.5f; break;
        }
        RecalculateAllStats();
    }

    public LegacyItem GetLegacyItem(string itemName) => legacyShop.Find(i => i.itemName == itemName);

    public bool IsLegacyItemPurchased(string itemName) => purchasedLegacyItems.Contains(itemName);

    public void BuyLegacyItem(string itemName)
    {
        if (currentPhase != Phase.BetweenRuns) return;

        LegacyItem item = GetLegacyItem(itemName);
        if (item == null) return;
        if (purchasedLegacyItems.Contains(itemName)) return;
        if (legacyPoints < item.cost) return;
        string prev = PreviousLegacyTier(itemName);
        if (prev != null && !purchasedLegacyItems.Contains(prev)) { Log("Primero necesitas: " + prev); return; }
        legacyPoints -= item.cost;
        purchasedLegacyItems.Add(itemName);
        ApplyLegacyEffect(item);
        Log(Loc("log_legacy_bought", item.itemName));
        RefreshAllUI();
        SaveGame();
    }
    void ResetRunState()
    {
        money = 0;
        billCycle = 1;
        waterCurrentML = legacyStartingWater;
        waterTargetML = waterTargetStart * (1f - legacyOrderDiscount);
        daysLeft = GetDayLimitBase();
        totalCoconutsKilled = 0;
        unlockedSkillIds.Clear();
        killsAtDayStart = 0;
        announcedUnlocks.Clear();
        skillDamageBonus = 0f;
        skillStaminaBonus = 0f;
        skillSwingIntervalReduction = 0f;
        skillHitRadiusBonus = 0f;
        skillWaterMultiplierBonus = 0f;
        skillStaminaRecoveryChance = 0f;
        skillStaminaRecoveryAmount = 0f;
        skillStaminaRecoveryLevel = 0;
        skillJackpotChance = 0f;
        skillJackpotMultBonus = 0f;
        skillCritDamageBonus = 0f;
        skillCritChanceBonus = 0f;
        skillExtraCoconutChance = 0f;
        skillStreakForgives = 0;
        streakForgivesLeft = 0;
        skillStreakStackBonus = 0;
        skillStreakPowerBonus = 0f;
        skillLastBreathBonus = 0f;
        skillLastBreathSwingReduction = 0f;
        skillExecuteBonus = 0f;
        skillMultiHitBonus = 0f;
        skillOrderRefund = 0f;
        skillEarlyDayBonus = 0f;
        skillChainSplashPercent = 0f;

        equippedMacheteIndex = legacyStartingMachete;
        currentHitStreak = 0;

        perkWaterMultiplicative = 1f;
        perkDamageBonus = 0f;
        perkStaminaMaxBonus = 0f;
        perkWaterMultiplierBonus = 0f;
        perkSwingIntervalReduction = 0f;
        perkCritChanceBonus = 0f;
        perkCopies.Clear();

        relicDamageBonus = 0f;
        relicStaminaMaxBonus = 0f;
        relicWaterMultiplierBonus = 0f;
        relicSwingIntervalReduction = 0f;
        relicHitRadiusBonus = 0f;
        relicCritChanceBonus = 0f;

        coinDropChance = 0.005f + legacyCoinChanceBonus;
        runVictoryAwarded = false;

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.startingCoconuts = 4 + legacyStartingCoconuts;
            CoconutSpawner.Instance.spawnInterval = CoconutSpawner.BaseSpawnInterval;
        }
        forcedOrderDecision = false;
        RecalculateAllStats();
    }
    void ApplyLegacyEffect(LegacyItem item)
    {
        switch (item.effect)
        {
            case LegacyEffect.PermanentDamageBonus: legacyDamageBonus += item.value; break;
            case LegacyEffect.PermanentStaminaMaxBonus: legacyStaminaBonus += item.value; break;
            case LegacyEffect.PermanentMoneyMultiplierBonus: legacyWaterMultiplierBonus += item.value; break;
            case LegacyEffect.PermanentCritChanceBonus: legacyCritChanceBonus += item.value; break;
            case LegacyEffect.PermanentStartingCoconutBonus: legacyStartingCoconuts += Mathf.RoundToInt(item.value); if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(item.value); break;
            case LegacyEffect.PermanentLegacyMultiplierBonus: legacyMultiplierBonus += item.value; RecalcLegacyMultiplier(); break;
            case LegacyEffect.PermanentCoinChanceBonus: legacyCoinChanceBonus += item.value; coinDropChance += item.value; break;
            case LegacyEffect.PermanentExtraDay: legacyExtraDays += Mathf.RoundToInt(item.value); break;
            case LegacyEffect.PermanentStartingWater: legacyStartingWater += item.value; break;
            case LegacyEffect.PermanentJackpotChance: legacyJackpotChance += item.value; break;
            case LegacyEffect.PermanentStreakStacks: legacyStreakStacks += Mathf.RoundToInt(item.value); break;
            case LegacyEffect.PermanentHitRadius: legacyHitRadiusBonus += item.value; break;
            case LegacyEffect.PermanentOrderDiscount: legacyOrderDiscount += item.value; break;
            case LegacyEffect.PermanentStartingMachete: legacyStartingMachete += Mathf.RoundToInt(item.value); equippedMacheteIndex = Mathf.Max(equippedMacheteIndex, legacyStartingMachete); break;
        }
        RecalculateAllStats();
    }
    public void Log(string msg)
    {
        if (logText != null) logText.text = msg;
    }

    void RefreshAllUI()
    {
        if (indicacionesPanel != null)
        {
            bool showIndicaciones = (upgradeTreePanel != null && upgradeTreePanel.activeSelf)
                      || (tiendaPanel != null && tiendaPanel.activeSelf)
                      || (rewardMachinePanel != null && rewardMachinePanel.activeSelf);
            indicacionesPanel.SetActive(showIndicaciones);
        }
        UpdateMoneyUI();

        if (runKillsText != null) runKillsText.text = Loc("collection_coconuts_killed", dayCoconutsKilled);
        if (runMoneyText != null) runMoneyText.text = Loc("collection_money_earned", FormatWater(dayMoneyEarned));

        if (accuracyText != null)
        {
            float accuracyPercent = daySwings > 0 ? (dayHits / (float)daySwings) * 100f : 0f;
            accuracyText.text = Mathf.RoundToInt(accuracyPercent) + "% de punteria";
        }

        if (dayCoinsText != null)
        {
            bool gotCoinsToday = dayCoinsFound > 0;
            dayCoinsText.gameObject.SetActive(gotCoinsToday);
            if (gotCoinsToday)
            {
                dayCoinsText.text = "+" + dayCoinsFound + (dayCoinsFound == 1 ? " moneda encontrada" : " monedas encontradas");
            }
        }

        CoconutUnlockThreshold nextUnlock = GetNextLockedCoconut();
        float unlockProgress = GetNextUnlockProgress01();

        if (nextUnlockImage != null)
        {
            bool showImage = nextUnlock != null;
            nextUnlockImage.gameObject.SetActive(showImage);
            if (showImage)
            {
                if (nextUnlock.icon != null) nextUnlockImage.sprite = nextUnlock.icon;
                Color c = nextUnlockImage.color;
                c.a = Mathf.Lerp(lockedImageMinAlpha, 1f, unlockProgress);
                nextUnlockImage.color = c;
            }
        }
        if (nextUnlockPercentText != null)
        {
            bool showText = nextUnlock != null;
            nextUnlockPercentText.gameObject.SetActive(showText);
            if (showText) nextUnlockPercentText.text = "Pedido " + nextUnlock.cycleRequired;
        }

        if (deudaMoneyText != null) deudaMoneyText.text = Loc("collection_current_money", FormatWater(waterCurrentML));

        bool readyToDeliver = waterCurrentML >= waterTargetML;

        if (deudaBillText != null)
        {
            if (readyToDeliver) deudaBillText.text = "¡Pedido listo! Entregalo";
            else
            {
                float remaining = Mathf.Max(0f, waterTargetML - waterCurrentML);
                deudaBillText.text = "Faltan " + FormatWater(remaining);
            }
        }

        if (deudaDaysLeftText != null)
        {
            int daysToPlay = DaysToPlay;
            if (forcedOrderDecision)
            {
                deudaDaysLeftText.text = readyToDeliver ? "¡ÚLTIMO DÍA! Paga o bancarrota" : "¡ÚLTIMO DÍA! No alcanza: bancarrota";
                deudaDaysLeftText.color = urgentColor;
            }
            else if (!readyToDeliver && daysToPlay <= 1)
            {
                deudaDaysLeftText.text = "¡ÚLTIMO DÍA PARA JUNTAR EL AGUA!";
                deudaDaysLeftText.color = urgentColor;
            }
            else
            {
                deudaDaysLeftText.text = daysToPlay + (daysToPlay == 1 ? " día restante" : " días restantes");
                deudaDaysLeftText.color = normalDaysColor;
            }
        }

        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(forcedOrderDecision);

        if (payDebtButton != null) payDebtButton.interactable = readyToDeliver;

        MacheteData current = GetCurrentMachete();
        MacheteData next = GetNextMachete();
        if (macheteStatusText != null)
            macheteStatusText.text = Loc("shop_current_machete", current != null ? current.macheteName : "-") + PassiveLine(current);
        if (next != null)
        {
            if (macheteUpgradeCostText != null)
                macheteUpgradeCostText.text = Loc("shop_next_machete", next.macheteName, FormatWater(next.unlockCost))
                            + StatsGainLine(current, next) + PassiveLine(next);
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = waterCurrentML >= next.unlockCost;
        }
        else
        {
            if (macheteUpgradeCostText != null) macheteUpgradeCostText.text = Loc("shop_max_machete");
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = false;
        }
        if (coinCountText != null) coinCountText.text = Loc("collection_coins", coconutCoins);

        if (legacyPointsText != null) legacyPointsText.text = Loc("betweenruns_legacy_points", legacyPoints);
    }

    void CheckVictory(int deliveredCycle)
    {
        if (runVictoryAwarded || deliveredCycle < victoryCycle) return;
        runVictoryAwarded = true;
        legacyPoints += victoryLegacyBonus;
        totalLegacyPointsEarned += victoryLegacyBonus;
        RecalcLegacyMultiplier();
        Log("¡COMPLETASTE EL CICLO " + victoryCycle + "! +" + victoryLegacyBonus + " puntos de legado. Sigue en modo infinito.");
        BetaLog("VICTORY");
        SaveGame();
    }
    void BetaLog(string evt)
    {
        try
        {
            string line = System.DateTime.Now.ToString("HH:mm:ss") + "," + evt
                + ",run_t=" + (Time.time - runStartTime).ToString("F0")
                + ",cycle=" + billCycle + ",days=" + daysLeft
                + ",water=" + waterCurrentML.ToString("F0") + ",target=" + waterTargetML.ToString("F0")
                + ",machete=" + equippedMacheteIndex + ",skills=" + unlockedSkillIds.Count
                + ",swings_per_kill=" + (dayCoconutsKilled > 0 ? (daySwings / (float)dayCoconutsKilled).ToString("F1") : "0")
                + ",kills=" + totalCoconutsKilled + "\n";
            System.IO.File.AppendAllText(System.IO.Path.Combine(Application.persistentDataPath, "beta_log.csv"), line);
        }
        catch { }
    }
    void EnsureDefaultData()
    {
        static SkillNode N(string id, string name, string desc, int cost, SkillEffect effect, float value, params string[] pre)
        {
            return new SkillNode { id = id, nodeName = name, description = desc, cost = cost, prerequisiteIds = pre, effect = effect, effectValue = value };
        }
        if (forceDefaultData || macheteOptions == null || macheteOptions.Count == 0)
        {
            macheteOptions = new List<MacheteData>
{
    new MacheteData { macheteName = "Machete Oxidado",      description = "El que ya tienes.",                     baseDamage = 5f,   baseSwingInterval = 1.2f,  baseHitRadius = .8f,  unlockCost = 0 },
    new MacheteData { macheteName = "Machete Normal",       description = "Mas daño, mas rapido, mas rango.",      baseDamage = 9f,   baseSwingInterval = 1.1f,  baseHitRadius = 1.2f,  unlockCost = 2500 },
    new MacheteData { macheteName = "Machete de Acero",     description = "Un salto grande de poder.",             baseDamage = 14f,  baseSwingInterval = 1.05f, baseHitRadius = 1.5f, unlockCost = 10000,
        passive = MachetePassive.StaminaMax,   passiveValue = 6f,    passiveDescription = "Acero Templado: +6 de resistencia maxima." },
    new MacheteData { macheteName = "Machete de Oro",       description = "Brilla y pega fuerte.",                 baseDamage = 22f,  baseSwingInterval = 1.0f,  baseHitRadius = 1.8f,  unlockCost = 30000,
        passive = MachetePassive.WaterBonus,   passiveValue = 0.15f, passiveDescription = "Toque Dorado: +15% de agua por coco." },
    new MacheteData { macheteName = "Machete de Obsidiana", description = "Corta como el vidrio. Pega muy duro.", baseDamage = 35f,  baseSwingInterval = 0.95f, baseHitRadius = 2f, unlockCost = 80000,
        passive = MachetePassive.CritChance,   passiveValue = 0.10f, passiveDescription = "Filo de Vidrio: +10% de probabilidad de critico." },
    new MacheteData { macheteName = "Machete de Maestro",   description = "La obra final del herrero.",            baseDamage = 50f,  baseSwingInterval = 0.9f,  baseHitRadius = 2.1f,  unlockCost = 200000,
        passive = MachetePassive.ChainSplash,  passiveValue = 0.25f, passiveDescription = "Obra Maestra: al matar un coco, los cercanos reciben 25% del daño." },
    new MacheteData { macheteName = "Machete Volcanico",    description = "Forjado en lava. Los criticos arden.",   baseDamage = 70f,  baseSwingInterval = 0.88f, baseHitRadius = 2.2f,  unlockCost = 450000,
        passive = MachetePassive.CritDamage,   passiveValue = 0.5f,  passiveDescription = "Brasas: los criticos pegan +0.5x mas." },
    new MacheteData { macheteName = "Machete Abisal",       description = "Viene de lo mas hondo del coco.",        baseDamage = 95f,  baseSwingInterval = 0.86f, baseHitRadius = 2.4f,  unlockCost = 1000000,
        passive = MachetePassive.JackpotChance, passiveValue = 0.08f, passiveDescription = "Sed del Abismo: +8% de probabilidad de Jackpot." },
    new MacheteData { macheteName = "Machete Ancestral",    description = "El primer machete. Resuena en todos.",   baseDamage = 130f, baseSwingInterval = 0.84f, baseHitRadius = 2.5f,  unlockCost = 2500000,
        passive = MachetePassive.MultiHit,     passiveValue = 0.25f, passiveDescription = "Eco Ancestral: +25% de daño al golpear 2 o mas cocos a la vez." },
};
        }

        if (forceDefaultData || skillTree == null || skillTree.Count == 0)
        {
            skillTree = new List<SkillNode>
        {
            N("fuerza_1", "Mas Fuerza I",   "+2 de daño. Abre el resto del arbol.", 50,    SkillEffect.DamageFlatBonus, 2f),
            N("fuerza_2", "Mas Fuerza II",  "+3 de daño",  900,   SkillEffect.DamageFlatBonus, 3f,  "fuerza_1"),
            N("fuerza_3", "Mas Fuerza III", "+5 de daño",  4000,  SkillEffect.DamageFlatBonus, 5f,  "fuerza_2"),
            N("fuerza_4", "Mas Fuerza IV",  "+8 de daño",  15000, SkillEffect.DamageFlatBonus, 8f,  "fuerza_3"),
            N("fuerza_5", "Mas Fuerza V",   "+12 de daño", 40000, SkillEffect.DamageFlatBonus, 12f, "fuerza_4"),
            N("fuerza_6", "Mas Fuerza VI",  "+15 de daño", 90000,  SkillEffect.DamageFlatBonus, 15f, "fuerza_5"),
            N("fuerza_7", "Mas Fuerza VII", "+25 de daño", 160000, SkillEffect.DamageFlatBonus, 25f, "fuerza_6"),

            N("velocidad_1", "Manos Rapidas I",   "Golpea 0.1s mas rapido", 150,   SkillEffect.SwingIntervalReduction, 0.10f, "fuerza_1"),
            N("velocidad_2", "Manos Rapidas II",  "Golpea 0.1s mas rapido", 1300,  SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_1"),
            N("velocidad_3", "Manos Rapidas III", "Golpea 0.1s mas rapido", 25000, SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_2"),
            N("velocidad_4", "Manos Rapidas IV",  "Golpea 0.1s mas rapido", 45000, SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_3"),
            N("velocidad_5", "Manos Rapidas V",   "Golpea 0.1s mas rapido", 80000, SkillEffect.SwingIntervalReduction, 0.10f, "velocidad_4"),

            N("suerte_1", "Buen Ojo I",  "+10% de agua por coco", 500,  SkillEffect.MoneyMultiplierBonus, 0.10f, "fuerza_2"),
            N("suerte_2", "Buen Ojo II", "+15% de agua por coco", 5000, SkillEffect.MoneyMultiplierBonus, 0.15f,  "suerte_1"),
            N("suerte_3", "Buen Ojo III", "+20% de agua por coco", 15000, SkillEffect.WaterMultiplierBonus, 0.20f, "suerte_2"),
            N("suerte_4", "Buen Ojo IV", "+25% de agua por coco", 60000, SkillEffect.WaterMultiplierBonus, 0.25f, "suerte_3"),
            N("suerte_5", "Buen Ojo V", "+30% de agua por coco", 100000, SkillEffect.WaterMultiplierBonus, 0.3f, "suerte_4"),

            N("radio_1", "Golpe Amplio I",  "Mas radio de golpe",     1200, SkillEffect.HitRadiusBonus, 0.2f, "velocidad_1"),
            N("radio_2", "Golpe Amplio II", "Aun mas radio de golpe", 2200, SkillEffect.HitRadiusBonus, 0.3f, "radio_1"),
            N("radio_3", "Golpe Amplio III", "Mas radio de golpe", 10000,  SkillEffect.HitRadiusBonus, 0.20f, "protegida_2"),
            N("radio_4", "Golpe Amplio IV",  "Mas radio de golpe", 20000, SkillEffect.HitRadiusBonus, 0.25f, "radio_3"),
            N("radio_5", "Golpe Amplio V",   "Mas radio de golpe", 35000, SkillEffect.HitRadiusBonus, 0.30f, "radio_4"),
            N("radio_6", "Golpe Amplio VI",  "Mas radio de golpe", 80000, SkillEffect.HitRadiusBonus, 0.35f, "radio_5"),

            N("multi_1", "Golpe Multiple I",   "+5% de daño si golpeas 2 o mas cocos a la vez",     15000,  SkillEffect.MultiHitDamageBonus, 0.05f, "radio_3"),
            N("multi_2", "Golpe Multiple II",  "+10% mas (total +15%) al golpear 2 o mas cocos",     40000, SkillEffect.MultiHitDamageBonus, 0.10f, "multi_1"),
            N("multi_3", "Golpe Multiple III", "+15% mas (total +30%) al golpear 2 o mas cocos",     90000, SkillEffect.MultiHitDamageBonus, 0.15f, "multi_2"),
            N("multi_4", "Golpe Multiple IV", "+20% mas (total +50%) al golpear 2 o mas cocos", 150000, SkillEffect.MultiHitDamageBonus, 0.2f, "multi_3"),
            N("multi_5", "Golpe Multiple V", "+25% mas (total +75%) al golpear 2 o mas cocos", 300000, SkillEffect.MultiHitDamageBonus, 0.25f, "multi_4"),

            N("eficiencia_1", "Segundo Aliento I",   "9% de probabilidad de recuperar 1 de resistencia al destruir un coco", 750,   SkillEffect.StaminaRecoveryChanceBonus, 0.09f, "resistencia_2"),
            N("eficiencia_2", "Segundo Aliento II",  "+8% de probabilidad (total 17%)", 3500,  SkillEffect.StaminaRecoveryChanceBonus, 0.08f, "eficiencia_1"),
            N("eficiencia_3", "Segundo Aliento III", "+7% de probabilidad (total 24%)", 9000,  SkillEffect.StaminaRecoveryChanceBonus, 0.07f, "eficiencia_2"),
            N("eficiencia_4", "Segundo Aliento IV",  "+6% de probabilidad (total 30%)", 22000, SkillEffect.StaminaRecoveryChanceBonus, 0.06f, "eficiencia_3"),
            N("eficiencia_5", "Segundo Aliento V",   "+5% de probabilidad (total 35%, el maximo)", 50000, SkillEffect.StaminaRecoveryChanceBonus, 0.05f, "eficiencia_4"),

            N("resistencia_1", "Aguante I",   "+5 segundos de resistencia", 450,   SkillEffect.StaminaMaxFlatBonus, 5f, "fuerza_1"),
            N("resistencia_2", "Aguante II",  "+5 segundos de resistencia", 1800,  SkillEffect.StaminaMaxFlatBonus, 5f, "resistencia_1"),
            N("resistencia_3", "Aguante III", "+5 segundos de resistencia", 4500,  SkillEffect.StaminaMaxFlatBonus, 5f, "resistencia_2"),
            N("resistencia_4", "Aguante IV",  "+5 segundos de resistencia", 12000, SkillEffect.StaminaMaxFlatBonus, 5f, "resistencia_3"),
            N("resistencia_5", "Aguante V",   "+6 segundos de resistencia", 60000, SkillEffect.StaminaMaxFlatBonus, 6f, "resistencia_4"),
            N("resistencia_6", "Aguante VI", "+8 segundos de resistencia", 150000, SkillEffect.StaminaMaxFlatBonus, 8f, "resistencia_5"),

            N("recuperacion_1", "Aliento Profundo I",   "Cada recuperacion da +1.6 de resistencia (total 2.6)", 1500,  SkillEffect.StaminaRecoveryAmountBonus, 1.6f, "eficiencia_1"),
            N("recuperacion_2", "Aliento Profundo II",  "+1.5 por recuperacion (total 4.1)",  5000,  SkillEffect.StaminaRecoveryAmountBonus, 1.5f, "recuperacion_1"),
            N("recuperacion_3", "Aliento Profundo III", "+1.4 por recuperacion (total 5.5)",  14000, SkillEffect.StaminaRecoveryAmountBonus, 1.4f, "recuperacion_2"),
            N("recuperacion_4", "Aliento Profundo IV",  "+1.3 por recuperacion (total 6.8)",  32000, SkillEffect.StaminaRecoveryAmountBonus, 1.3f, "recuperacion_3"),
            N("recuperacion_5", "Aliento Profundo V",   "+1.2 por recuperacion (total 8, el maximo)", 65000, SkillEffect.StaminaRecoveryAmountBonus, 1.2f, "recuperacion_4"),

            N("cocos_1", "Cosecha Inicial",     "+1 coco al iniciar el dia", 400,  SkillEffect.ExtraStartingCoconut, 1f, "fuerza_1"),
            N("cocos_2", "Mejores vendedores",  "+2 cocos al iniciar el dia", 950,  SkillEffect.ExtraStartingCoconut, 2f, "aparicion_1"),
            N("cocos_3", "Cocos locos",         "+3 cocos al iniciar el dia", 2300, SkillEffect.ExtraStartingCoconut, 3f, "aparicion_2"),
            N("cocos_4", "Cocos locos II", "+2 cocos al iniciar el dia", 20000, SkillEffect.ExtraStartingCoconut, 2f, "cocos_3"),

            N("aparicion_1", "Cosecha Rapida",     "DESBLOQUEA la aparicion de cocos durante el dia: aparece uno nuevo cada 10 s (antes solo salen los del inicio)", 600,  SkillEffect.SpawnIntervalReduction, 0f, "cocos_1"),
            N("aparicion_2", "Cosecha Rapidisima", "Los cocos aparecen mas seguido (-1 s entre cocos)", 1300, SkillEffect.SpawnIntervalReduction, 1f, "cocos_2"),
            N("aparicion_3", "Cosecha Veloz",      "Los cocos aparecen mas seguido (-2 s entre cocos)", 4000, SkillEffect.SpawnIntervalReduction,2f, "cocos_3"),
            N("aparicion_4", "Cosecha muy veloz",      "Los cocos aparecen mas seguido (-2 s entre cocos)", 10000, SkillEffect.SpawnIntervalReduction,2f, "aparicion_3"),
            N("aparicion_5", "Cosecha final", "Los cocos aparecen mas seguido (-1 s entre cocos)", 25000, SkillEffect.SpawnIntervalReduction, 1f, "aparicion_4"),

            N("leche_1", "Coco Bendito I",   "+10% de agua de coco", 500,   SkillEffect.WaterMultiplierBonus, 0.1f, "velocidad_2"),
            N("leche_2", "Coco Bendito II",  "+15% de agua de coco", 1500,  SkillEffect.WaterMultiplierBonus, 0.15f,  "leche_1"),
            N("leche_3", "Coco Bendito III", "+20% de agua de coco", 5000,  SkillEffect.WaterMultiplierBonus, 0.20f,  "leche_2"),
            N("leche_4", "Coco Bendito IV",  "+25% de agua de coco", 30000, SkillEffect.WaterMultiplierBonus, 0.25f,  "leche_3"),
            N("leche_5", "Coco Bendito V", "+30% de agua de coco", 90000, SkillEffect.WaterMultiplierBonus, 0.3f, "leche_4"),
            N("leche_6", "Coco Bendito VI", "+35% de agua de coco", 180000, SkillEffect.WaterMultiplierBonus, 0.35f, "leche_5"),

            N("moneda_1", "Golpe de Suerte I",   "+7% de probabilidad de Jackpot de Agua al destruir un coco", 1000,   SkillEffect.JackpotChanceBonus, 0.07f, "velocidad_2", "radio_1"),
            N("moneda_2", "Golpe de Suerte II",  "+4% de probabilidad de Jackpot (total 11%)",  9000,   SkillEffect.JackpotChanceBonus, 0.04f, "moneda_1", "radio_2"),
            N("moneda_3", "Golpe de Suerte III", "+6% de probabilidad de Jackpot (total 17%)",  14000,  SkillEffect.JackpotChanceBonus, 0.06f, "moneda_2"),
            N("moneda_4", "Golpe de Suerte IV",  "+8% de probabilidad de Jackpot (total 25%)",  30000,  SkillEffect.JackpotChanceBonus, 0.08f, "moneda_3"),
            N("moneda_5", "Golpe de Suerte V",   "+10% de probabilidad de Jackpot (total 35%)", 60000, SkillEffect.JackpotChanceBonus, 0.1f,  "moneda_4"),

            N("jackpot_1", "Botin de Jackpot I",  "El Jackpot da +1x mas agua (total x2.5 del coco)",   6000,  SkillEffect.JackpotWaterBonus, 1f,   "moneda_2"),
            N("jackpot_2", "Botin de Jackpot II", "El Jackpot da +1.5x mas agua (total x4 del coco)",   40000, SkillEffect.JackpotWaterBonus, 1.5f, "jackpot_1"),
            N("jackpot_3", "Botin de Jackpot III", "El Jackpot da +2x mas agua (total x6 del coco)", 110000, SkillEffect.JackpotWaterBonus, 2f, "jackpot_2", "moneda_5"),

            N("critico_1", "Golpe Certero I",   "+5% de probabilidad de golpe crítico.",  2000,  SkillEffect.CritChanceBonus, 0.05f, "leche_1"),
            N("critico_2", "Golpe Certero II",  "+7% de probabilidad de golpe crítico(total 12%)",  8000,  SkillEffect.CritChanceBonus, 0.07f, "critico_1"),
            N("critico_3", "Golpe Certero III", "+8% de probabilidad de golpe crítico(total 20%)",  25000, SkillEffect.CritChanceBonus, 0.08f, "critico_2"),
            N("critico_4", "Golpe Certero IV",  "+10% de probabilidad de golpe crítico(total 30%)", 80000, SkillEffect.CritChanceBonus, 0.10f, "critico_3"),
            N("critico_5", "Golpe Certero V",   "+10% de probabilidad de golpe crítico(total 40%)", 150000, SkillEffect.CritChanceBonus, 0.1f, "critico_4"),
            N("critico_6", "Golpe Certero VI", "+15% de probabilidad de golpe crítico(total 55%)", 300000, SkillEffect.CritChanceBonus, 0.15f, "critico_5"),

            N("critico_dano_1", "Golpe Demoledor I",   "Los criticos pegan +0.1x mas (de x1.3 a x1.4)", 6000,   SkillEffect.CritDamageBonus, 0.10f, "critico_2"),
            N("critico_dano_2", "Golpe Demoledor II",  "+0.15x mas (total x1.55)", 14000,  SkillEffect.CritDamageBonus, 0.15f, "critico_dano_1", "critico_3"),
            N("critico_dano_3", "Golpe Demoledor III", "+0.2x mas (total x1.75)",  30000,  SkillEffect.CritDamageBonus, 0.20f, "critico_dano_2"),
            N("critico_dano_4", "Golpe Demoledor IV",  "+0.3x mas (total x2.05)",  55000,  SkillEffect.CritDamageBonus, 0.30f, "critico_dano_3"),
            N("critico_dano_5", "Golpe Demoledor V",   "+0.35x mas (total x2.4)",  100000, SkillEffect.CritDamageBonus, 0.35f, "critico_dano_4"),
            N("critico_dano_6", "Golpe Demoledor VI",  "+0.4x mas (total x2.8)",   170000, SkillEffect.CritDamageBonus, 0.40f, "critico_dano_5"),

            N("extra_1", "Cocos Gemelos I",   "5% de probabilidad de que aparezca otro coco al romper uno", 800,   SkillEffect.ExtraCoconutChanceBonus, 0.05f, "resistencia_3"),
            N("extra_2", "Cocos Gemelos II",  "+5% (total 10%)", 2000,  SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_1"),
            N("extra_3", "Cocos Gemelos III", "+5% (total 15%)", 7000,  SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_2"),
            N("extra_4", "Cocos Gemelos IV",  "+5% (total 20%)", 13500, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_3"),
            N("extra_5", "Cocos Gemelos V", "+5% (total 25%)", 30000, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_4"),
            N("extra_6", "Cocos Gemelos VI", "+5% (total 30%)", 60000, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_5"),
            N("extra_7", "Cocos Gemelos VII", "+5% (total 35%)", 120000, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_6"),

            N("protegida_1", "Racha Protegida I", "Un fallo por dia no rompe tu racha", 1800, SkillEffect.StreakForgiveness, 1f, "radio_2", "racha_1"),
            N("protegida_2", "Racha Protegida II", "Dos fallos por dia no rompen tu racha", 7500, SkillEffect.StreakForgiveness, 1f, "protegida_1"),
            N("protegida_3", "Racha Protegida III", "Tres fallos por dia no rompen tu racha", 20000, SkillEffect.StreakForgiveness, 1f, "protegida_2"),

            N("negociador_1", "Negociador I",   "-3% al pedido actual y a los siguientes", 2500,   SkillEffect.OrderDiscount, 0.03f, "suerte_1"),
            N("negociador_2", "Negociador II",  "-5% mas al pedido",  9000,   SkillEffect.OrderDiscount, 0.05f, "negociador_1", "suerte_1"),
            N("negociador_3", "Negociador III", "-7% mas al pedido", 24000,  SkillEffect.OrderDiscount, 0.07f,  "negociador_2"),
            N("negociador_4", "Negociador IV",  "-10% mas al pedido", 100000, SkillEffect.OrderDiscount, 0.1f, "negociador_3"),
            N("negociador_5", "Negociador V", "-5% mas al pedido", 220000, SkillEffect.OrderDiscount, 0.05f, "negociador_4"),

            N("aliento_1", "Aliento Final I",  "Con menos del 20% de resistencia: +20% de agua", 4500,  SkillEffect.LastBreathBonus, 0.2f, "suerte_2"),
            N("aliento_2", "Aliento Final II", "Con menos del 20% de resistencia: +40% de agua (total)", 12000, SkillEffect.LastBreathBonus, 0.2f, "aliento_1"),
            N("aliento_3",   "Aliento Final III", "Con menos del 20% de resistencia: +60% de agua (total)", 30000, SkillEffect.LastBreathBonus, 0.2f, "aliento_2"),

            N("cadena_1", "Golpe en Cadena I",  "Al matar un coco, los cercanos reciben 15% del daño", 7000,  SkillEffect.ChainSplash, 0.15f, "extra_1"),
            N("cadena_2", "Golpe en Cadena II", "+10% de daño en cadena (total 25%)", 22000, SkillEffect.ChainSplash, 0.10f, "cadena_1"),
            N("cadena_3", "Golpe en Cadena III", "+10% de daño en cadena (total 35%)", 60000, SkillEffect.ChainSplash, 0.10f, "cadena_2"),

            N("manofirme_1", "Mano Firme I",  "Con menos del 20% de resistencia: golpeas 0.15s más rápido", 5000,  SkillEffect.LastBreathSwingBonus, 0.15f, "aliento_1"),
            N("manofirme_2", "Mano Firme II", "Con menos del 20% de resistencia: 0.3s más rápido (total)",  13500, SkillEffect.LastBreathSwingBonus, 0.15f, "manofirme_1"),
            N("manofirme_3", "Mano Firme III",    "Con menos del 20% de resistencia: 0.4s mas rapido (total)", 30000, SkillEffect.LastBreathSwingBonus, 0.10f, "manofirme_2"),

            N("filo_1", "Filo Afilado I",   "+5% de daño a cocos con menos del 50% de vida", 20000,  SkillEffect.ExecuteDamageBonus, 0.05f, "fuerza_5"),
            N("filo_2", "Filo Afilado II",  "+5% más (total 10%)", 30000, SkillEffect.ExecuteDamageBonus, 0.05f,  "filo_1"),
            N("filo_3", "Filo Afilado III", "+10% más (total 20%)", 45000, SkillEffect.ExecuteDamageBonus, 0.1f, "filo_2"),
            N("filo_4", "Filo Afilado IV", "+15% mas (total 35%)", 80000, SkillEffect.ExecuteDamageBonus, 0.15f, "filo_3"),

            N("reembolso_1", "Pedido Flexible I",  "Al entregar un pedido, recuperas 5% de lo entregado", 20000,  SkillEffect.OrderRefundBonus, 0.05f, "negociador_3"),
            N("reembolso_2", "Pedido Flexible II", "+5% más, total 15%",                                 50000, SkillEffect.OrderRefundBonus, 0.05f, "reembolso_1"),
            N("reembolso_3", "Pedido Flexible III", "+5% mas, total 15%", 120000, SkillEffect.OrderRefundBonus, 0.05f, "reembolso_2"),

            N("calma_1", "Calma Antes de la Tormenta I",   "+10% de agua en los primeros 5 segundos del día", 3000,  SkillEffect.EarlyDayWaterBonus, 0.10f, "manofirme_1"),
            N("calma_2", "Calma Antes de la Tormenta II",  "+15% más (total +25%)", 8000,  SkillEffect.EarlyDayWaterBonus, 0.15f, "calma_1"),
            N("calma_3", "Calma Antes de la Tormenta III", "+20% más (total +45%)", 18000, SkillEffect.EarlyDayWaterBonus, 0.2f, "calma_2"),
            N("calma_4", "Calma Antes de la Tormenta IV", "+25% mas (total +70%)", 40000, SkillEffect.EarlyDayWaterBonus, 0.25f, "calma_3"),
            N("calma_5", "Calma Antes de la Tormenta V", "+30% mas (total +100%)", 120000, SkillEffect.EarlyDayWaterBonus, 0.3f, "calma_4"),

            N("racha_1", "Racha Ardiente I",   "DESBLOQUEA la racha: desde el 3er golpe seguido que conecte, cada golpe da +5% de daño y agua (hasta 4 veces)", 1500, SkillEffect.StreakMaxStacksBonus, 4f, "radio_1"),
            N("racha_2", "Racha Ardiente II",  "+2 acumulaciones maximas de racha", 8000,  SkillEffect.StreakMaxStacksBonus, 2f, "racha_1"),
            N("racha_3", "Racha Ardiente III", "+2 acumulaciones maximas de racha", 30000, SkillEffect.StreakMaxStacksBonus, 2f, "racha_2"),
            N("racha_4", "Racha Ardiente IV", "+2 acumulaciones maximas de racha", 60000, SkillEffect.StreakMaxStacksBonus, 2f, "racha_3"),

            N("rachapoder_1", "Racha Poderosa I",   "Cada acumulacion de racha da +1% mas de daño y agua",   6000,  SkillEffect.StreakPowerBonus, 0.01f,  "racha_1"),
            N("rachapoder_2", "Racha Poderosa II",  "+1% mas por acumulacion",                               25000, SkillEffect.StreakPowerBonus, 0.01f,  "rachapoder_1", "racha_2"),
            N("rachapoder_3", "Racha Poderosa III", "+1.5% mas por acumulacion",                             70000, SkillEffect.StreakPowerBonus, 0.015f, "rachapoder_2", "racha_3"),
            N("rachapoder_4", "Racha Poderosa IV", "+1.5% mas por acumulacion (llega al tope de 10%)", 110000, SkillEffect.StreakPowerBonus, 0.015f, "rachapoder_3"),

            N("tesoro_1", "Ojo de Tesorero I",   "+1% de probabilidad de encontrar monedas",   2000,  SkillEffect.CoinChanceBonus, 0.01f,  "suerte_1"),
            N("tesoro_2", "Ojo de Tesorero II",  "+1.5% de probabilidad de encontrar monedas", 8000,  SkillEffect.CoinChanceBonus, 0.015f, "tesoro_1"),
            N("tesoro_3", "Ojo de Tesorero III", "+2% de probabilidad de encontrar monedas",   25000, SkillEffect.CoinChanceBonus, 0.02f,  "tesoro_2"),
            N("tesoro_4", "Ojo de Tesorero IV", "+2.5% de probabilidad de encontrar monedas", 60000, SkillEffect.CoinChanceBonus, 0.025f, "tesoro_3"),

                };
        }

        if (forceDefaultData || perkPool == null || perkPool.Count == 0)
        {
            perkPool = new List<PerkOption>
        {
            new PerkOption { perkName = "Manos Firmes",    description = "+4 daño.",                          effect = PerkEffect.DamageBonus,           value = 4f },
            new PerkOption { perkName = "Segundo Aire",    description = "+8 stamina máxima.",                effect = PerkEffect.StaminaMaxBonus,       value = 8f },
            new PerkOption { perkName = "Buen Trato",      description = "+15% agua obtenida.",               effect = PerkEffect.WaterMultiplierBonus,  value = 0.15f },
            new PerkOption { perkName = "Reflejos",        description = "-0.08 segundos entre golpes.",      effect = PerkEffect.SwingIntervalReduction,value = 0.08f },
            new PerkOption { perkName = "Golpe Certero",   description = "+6% de probabilidad de crítico.",   effect = PerkEffect.CritChanceBonus,       value = 0.06f },
            new PerkOption { perkName = "Cosecha Extra",   description = "+2 cocos iniciales cada día (esta partida).", effect = PerkEffect.ExtraCoconutNextCycle, value = 2f },
            new PerkOption { perkName = "Golpe de Suerte", description = "+10% de probabilidad de crítico.",  effect = PerkEffect.CritChanceBonus,       value = 0.10f },
            new PerkOption { perkName = "Coco Generoso",   description = "+25% agua, pero los golpes son 0.12s mas lentos.", effect = PerkEffect.WaterForSwingPenalty, value = 0.25f },
            new PerkOption { perkName = "Cosecha Abundante", description = "+3 cocos iniciales cada día (esta partida).", effect = PerkEffect.ExtraCoconutNextCycle, value = 3f },
            new PerkOption { perkName = "Manos de Hierro",   description = "+10 daño.",              effect = PerkEffect.DamageBonus,          value = 10f },
            new PerkOption { perkName = "Pulmones de Acero", description = "+12 stamina máxima.",    effect = PerkEffect.StaminaMaxBonus,      value = 12f },
            new PerkOption { perkName = "Pacto del Cobrador",description = "+25% agua obtenida.",    effect = PerkEffect.WaterMultiplierBonus, value = 0.25f },
            new PerkOption { perkName = "Ojo de Halcón",     description = "+12% de probabilidad de crítico.", effect = PerkEffect.CritChanceBonus, value = 0.12f },
            new PerkOption { perkName = "Coco Dorado",   description = "+50% agua, pero los golpes son 0.24s mas lentos.", effect = PerkEffect.WaterForSwingPenalty, value = 0.50f },
            new PerkOption { perkName = "Manos de Titán",description = "+18 daño.",                   effect = PerkEffect.DamageBonus, value = 18f },
            new PerkOption { perkName = "Ojo de Águila",      description = "+0.25 de radio de golpe.",                          effect = PerkEffect.HitRadiusBonus,        value = 0.25f },
            new PerkOption { perkName = "Moneda de la Suerte",description = "+1% de probabilidad de encontrar monedas.",         effect = PerkEffect.CoinChanceBonus,       value = 0.01f },
            new PerkOption { perkName = "Madrugador",         description = "+20% de agua en los primeros segundos del día.",    effect = PerkEffect.EarlyDayWaterBonus,    value = 0.20f },
            new PerkOption { perkName = "Buen Regateo",       description = "-4% al pedido actual y a los siguientes.",         effect = PerkEffect.OrderDiscount,         value = 0.04f },
            new PerkOption { perkName = "Racha Viva",         description = "+1 acumulación máxima de racha.",                   effect = PerkEffect.StreakMaxStacksBonus,  value = 1f },
            new PerkOption { perkName = "Pulso Firme",        description = "+1.5% de daño y agua por acumulación de racha.",    effect = PerkEffect.StreakPowerBonus,      value = 0.015f },
            new PerkOption { perkName = "Doble Golpe",        description = "+12% de daño al golpear 2 o más cocos a la vez.",   effect = PerkEffect.MultiHitDamageBonus,   value = 0.12f },
            new PerkOption { perkName = "Corazón de Jackpot", description = "+5% de probabilidad de Jackpot de agua.",           effect = PerkEffect.JackpotChanceBonus,    value = 0.05f },
            new PerkOption { perkName = "Último Aliento",     description = "Con poca resistencia: +15% de agua.",               effect = PerkEffect.LastBreathBonus,       value = 0.15f },
            new PerkOption { perkName = "Machete Pesado",     description = "+8 daño, pero -4 de stamina máxima.",               effect = PerkEffect.DamageForStaminaPenalty, value = 8f },
            new PerkOption { perkName = "Instinto Cazador",   description = "+8% de daño a cocos con menos del 50% de vida.",    effect = PerkEffect.ExecuteDamageBonus,    value = 0.08f },
            new PerkOption { perkName = "Chispa en Cadena",   description = "Al matar un coco, los cercanos reciben +12% del daño.", effect = PerkEffect.ChainSplash,       value = 0.12f },
            new PerkOption { perkName = "Racha Imparable",    description = "+2 acumulaciones máximas de racha.",                effect = PerkEffect.StreakMaxStacksBonus,  value = 2f },
            new PerkOption { perkName = "Jackpot Mayor",      description = "+10% de probabilidad de Jackpot de agua.",          effect = PerkEffect.JackpotChanceBonus,    value = 0.10f },
            new PerkOption { perkName = "Cazador Maestro",    description = "+15% de daño a cocos con menos del 50% de vida.",   effect = PerkEffect.ExecuteDamageBonus,    value = 0.15f },
        };
        }

        if (forceDefaultData || legacyShop == null || legacyShop.Count == 0)
        {
            legacyShop = new List<LegacyItem>
        {
            new LegacyItem { itemName = "Anillo del Machetero",   description = "+1 daño permanente.",                      cost = 1, effect = LegacyEffect.PermanentDamageBonus,           value = 1f },
            new LegacyItem { itemName = "Pulsera de Aguante",     description = "+5 stamina máxima permanente.",            cost = 2, effect = LegacyEffect.PermanentStaminaMaxBonus,       value = 5f },
            new LegacyItem { itemName = "Amuleto del Cobrador",   description = "+10% agua obtenida permanentemente.",      cost = 3, effect = LegacyEffect.PermanentMoneyMultiplierBonus,  value = 0.10f },
            new LegacyItem { itemName = "Cesta Grande",           description = "+1 coco inicial cada día.",                cost = 4, effect = LegacyEffect.PermanentStartingCoconutBonus,  value = 1f },
            new LegacyItem { itemName = "Lente del Afortunado",   description = "+3% probabilidad de crítico permanente.",  cost = 2, effect = LegacyEffect.PermanentCritChanceBonus,       value = 0.03f },
            new LegacyItem { itemName = "Moneda Antigua",         description = "+0.5% probabilidad de encontrar monedas.", cost = 1, effect = LegacyEffect.PermanentCoinChanceBonus,       value = 0.005f },
            new LegacyItem { itemName = "Anillo del Machetero II",   description = "+2 daño permanente.",                    cost = 3, effect = LegacyEffect.PermanentDamageBonus,          value = 2f },
            new LegacyItem { itemName = "Pulsera de Aguante II",     description = "+8 stamina máxima permanente.",          cost = 4, effect = LegacyEffect.PermanentStaminaMaxBonus,      value = 8f },
            new LegacyItem { itemName = "Amuleto del Cobrador II",   description = "+15% agua obtenida permanentemente.",    cost = 6, effect = LegacyEffect.PermanentMoneyMultiplierBonus, value = 0.15f },
            new LegacyItem { itemName = "Cesta Grande II",           description = "+1 coco inicial cada día.",              cost = 6, effect = LegacyEffect.PermanentStartingCoconutBonus, value = 1f },
            new LegacyItem { itemName = "Lente del Afortunado II",   description = "+6% probabilidad de crítico permanente.",cost = 5, effect = LegacyEffect.PermanentCritChanceBonus,      value = 0.06f },
            new LegacyItem { itemName = "Moneda Antigua II",         description = "+1% probabilidad de encontrar monedas.", cost = 3, effect = LegacyEffect.PermanentCoinChanceBonus,      value = 0.01f },
            new LegacyItem { itemName = "Anillo del Machetero III",  description = "+4 daño permanente.",                    cost = 6,  effect = LegacyEffect.PermanentDamageBonus,          value = 4f },
            new LegacyItem { itemName = "Pulsera de Aguante III",    description = "+12 stamina máxima permanente.",         cost = 8,  effect = LegacyEffect.PermanentStaminaMaxBonus,      value = 12f },
            new LegacyItem { itemName = "Amuleto del Cobrador III",  description = "+25% agua obtenida permanentemente.",    cost = 10, effect = LegacyEffect.PermanentMoneyMultiplierBonus, value = 0.25f },
            new LegacyItem { itemName = "Cesta Grande III",          description = "+2 cocos iniciales cada día.",           cost = 10, effect = LegacyEffect.PermanentStartingCoconutBonus, value = 2f },
            new LegacyItem { itemName = "Lente del Afortunado III",  description = "+10% probabilidad de crítico permanente.",cost = 8,  effect = LegacyEffect.PermanentCritChanceBonus,      value = 0.10f },
            new LegacyItem { itemName = "Moneda Antigua III",        description = "+2% probabilidad de encontrar monedas.", cost = 6,  effect = LegacyEffect.PermanentCoinChanceBonus,      value = 0.02f },
            new LegacyItem { itemName = "Lente del Afortunado IV",   description = "+12% probabilidad de crítico permanente.",cost = 12, effect = LegacyEffect.PermanentCritChanceBonus,     value = 0.12f },
            new LegacyItem { itemName = "Sello Ancestral I",   description = "+25% al valor de cada punto de legado.",  cost = 8,  effect = LegacyEffect.PermanentLegacyMultiplierBonus, value = 0.25f },
            new LegacyItem { itemName = "Sello Ancestral II",  description = "+50% al valor de cada punto de legado.",  cost = 16, effect = LegacyEffect.PermanentLegacyMultiplierBonus, value = 0.5f },
            new LegacyItem { itemName = "Sello Ancestral III", description = "+100% al valor de cada punto de legado.", cost = 30, effect = LegacyEffect.PermanentLegacyMultiplierBonus, value = 1f },
            new LegacyItem { itemName = "Reloj de Arena I",       description = "+1 día por ciclo.",                          cost = 8,  effect = LegacyEffect.PermanentExtraDay,        value = 1f },
            new LegacyItem { itemName = "Reloj de Arena II",      description = "+1 día por ciclo (total +2).",               cost = 16, effect = LegacyEffect.PermanentExtraDay,        value = 1f },
            new LegacyItem { itemName = "Barril de Reserva I",    description = "Empiezas cada partida con 250 mL.",          cost = 3,  effect = LegacyEffect.PermanentStartingWater,   value = 250f },
            new LegacyItem { itemName = "Barril de Reserva II",   description = "Empiezas con 750 mL.",                       cost = 6,  effect = LegacyEffect.PermanentStartingWater,   value = 500f },
            new LegacyItem { itemName = "Barril de Reserva III",  description = "Empiezas con 2000 mL.",                      cost = 10, effect = LegacyEffect.PermanentStartingWater,   value = 1250f },
            new LegacyItem { itemName = "Trébol de Agua I",       description = "+3% de probabilidad de Jackpot permanente.", cost = 4,  effect = LegacyEffect.PermanentJackpotChance,   value = 0.03f },
            new LegacyItem { itemName = "Trébol de Agua II",      description = "+5% de probabilidad de Jackpot permanente.", cost = 8,  effect = LegacyEffect.PermanentJackpotChance,   value = 0.05f },
            new LegacyItem { itemName = "Ritmo del Machetero I",  description = "+1 acumulación máxima de racha permanente.", cost = 4,  effect = LegacyEffect.PermanentStreakStacks,    value = 1f },
            new LegacyItem { itemName = "Ritmo del Machetero II", description = "+1 acumulación máxima de racha permanente.", cost = 8,  effect = LegacyEffect.PermanentStreakStacks,    value = 1f },
            new LegacyItem { itemName = "Ojo Largo I",            description = "+0.1 de radio de golpe permanente.",         cost = 3,  effect = LegacyEffect.PermanentHitRadius,       value = 0.1f },
            new LegacyItem { itemName = "Ojo Largo II",           description = "+0.15 de radio de golpe permanente.",        cost = 6,  effect = LegacyEffect.PermanentHitRadius,       value = 0.15f },
            new LegacyItem { itemName = "Contrato Favorable I",   description = "-5% a todos los pedidos.",                   cost = 6,  effect = LegacyEffect.PermanentOrderDiscount,   value = 0.05f },
            new LegacyItem { itemName = "Contrato Favorable II",  description = "-5% más a todos los pedidos.",               cost = 12, effect = LegacyEffect.PermanentOrderDiscount,   value = 0.05f },
            new LegacyItem { itemName = "Herencia del Herrero I", description = "Empiezas con el Machete Normal.",            cost = 8,  effect = LegacyEffect.PermanentStartingMachete, value = 1f },
            new LegacyItem { itemName = "Herencia del Herrero II",description = "Empiezas con el Machete de Acero.",          cost = 20, effect = LegacyEffect.PermanentStartingMachete, value = 1f },
        };
        }
        StampCoconutUnlocks();

    }
    private static readonly CoconutUnlockThreshold[] CanonicalUnlocks = new CoconutUnlockThreshold[]
{
     new CoconutUnlockThreshold { coconutName = "Coco Verde",       cycleRequired = 1,  ability = CoconutAbility.Ninguna },
    new CoconutUnlockThreshold { coconutName = "Coco Maduro",      cycleRequired = 2,  ability = CoconutAbility.Generoso },
    new CoconutUnlockThreshold { coconutName = "Coco Correoso",    cycleRequired = 3,  ability = CoconutAbility.Resistente },
    new CoconutUnlockThreshold { coconutName = "Coco Fibroso",     cycleRequired = 4,  ability = CoconutAbility.FibraDura },
    new CoconutUnlockThreshold { coconutName = "Coco Petreo",      cycleRequired = 5,  ability = CoconutAbility.Pesado },
    new CoconutUnlockThreshold { coconutName = "Coco Curtido",     cycleRequired = 6,  ability = CoconutAbility.Tenaz },
    new CoconutUnlockThreshold { coconutName = "Coco Blindado",    cycleRequired = 7,  ability = CoconutAbility.Armadura },
    new CoconutUnlockThreshold { coconutName = "Coco de Hierro",   cycleRequired = 8,  ability = CoconutAbility.Rebote },
    new CoconutUnlockThreshold { coconutName = "Coco de Acero",    cycleRequired = 9,  ability = CoconutAbility.Fortificado },
    new CoconutUnlockThreshold { coconutName = "Coco de Titanio",  cycleRequired = 10, ability = CoconutAbility.Implacable },
    new CoconutUnlockThreshold { coconutName = "Coco de Diamante", cycleRequired = 11, ability = CoconutAbility.FragilValioso },
    new CoconutUnlockThreshold { coconutName = "Coco Legendario",  cycleRequired = 12, ability = CoconutAbility.Regeneracion },
    new CoconutUnlockThreshold { coconutName = "Coco Mitico",      cycleRequired = 13, ability = CoconutAbility.Camuflaje },
    new CoconutUnlockThreshold { coconutName = "Coco Ancestral",   cycleRequired = 14, ability = CoconutAbility.Maldicion },
    new CoconutUnlockThreshold { coconutName = "Coco Supremo",     cycleRequired = 16, ability = CoconutAbility.Supremo },
};

    void StampCoconutUnlocks()
    {
        if (coconutUnlocks == null) coconutUnlocks = new List<CoconutUnlockThreshold>();

        while (coconutUnlocks.Count < CanonicalUnlocks.Length)
            coconutUnlocks.Add(new CoconutUnlockThreshold());

        for (int i = 0; i < CanonicalUnlocks.Length; i++)
        {
            Sprite keepIcon = coconutUnlocks[i].icon;
            GameObject keepPrefab = coconutUnlocks[i].prefab;
            coconutUnlocks[i].coconutName = CanonicalUnlocks[i].coconutName;
            coconutUnlocks[i].cycleRequired = CanonicalUnlocks[i].cycleRequired;
            coconutUnlocks[i].ability = CanonicalUnlocks[i].ability;
            coconutUnlocks[i].icon = keepIcon;
            coconutUnlocks[i].prefab = keepPrefab;
        }
    }
    [System.Serializable]
    public class PerkIconEntry
    {
        public string perkName;
        public Sprite icon;
    }

    [Header("Iconos de perks (por nombre)")]
    public List<PerkIconEntry> perkIcons = new List<PerkIconEntry>();

    Sprite GetPerkIcon(PerkOption perk)
    {
        if (perk == null) return null;
        foreach (var entry in perkIcons)
        {
            if (entry != null && entry.perkName == perk.perkName) return entry.icon;
        }
        return perk.icon;
    }

    [ContextMenu("Rellenar nombres de perks")]
    void FillPerkIconNames()
    {
        string[] names = { "Manos Firmes", "Segundo Aire", "Buen Trato", "Reflejos",
                     "Golpe Certero", "Cosecha Extra", "Coco Generoso", "Golpe de Suerte",
                      "Cosecha Abundante", "Manos de Hierro", "Pulmones de Acero",
                      "Pacto del Cobrador", "Ojo de Halcón", "Coco Dorado", "Manos de Titán" };
        perkIcons.Clear();
        foreach (var n in names) perkIcons.Add(new PerkIconEntry { perkName = n });
    }
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        SetCursorVisible(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        SetCursorVisible(currentPhase != Phase.Hitting);
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        SaveGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
