using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
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
    public float waterTargetML = 500f;
    [Tooltip("Cuanto crece el pedido de agua en cada ciclo nuevo (2 = se duplica)")]
    public float waterTargetGrowth = 1.65f;
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
    private bool lastBreathSwingActive = false;
    private bool isApplyingSplash = false;
    public float executeHpThreshold = 0.5f;
    public float earlyDayDuration = 3f;
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
    [Tooltip("Si es true, la PROXIMA llamada a ContinueToNextDay() no descuenta daysLeft (se consume solo).")]
    public bool skipNextDayDecrement = false;
    [System.Serializable]
    public class CoconutUnlockThreshold
    {
        [HideInInspector] public string coconutName;
        [HideInInspector] public int killsRequired;
        [HideInInspector] public CoconutAbility ability = CoconutAbility.Ninguna;

        public Sprite icon;
        public GameObject prefab;
    }
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
        int stacks = currentHitStreak - streakThreshold + 1;
        return Mathf.Clamp(stacks, 0, streakMaxStacks);
    }

    public float GetStreakDamageMultiplier() => 1f + CurrentStreakStacks() * streakDamageBonusPerHit;
    public float GetStreakWaterMultiplier() => 1f + CurrentStreakStacks() * streakWaterBonusPerHit;
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
    public float GetCritMultiplier() => 2f + skillCritDamageBonus;

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
    public int coconutCoins = 0;
    [HideInInspector] public bool lastKillFoundCoin = false;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que un coco suelte 1 moneda al morir. Empieza en 0.5% (0.005) y sube con los nodos 'Suerte de Moneda' del arbol de mejoras.")]
    public float coinDropChance = 0.005f;
    [Header("Progreso permanente (guardado)")]
    public int bestRunKills = 0;
    public int bestCycleReached = 1;
    [Header("Machete: progresion lineal (se paga con agua de coco, se resetea en bancarrota)")]
    public List<MacheteData> macheteOptions = new List<MacheteData>();
    public int equippedMacheteIndex = 0;

    [Header("Arbol de Mejoras (se paga con agua de coco, ramificado, se resetea en bancarrota)")]
    public List<SkillNode> skillTree = new List<SkillNode>();
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
    [Header("Perks (1 de 3 al pagar cada cuenta)")]
    public List<PerkOption> perkPool = new List<PerkOption>();
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
    [Header("Legado (permanente, solo se gasta tras una bancarrota)")]
    public int legacyPoints = 0;
    public int totalLegacyPointsEarned = 0;
    public float legacyMultiplier = 1f;
    public List<LegacyItem> legacyShop = new List<LegacyItem>();
    private HashSet<string> purchasedLegacyItems = new HashSet<string>();
    [Header("Legacy")]
    public float legacyDamageBonus = 0f;
    public float legacyStaminaBonus = 0f;
    public float legacyWaterMultiplierBonus = 0f;
    public float legacyCritChanceBonus = 0f;

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
    static readonly Dictionary<string, int> PerkMinCycle = new Dictionary<string, int>
{
    { "Cosecha Abundante", 7 }, { "Manos de Hierro", 7 }, { "Pulmones de Acero", 7 },
    { "Pacto del Cobrador", 8 }, { "Ojo de Halcón", 8 },
    { "Coco Dorado", 11 }, { "Manos de Titán", 11 },
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
        if (Input.GetKeyDown(KeyCode.Escape) && currentPhase != Phase.MainMenu)
        {
            if (isPaused) ResumeGame(); else PauseGame();
        }
        if (currentPhase == Phase.Hitting)
        {
            stamina -= Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                EnterRecaudacionPhase();
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

    public bool IsOrderUrgent => waterCurrentML < waterTargetML && daysLeft <= 1;

    string Loc(string key) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
    string Loc(string key, params object[] args) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key, args) : key;

    public void BeginNewGame()
    {
        stamina = GetStaminaMax();
        daysLeft = dayLimitBase;
        runCoconutsKilled = 0;
        runMoneyEarned = 0;
        RecalculateAllStats();
        ShowHittingUI();
        RefreshAllUI();
        runStartTime = Time.time;
        BetaLog("run_start");
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
        float baseRadius = m != null ? m.baseHitRadius : 1.5f;
        int damage = Mathf.RoundToInt(baseDmg + skillDamageBonus + perkDamageBonus + legacyDamageBonus + relicDamageBonus);

        if (MacheteController.Instance != null)
        {
            MacheteController.Instance.damageOverride = damage;
            MacheteController.Instance.hitRadius = baseRadius + skillHitRadiusBonus + relicHitRadiusBonus;
            MacheteController.Instance.swingInterval = Mathf.Max(0.2f,
    baseSwing - skillSwingIntervalReduction - perkSwingIntervalReduction - relicSwingIntervalReduction
    + curseSwingPenalty - (lastBreathSwingActive ? skillLastBreathSwingReduction : 0f));
        }
    }
    public void RefreshCombatStats() => RecalculateAllStats();
    public float GetStaminaMax()
    {
        return Mathf.Max(1f, staminaMaxBase + skillStaminaBonus + perkStaminaMaxBonus
        + legacyStaminaBonus + relicStaminaMaxBonus - curseStaminaPenalty);
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
            if (totalCoconutsKilled < u.killsRequired)
            {
                if (next == null || u.killsRequired < next.killsRequired) next = u;
            }
        }
        return next;
    }

    public float GetNextUnlockProgress01()
    {
        CoconutUnlockThreshold next = GetNextLockedCoconut();
        if (next == null || next.killsRequired <= 0) return 1f;
        return Mathf.Clamp01((float)totalCoconutsKilled / next.killsRequired);
    }

    public float GetCritChance()
    {
        float chance = skillCritChanceBonus + perkCritChanceBonus + legacyCritChanceBonus + relicCritChanceBonus;
        return Mathf.Clamp(chance, 0f, 2f);
    }

    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained)
    {
        totalCoconutsKilled++;
        runCoconutsKilled++;
        if (runCoconutsKilled > bestRunKills) bestRunKills = runCoconutsKilled;
        dayCoconutsKilled++;
        float waterExtraMult = 1f + skillWaterMultiplierBonus + perkWaterMultiplierBonus + legacyWaterMultiplierBonus + relicWaterMultiplierBonus - curseWaterPenalty;
        float streakMult = GetStreakWaterMultiplier();
        waterGained = (baseWaterPerKill + Random.Range(0f, waterVariance)) * lootMultiplier * waterExtraMult * legacyMultiplier * streakMult * GetLastBreathMultiplier() * GetEarlyDayMultiplier();
        lastKillHadJackpot = skillJackpotChance > 0f && Random.value < skillJackpotChance;
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
        if (skillStaminaRecoveryChance > 0f && dayStaminaRecovered < maxStaminaRecoveryPerDay
    && Random.value < skillStaminaRecoveryChance)
        {
            float amount = Mathf.Min(skillStaminaRecoveryAmount, maxStaminaRecoveryPerDay - dayStaminaRecovered);
            stamina = Mathf.Min(GetStaminaMax(), stamina + amount);
            dayStaminaRecovered += amount;
            Log("¡Recuperaste " + Mathf.RoundToInt(amount) + " de resistencia!");
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
        }
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

    public void ToggleTiendaPanel()
    {
        if (forcedOrderDecision) return;
        bool willOpen = tiendaPanel != null && !tiendaPanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(tiendaPanel, new GameObject[] { upgradeTreePanel, rewardMachinePanel }, willOpen));
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
        return (skillMultiHitBonus > 0f && targetsHit >= 2) ? 1f + skillMultiHitBonus : 1f;
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
        legacyDamageBonus = 0f;
        legacyStaminaBonus = 0f;
        legacyWaterMultiplierBonus = 0f;
        legacyCritChanceBonus = 0f;
        legacyStartingCoconuts = 0;
        legacyMultiplierBonus = 0f;
        legacyCoinChanceBonus = 0f;
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
        daysLeft = Mathf.Max(3, dayLimitBase - ((billCycle - 1) / 2));
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

        int gained = Mathf.Max(1, Mathf.RoundToInt(billCycle / 2f));
        legacyPoints += gained;
        totalLegacyPointsEarned += gained;
        RecalcLegacyMultiplier();
        skillStaminaRecoveryChance = 0f;
        skillStaminaRecoveryAmount = 0f;
        skillJackpotChance = 0f; 
        skillJackpotMultBonus = 0f;
        skillCritDamageBonus = 0f;
        currentHitStreak = 0;
        money = 0;
        billCycle = 1;
        waterCurrentML = 0f;
        waterTargetML = 500f;
        daysLeft = dayLimitBase;
        totalCoconutsKilled = 0;
        unlockedSkillIds.Clear();
        skillDamageBonus = 0f;
        skillStaminaBonus = 0f;
        skillSwingIntervalReduction = 0f;
        skillHitRadiusBonus = 0f;
        skillWaterMultiplierBonus = 0f;
        equippedMacheteIndex = 0;
        perkDamageBonus = 0f;
        perkStaminaMaxBonus = 0f;
        perkWaterMultiplierBonus = 0f;
        perkSwingIntervalReduction = 0f;
        relicDamageBonus = 0f;
        relicStaminaMaxBonus = 0f;
        relicWaterMultiplierBonus = 0f;
        relicSwingIntervalReduction = 0f;
        relicHitRadiusBonus = 0f;
        relicCritChanceBonus = 0f;
        coinDropChance = 0.005f + legacyCoinChanceBonus;
        skillExtraCoconutChance = 0f;
        skillCritChanceBonus = 0f;
        perkCritChanceBonus = 0f;
        skillStreakForgives = 0;
        streakForgivesLeft = 0;
        skillLastBreathBonus = 0f;
        skillExecuteBonus = 0f;
        skillOrderRefund = 0f;
        skillEarlyDayBonus = 0f;
        skillChainSplashPercent = 0f;
        skillLastBreathSwingReduction = 0f;
        skillMultiHitBonus = 0f;
        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.startingCoconuts = 4 + legacyStartingCoconuts;
            CoconutSpawner.Instance.spawnInterval = 5f;
        }
        perkCopies.Clear();
        RecalculateAllStats();

        if (bankruptcyMessageText != null)
        {
            bankruptcyMessageText.text = Loc("log_bankruptcy", gained);
        }
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
                    CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - node.effectValue);break;
            case SkillEffect.CoinChanceBonus:coinDropChance += node.effectValue;break;
            case SkillEffect.CritChanceBonus:skillCritChanceBonus += node.effectValue;break;
            case SkillEffect.StaminaRecoveryChanceBonus:skillStaminaRecoveryChance = Mathf.Min(0.35f, skillStaminaRecoveryChance + node.effectValue);skillStaminaRecoveryAmount += 2f;break;
            case SkillEffect.JackpotChanceBonus:skillJackpotChance += node.effectValue;break;
            case SkillEffect.JackpotWaterBonus: skillJackpotMultBonus += node.effectValue; break;
            case SkillEffect.CritDamageBonus:skillCritDamageBonus += node.effectValue;break;
            case SkillEffect.ExtraCoconutChanceBonus:skillExtraCoconutChance = Mathf.Min(0.5f, skillExtraCoconutChance + node.effectValue);break;
            case SkillEffect.StreakForgiveness:skillStreakForgives += Mathf.RoundToInt(node.effectValue);streakForgivesLeft += Mathf.RoundToInt(node.effectValue);break;
            case SkillEffect.OrderDiscount:waterTargetML *= 1f - node.effectValue;break;
            case SkillEffect.LastBreathBonus:skillLastBreathBonus += node.effectValue;break;
            case SkillEffect.ChainSplash: skillChainSplashPercent += node.effectValue; break;
            case SkillEffect.LastBreathSwingBonus: skillLastBreathSwingReduction += node.effectValue; break;
            case SkillEffect.ExecuteDamageBonus: skillExecuteBonus += node.effectValue; break;
            case SkillEffect.OrderRefundBonus: skillOrderRefund += node.effectValue; break;
            case SkillEffect.EarlyDayWaterBonus: skillEarlyDayBonus += node.effectValue; break;
            case SkillEffect.MultiHitDamageBonus: skillMultiHitBonus += node.effectValue; break;
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
        if (skillChainSplashPercent <= 0f || isApplyingSplash) return;
        isApplyingSplash = true;
        int splashDamage = Mathf.Max(1, Mathf.RoundToInt(killingDamage * skillChainSplashPercent));
        float radius = MacheteController.Instance != null ? MacheteController.Instance.hitRadius : 1.5f;
        foreach (var col in Physics.OverlapSphere(position, radius))
        {
            if (col.gameObject == source) continue;
            var target = col.GetComponentInParent<CoconutTarget>();
            if (target == null || target.gameObject == source) continue;
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
        BeginNewGame();
    }
    void ApplyPerk(PerkOption perk)
    {
        switch (perk.effect)
        {
            case PerkEffect.DamageBonus: perkDamageBonus += perk.value; break;
            case PerkEffect.StaminaMaxBonus: perkStaminaMaxBonus += perk.value; break;
            case PerkEffect.MoneyMultiplierBonus: perkWaterMultiplierBonus += perk.value; break;
            case PerkEffect.SwingIntervalReduction: perkSwingIntervalReduction += perk.value; break;
            case PerkEffect.ExtraCoconutNextCycle:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(perk.value);
                break;
            case PerkEffect.CritChanceBonus:
                perkCritChanceBonus += perk.value;
                break;
            case PerkEffect.WaterForSwingPenalty:perkWaterMultiplierBonus += perk.value;perkSwingIntervalReduction -= perk.value * 0.48f;break;
            case PerkEffect.WaterMultiplierBonus:
                perkWaterMultiplierBonus += perk.value;
                break;
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
        waterCurrentML = 0f;
        waterTargetML = 500f;
        daysLeft = dayLimitBase;
        totalCoconutsKilled = 0;
        unlockedSkillIds.Clear();
        skillExecuteBonus = 0f;
        skillDamageBonus = 0f;
        skillStaminaBonus = 0f;
        skillSwingIntervalReduction = 0f;
        skillHitRadiusBonus = 0f;
        skillWaterMultiplierBonus = 0f;
        skillStaminaRecoveryChance = 0f;
        skillStaminaRecoveryAmount = 0f;
        skillJackpotChance = 0f;
        skillJackpotMultBonus = 0f;
        skillCritDamageBonus = 0f;
        skillCritChanceBonus = 0f;
        skillExtraCoconutChance = 0f;
        skillStreakForgives = 0;
        streakForgivesLeft = 0;
        skillLastBreathBonus = 0f;
        skillExecuteBonus = 0f;
        skillOrderRefund = 0f;
        skillEarlyDayBonus = 0f;
        skillChainSplashPercent = 0f;
        skillLastBreathSwingReduction = 0f;

        equippedMacheteIndex = 0;
        currentHitStreak = 0;

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
            CoconutSpawner.Instance.spawnInterval = 5f;
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
            case LegacyEffect.PermanentStartingCoconutBonus:legacyStartingCoconuts += Mathf.RoundToInt(item.value);if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(item.value);break;
            case LegacyEffect.PermanentLegacyMultiplierBonus:legacyMultiplierBonus += item.value;RecalcLegacyMultiplier();break;
            case LegacyEffect.PermanentCoinChanceBonus:legacyCoinChanceBonus += item.value;coinDropChance += item.value;break;

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
            if (showText) nextUnlockPercentText.text = Mathf.RoundToInt(unlockProgress * 100f) + "%";
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
            if (forcedOrderDecision)
            {
                deudaDaysLeftText.text = readyToDeliver ? "¡ÚLTIMO DÍA! Paga o bancarrota" : "¡ÚLTIMO DÍA! No alcanza: bancarrota";
                deudaDaysLeftText.color = urgentColor;
            }
            else if (!readyToDeliver && daysLeft <= 1)
            {
                deudaDaysLeftText.text = "¡ENTREGALO YA!";
                deudaDaysLeftText.color = urgentColor;
            }
            else
            {
                deudaDaysLeftText.text = daysLeft + " dias restantes";
                deudaDaysLeftText.color = normalDaysColor;
            }
        }

        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(forcedOrderDecision);

        if (payDebtButton != null) payDebtButton.interactable = readyToDeliver;

        MacheteData current = GetCurrentMachete();
        MacheteData next = GetNextMachete();
        if (macheteStatusText != null) macheteStatusText.text = Loc("shop_current_machete", current != null ? current.macheteName : "-");
        if (next != null)
        {
            if (macheteUpgradeCostText != null) macheteUpgradeCostText.text = Loc("shop_next_machete", next.macheteName, FormatWater(next.unlockCost));
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
            new MacheteData { macheteName = "Machete Oxidado",     description = "El que ya tienes.",                    baseDamage = 5f,  baseSwingInterval = 1.2f,  baseHitRadius = 1.5f,  unlockCost = 0 },
            new MacheteData { macheteName = "Machete Normal",      description = "Mas daño, mas rapido, mas rango.",     baseDamage = 9f,  baseSwingInterval = 1.1f,  baseHitRadius = 1.6f,  unlockCost = 2500 },
            new MacheteData { macheteName = "Machete de Acero",    description = "Un salto grande de poder.",            baseDamage = 14f, baseSwingInterval = 1.05f, baseHitRadius = 1.75f, unlockCost = 12000 },
            new MacheteData { macheteName = "Machete de Oro",      description = "Brilla y pega fuerte.",                baseDamage = 22f, baseSwingInterval = 1.0f,  baseHitRadius = 1.9f,  unlockCost = 45000 },
            new MacheteData { macheteName = "Machete de Obsidiana",description = "Corta como el vidrio. Pega muy duro.",baseDamage = 35f, baseSwingInterval = 0.95f, baseHitRadius = 2.05f, unlockCost = 150000 },
            new MacheteData { macheteName = "Machete de Maestro",  description = "La obra final del herrero.",           baseDamage = 50f, baseSwingInterval = 0.9f,  baseHitRadius = 2.2f,  unlockCost = 400000 },
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

            N("velocidad_1", "Manos Rapidas I",   "Golpea mas seguido",      150,   SkillEffect.SwingIntervalReduction, 0.15f, "fuerza_1"),
            N("velocidad_2", "Manos Rapidas II",  "Golpea aun mas seguido",  1300,  SkillEffect.SwingIntervalReduction, 0.2f,  "velocidad_1"),
            N("velocidad_3", "Manos Rapidas III", "Golpea aun mas seguido",  25000, SkillEffect.SwingIntervalReduction, 0.15f, "velocidad_2"),

            N("suerte_1", "Buen Ojo I",  "+15% de agua por coco", 500,  SkillEffect.MoneyMultiplierBonus, 0.15f, "fuerza_2"),
            N("suerte_2", "Buen Ojo II", "+20% de agua por coco", 5000, SkillEffect.MoneyMultiplierBonus, 0.2f,  "suerte_1"),

            N("radio_1", "Golpe Amplio I",  "Mas radio de golpe",     1200, SkillEffect.HitRadiusBonus, 0.2f, "velocidad_1"),
            N("radio_2", "Golpe Amplio II", "Aun mas radio de golpe", 2200, SkillEffect.HitRadiusBonus, 0.3f, "radio_1"),
            N("radio_3", "Golpe Amplio III", "Mas radio de golpe", 10000,  SkillEffect.HitRadiusBonus, 0.20f, "protegida_2"),
            N("radio_4", "Golpe Amplio IV",  "Mas radio de golpe", 20000, SkillEffect.HitRadiusBonus, 0.25f, "radio_3"),
            N("radio_5", "Golpe Amplio V",   "Mas radio de golpe", 35000, SkillEffect.HitRadiusBonus, 0.30f, "radio_4"),
            N("radio_6", "Golpe Amplio VI",  "Mas radio de golpe", 80000, SkillEffect.HitRadiusBonus, 0.35f, "radio_5"),

            N("multi_1", "Golpe Multiple I",   "+20% de daño si golpeas 2 o mas cocos a la vez",     15000,  SkillEffect.MultiHitDamageBonus, 0.20f, "radio_3"),
            N("multi_2", "Golpe Multiple II",  "+15% mas (total +35%) al golpear 2 o mas cocos",     40000, SkillEffect.MultiHitDamageBonus, 0.15f, "multi_1"),
            N("multi_3", "Golpe Multiple III", "+15% mas (total +50%) al golpear 2 o mas cocos",     90000, SkillEffect.MultiHitDamageBonus, 0.15f, "multi_2"),

            N("resistencia_1", "Aguante I",   "+6 segundos de resistencia", 500,   SkillEffect.StaminaMaxFlatBonus, 6f, "fuerza_1"),
            N("resistencia_2", "Aguante II",  "+6 segundos de resistencia", 1400,  SkillEffect.StaminaMaxFlatBonus, 6f, "resistencia_1"),
            N("resistencia_3", "Aguante III", "+6 segundos de resistencia", 2400,  SkillEffect.StaminaMaxFlatBonus, 6f, "resistencia_2"),
            N("resistencia_4", "Aguante IV",  "+8 segundos de resistencia", 20000, SkillEffect.StaminaMaxFlatBonus, 8f, "resistencia_3"),

            N("eficiencia_1", "Segundo Aliento I",   "10% de probabilidad de recuperar 2 de resistencia al destruir un coco", 750,  SkillEffect.StaminaRecoveryChanceBonus, 0.10f, "resistencia_2"),
            N("eficiencia_2", "Segundo Aliento II",  "+10% de probabilidad (total 20%) y +2 de resistencia recuperada (total 4)", 5000, SkillEffect.StaminaRecoveryChanceBonus, 0.10f, "eficiencia_1"),
            N("eficiencia_3", "Segundo Aliento III", "+8% de probabilidad (total 28%) y +2 de resistencia recuperada (total 6)", 15000, SkillEffect.StaminaRecoveryChanceBonus, 0.08f, "eficiencia_2"),
            N("eficiencia_4", "Segundo Aliento IV",  "+7% de probabilidad (total 35%, el maximo) y +2 de resistencia recuperada (total 8)", 50000, SkillEffect.StaminaRecoveryChanceBonus, 0.07f, "eficiencia_3"),

            N("cocos_1", "Cosecha Inicial",     "+1 coco al iniciar el dia", 400,  SkillEffect.ExtraStartingCoconut, 1f, "fuerza_1"),
            N("cocos_2", "Mejores vendedores",  "+2 cocos al iniciar el dia", 950,  SkillEffect.ExtraStartingCoconut, 2f, "aparicion_1"),
            N("cocos_3", "Cocos locos",         "+3 cocos al iniciar el dia", 2300, SkillEffect.ExtraStartingCoconut, 3f, "aparicion_2"),

            N("aparicion_1", "Cosecha Rapida",      "Los cocos aparecen mas seguido", 600,  SkillEffect.SpawnIntervalReduction, 0.5f, "cocos_1"),
            N("aparicion_2", "Cosecha Rapidisima",  "Los cocos aparecen mas seguido", 1300, SkillEffect.SpawnIntervalReduction, 0.7f, "cocos_2"),
            N("aparicion_3", "Cosecha Veloz",       "Los cocos aparecen mas seguido", 4000, SkillEffect.SpawnIntervalReduction, 1f,   "cocos_3"),

            N("leche_1", "Coco Bendito I",   "+15% de agua de coco", 500,   SkillEffect.WaterMultiplierBonus, 0.15f, "velocidad_2"),
            N("leche_2", "Coco Bendito II",  "+20% de agua de coco", 1500,  SkillEffect.WaterMultiplierBonus, 0.2f,  "leche_1"),
            N("leche_3", "Coco Bendito III", "+30% de agua de coco", 5000,  SkillEffect.WaterMultiplierBonus, 0.3f,  "leche_2"),
            N("leche_4", "Coco Bendito IV",  "+40% de agua de coco", 30000, SkillEffect.WaterMultiplierBonus, 0.4f,  "leche_3"),

            N("moneda_1", "Golpe de Suerte I",   "+7% de probabilidad de Jackpot de Agua al destruir un coco", 1000,   SkillEffect.JackpotChanceBonus, 0.07f, "velocidad_2", "radio_1"),
            N("moneda_2", "Golpe de Suerte II",  "+4% de probabilidad de Jackpot (total 11%)",  9000,   SkillEffect.JackpotChanceBonus, 0.04f, "moneda_1", "radio_2"),
            N("moneda_3", "Golpe de Suerte III", "+6% de probabilidad de Jackpot (total 17%)",  20000,  SkillEffect.JackpotChanceBonus, 0.06f, "moneda_2"),
            N("moneda_4", "Golpe de Suerte IV",  "+8% de probabilidad de Jackpot (total 25%)",  50000,  SkillEffect.JackpotChanceBonus, 0.08f, "moneda_3"),
            N("moneda_5", "Golpe de Suerte V",   "+10% de probabilidad de Jackpot (total 35%)", 100000, SkillEffect.JackpotChanceBonus, 0.1f,  "moneda_4"),

            N("jackpot_1", "Botin de Jackpot I",  "El Jackpot da +1x mas agua (total x2.5 del coco)",   1000,  SkillEffect.JackpotWaterBonus, 1f,   "moneda_2"),
            N("jackpot_2", "Botin de Jackpot II", "El Jackpot da +1.5x mas agua (total x4 del coco)",   40000, SkillEffect.JackpotWaterBonus, 1.5f, "jackpot_1"),

            N("critico_1", "Golpe Certero I",   "+5% de probabilidad de golpe crítico.",  1000,  SkillEffect.CritChanceBonus, 0.05f, "leche_1"),
            N("critico_2", "Golpe Certero II",  "+7% de probabilidad de golpe crítico.",  5000,  SkillEffect.CritChanceBonus, 0.07f, "critico_1"),
            N("critico_3", "Golpe Certero III", "+8% de probabilidad de golpe crítico.",  13500, SkillEffect.CritChanceBonus, 0.08f, "critico_2"),
            N("critico_4", "Golpe Certero IV",  "+10% de probabilidad de golpe crítico.", 40000, SkillEffect.CritChanceBonus, 0.10f, "critico_3"),
            N("critico_5", "Golpe Certero V",   "+10% de probabilidad de golpe crítico.", 90000, SkillEffect.CritChanceBonus, 0.10f, "critico_4"),

            N("critico_dano_1", "Golpe Demoledor I",   "Los criticos pegan +0.2x mas (de x2 a x2.2)", 6000,   SkillEffect.CritDamageBonus, 0.2f, "critico_2"),
            N("critico_dano_2", "Golpe Demoledor II",  "Los criticos pegan +0.3x mas (total x2.5)",   20000,  SkillEffect.CritDamageBonus, 0.3f, "critico_dano_1", "critico_3"),
            N("critico_dano_3", "Golpe Demoledor III", "Los criticos pegan +0.5x mas (total x3)",     50000,  SkillEffect.CritDamageBonus, 0.5f, "critico_dano_2"),
            N("critico_dano_4", "Golpe Demoledor IV",  "Los criticos pegan +0.5x mas (total x3.5)",   100000, SkillEffect.CritDamageBonus, 0.5f, "critico_dano_3"),

            N("extra_1", "Cocos Gemelos I",   "5% de probabilidad de que aparezca otro coco al romper uno", 800,   SkillEffect.ExtraCoconutChanceBonus, 0.05f, "resistencia_3"),
            N("extra_2", "Cocos Gemelos II",  "+5% (total 10%)", 2000,  SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_1"),
            N("extra_3", "Cocos Gemelos III", "+5% (total 15%)", 7000,  SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_2"),
            N("extra_4", "Cocos Gemelos IV",  "+5% (total 20%)", 13500, SkillEffect.ExtraCoconutChanceBonus, 0.05f, "extra_3"),

            N("protegida_1", "Racha Protegida I",  "Un fallo por dia no rompe tu racha",    1800, SkillEffect.StreakForgiveness, 1f, "radio_2"),
            N("protegida_2", "Racha Protegida II", "Dos fallos por dia no rompen tu racha", 7500, SkillEffect.StreakForgiveness, 1f, "protegida_1"),

            N("negociador_1", "Negociador I",   "-4% al pedido actual y a los siguientes", 2500,   SkillEffect.OrderDiscount, 0.04f, "suerte_1"),
            N("negociador_2", "Negociador II",  "-8% mas al pedido",  9000,   SkillEffect.OrderDiscount, 0.08f, "negociador_1"),
            N("negociador_3", "Negociador III", "-10% mas al pedido", 24000,  SkillEffect.OrderDiscount, 0.1f,  "negociador_2"),
            N("negociador_4", "Negociador IV",  "-15% mas al pedido", 100000, SkillEffect.OrderDiscount, 0.15f, "negociador_3"),

            N("aliento_1", "Aliento Final I",  "Con menos del 20% de resistencia: +20% de agua", 4500,  SkillEffect.LastBreathBonus, 0.2f, "suerte_2"),
            N("aliento_2", "Aliento Final II", "Con menos del 20% de resistencia: +40% de agua (total)", 12000, SkillEffect.LastBreathBonus, 0.2f, "aliento_1"),

            N("cadena_1", "Golpe en Cadena I",  "Al matar un coco, los cercanos reciben 20% del daño", 7000,  SkillEffect.ChainSplash, 0.20f, "extra_1"),
            N("cadena_2", "Golpe en Cadena II", "+15% de daño en cadena (total 35%)",                  22000, SkillEffect.ChainSplash, 0.15f, "cadena_1"),

            N("manofirme_1", "Mano Firme I",  "Con menos del 20% de resistencia: golpeas 0.15s más rápido", 5000,  SkillEffect.LastBreathSwingBonus, 0.15f, "aliento_1"),
            N("manofirme_2", "Mano Firme II", "Con menos del 20% de resistencia: 0.3s más rápido (total)",  13500, SkillEffect.LastBreathSwingBonus, 0.15f, "manofirme_1"),

            N("filo_1", "Filo Afilado I",   "+7% de daño a cocos con menos del 50% de vida", 4500,  SkillEffect.ExecuteDamageBonus, 0.07f, "fuerza_5"),
            N("filo_2", "Filo Afilado II",  "+10% más (total 17%)", 12000, SkillEffect.ExecuteDamageBonus, 0.1f,  "filo_1"),
            N("filo_3", "Filo Afilado III", "+18% más (total 35%)", 20000, SkillEffect.ExecuteDamageBonus, 0.18f, "filo_2"),

            N("reembolso_1", "Pedido Flexible I",  "Al entregar un pedido, recuperas 10% de lo entregado", 5000,  SkillEffect.OrderRefundBonus, 0.10f, "negociador_3"),
            N("reembolso_2", "Pedido Flexible II", "+10% más (total 20%)",                                 13500, SkillEffect.OrderRefundBonus, 0.10f, "reembolso_1"),
 
            N("calma_1", "Calma Antes de la Tormenta I",   "+25% de agua en los primeros 5 segundos del día", 3000,  SkillEffect.EarlyDayWaterBonus, 0.25f, "manofirme_1"),
            N("calma_2", "Calma Antes de la Tormenta II",  "+25% más (total +50%)", 8000,  SkillEffect.EarlyDayWaterBonus, 0.25f, "calma_1"),
            N("calma_3", "Calma Antes de la Tormenta III", "+25% más (total +75%)", 18000, SkillEffect.EarlyDayWaterBonus, 0.25f, "calma_2"),
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
        };
            }
            StampCoconutUnlocks();
        
    }
    private static readonly CoconutUnlockThreshold[] CanonicalUnlocks = new CoconutUnlockThreshold[]
{
    new CoconutUnlockThreshold { coconutName = "Coco Verde",       killsRequired = 0,   ability = CoconutAbility.Ninguna },
new CoconutUnlockThreshold { coconutName = "Coco Maduro",      killsRequired = 25,   ability = CoconutAbility.Generoso },
new CoconutUnlockThreshold { coconutName = "Coco Correoso",    killsRequired = 55,  ability = CoconutAbility.Resistente },
new CoconutUnlockThreshold { coconutName = "Coco Fibroso",     killsRequired = 110,  ability = CoconutAbility.FibraDura },
new CoconutUnlockThreshold { coconutName = "Coco Petreo",      killsRequired = 150,  ability = CoconutAbility.Pesado },
new CoconutUnlockThreshold { coconutName = "Coco Curtido",     killsRequired = 200,  ability = CoconutAbility.Tenaz },
new CoconutUnlockThreshold { coconutName = "Coco Blindado",    killsRequired = 250,  ability = CoconutAbility.Armadura },
new CoconutUnlockThreshold { coconutName = "Coco de Hierro",   killsRequired = 320, ability = CoconutAbility.Rebote },
new CoconutUnlockThreshold { coconutName = "Coco de Acero",    killsRequired = 400, ability = CoconutAbility.Fortificado },
new CoconutUnlockThreshold { coconutName = "Coco de Titanio",  killsRequired = 490, ability = CoconutAbility.Implacable },
new CoconutUnlockThreshold { coconutName = "Coco de Diamante", killsRequired = 590, ability = CoconutAbility.FragilValioso },
new CoconutUnlockThreshold { coconutName = "Coco Legendario",  killsRequired = 700, ability = CoconutAbility.Regeneracion },
new CoconutUnlockThreshold { coconutName = "Coco Mitico",      killsRequired = 830, ability = CoconutAbility.Camuflaje },
new CoconutUnlockThreshold { coconutName = "Coco Ancestral",   killsRequired = 10000, ability = CoconutAbility.Maldicion },
new CoconutUnlockThreshold { coconutName = "Coco Supremo",     killsRequired = 10100, ability = CoconutAbility.Supremo },
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
            coconutUnlocks[i].killsRequired = CanonicalUnlocks[i].killsRequired;
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
