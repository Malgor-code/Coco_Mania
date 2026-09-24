using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum Phase { MainMenu, Hitting, Shop, PerkChoice, BetweenRuns }

    public static GameManager Instance;

    [Header("Fase actual")]
    public Phase currentPhase = Phase.MainMenu;

    [Header("Machete (se ACTIVA recien al empezar a golpear, se apaga en menus/paneles)")]
    [Tooltip("El GameObject raiz del machete (el que tiene MacheteController). Se activa en Play y en Siguiente Dia, y se desactiva en cualquier otra fase (tienda, pedido, arbol, perks, entre-partidas).")]
    public GameObject machete;

    [Header("OBSOLETO - ya no se usa (la economia es 100% agua de coco). Se deja declarado para no romper otros scripts que lo referencien, pero siempre queda en 0.")]
    public int money = 0;

    [Header("Agua de coco (UNICO recurso del juego: sirve para el pedido Y para pagar mejoras/machetes)")]
    [Tooltip("Cuanta agua de coco (en mL) tienes acumulada ahora mismo. Se gasta tanto al entregar el pedido como al comprar mejoras en la tienda/arbol - por eso hay que elegir entre progresar o guardar para el pedido.")]
    public float waterCurrentML = 0f;
    [Tooltip("Cuanta agua de coco (en mL) pide el cliente este ciclo")]
    public float waterTargetML = 500f;
    [Tooltip("Cuanto crece el pedido de agua en cada ciclo nuevo (2 = se duplica)")]
    public float waterTargetGrowth = 2f;
    [Header("Cuanta agua suelta cada coco al morir")]
    public float baseWaterPerKill = 20f;
    public float waterVariance = 40f;
    [Header("Skill Tree - Critico")]
    public float skillCritChanceBonus = 0f;
    [Header("Dias / ciclos")]
    public int daysLeft = 6;
    public int dayLimitBase = 6;
    public int billCycle = 1;

    [Header("Coco / dano")]
    [Tooltip("HP base de un coco 'Coco Verde' (tier inicial). Ya NO crece con los ciclos de pedido - la dificultad ahora viene 100% de los TIPOS de coco que se van desbloqueando (mas duros y mas rentables).")]
    public int coconutHpMaxBase = 40;

    [Header("Progreso permanente (para desbloquear tipos de coco) - se resetea en bancarrota")]
    public int totalCoconutsKilled = 0;

    [System.Serializable]
    public class CoconutUnlockThreshold
    {
        public string coconutName;
        [Tooltip("Imagen/silueta de este tipo de coco, para mostrar en el Panel de Recaudacion mientras esta bloqueado.")]
        public Sprite icon;
        [Tooltip("Cuantos cocos totales (de ESTA partida, se resetea en bancarrota) hay que matar para desbloquearlo.")]
        public int killsRequired;
    }

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

    [Header("Energia (stamina) - se mide en SEGUNDOS, se gasta por tiempo, no por golpe")]
    public float stamina = 15f;
    public float staminaMaxBase = 15f;

    [Header("Monedas raras (coleccionable: no se ganan por golpe, es un drop raro al matar un coco)")]
    [Tooltip("Cuantas monedas has encontrado. Es un contador/coleccionable; no se gasta en nada todavia.")]
    public int coconutCoins = 0;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que un coco suelte 1 moneda al morir. Empieza en 0.5% (0.005) y sube con los nodos 'Suerte de Moneda' del arbol de mejoras.")]
    public float coinDropChance = 0.005f;

    [Header("Machete: progresion lineal (se paga con agua de coco, se resetea en bancarrota)")]
    public List<MacheteData> macheteOptions = new List<MacheteData>();
    public int equippedMacheteIndex = 0;

    [Header("Arbol de Mejoras (se paga con agua de coco, ramificado, se resetea en bancarrota)")]
    public List<SkillNode> skillTree = new List<SkillNode>();
    private HashSet<string> unlockedSkillIds = new HashSet<string>();

    private float skillDamageBonus = 0f;
    private float skillStaminaBonus = 0f;
    private float skillSwingIntervalReduction = 0f;
    private float skillHitRadiusBonus = 0f;
    [Tooltip("Bonus de agua extra por golpe. Alimentado tanto por nodos WaterMultiplierBonus (ej. 'Coco Lechero') como por nodos MoneyMultiplierBonus reciclados (ej. 'Buen Ojo'), ya que ahora todo bonus de 'ingreso' es agua.")]
    private float skillWaterMultiplierBonus = 0f;
    private float skillStaminaCostReduction = 0f;
    [Header("Perks (1 de 3 al pagar cada cuenta)")]
    public List<PerkOption> perkPool = new List<PerkOption>();
    private PerkOption[] currentPerkChoices = new PerkOption[3];
    [Header("Perks")]
    public float perkDamageBonus = 0f;
    public float perkStaminaMaxBonus = 0f;
    public float perkWaterMultiplierBonus = 0f;
    public float perkSwingIntervalReduction = 0f;
    public float perkCritChanceBonus = 0f;

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

    [Header("UI - Recaudacion (hub: estadisticas + 3 botones)")]
    public GameObject recaudacionPanel;
    public TMP_Text runKillsText;
    public TMP_Text runMoneyText;
    public Button continueButton;

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

    [Header("UI - Eleccion de Perk")]
    public GameObject perkChoiceUIPanel;
    public TMP_Text perkOption1Name, perkOption1Desc;
    public TMP_Text perkOption2Name, perkOption2Desc;
    public TMP_Text perkOption3Name, perkOption3Desc;

    [Header("UI - Entre partidas (SOLO aqui se gasta el Legado)")]
    public GameObject betweenRunsPanel;
    public TMP_Text legacyPointsText;
    public TMP_Text bankruptcyMessageText;

    [Header("UI - Log")]
    public TMP_Text logText;

    void Awake()
    {
        Instance = this;
        EnsureDefaultData();
    }

    void Start()
    {
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (machete != null) machete.SetActive(false);

        SetCursorVisible(true);

        RecalculateAllStats();
        UpdateMoneyUI();
    }

    void Update()
    {
        if (currentPhase == Phase.Hitting)
        {
            stamina -= Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                EnterRecaudacionPhase();
            }
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
        daysLeft = dayLimitBase;
        runCoconutsKilled = 0;
        runMoneyEarned = 0;
        RecalculateAllStats();
        ShowHittingUI();
        RefreshAllUI();
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
        float staminaCostMult = m != null ? m.staminaCostMultiplier : 1f;

        int damage = Mathf.RoundToInt(baseDmg + skillDamageBonus + perkDamageBonus + legacyDamageBonus);

        if (MacheteController.Instance != null)
        {
            MacheteController.Instance.damageOverride = damage;
            MacheteController.Instance.swingInterval = Mathf.Max(0.3f, baseSwing - skillSwingIntervalReduction - perkSwingIntervalReduction);
            MacheteController.Instance.hitRadius = baseRadius + skillHitRadiusBonus;
        }
    }
    public int GetCurrentDayLimit()
    {
        int reduction = (billCycle - 1) / 2;
        return Mathf.Max(3, dayLimitBase - reduction);
    }

    public float GetStaminaMax()
    {
        return staminaMaxBase + skillStaminaBonus + perkStaminaMaxBonus + legacyStaminaBonus;
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

    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained)
    {
        totalCoconutsKilled++;
        runCoconutsKilled++;
        dayCoconutsKilled++;

        float waterExtraMult = 1f + skillWaterMultiplierBonus + perkWaterMultiplierBonus + legacyWaterMultiplierBonus;
        waterGained = (baseWaterPerKill + Random.Range(0f, waterVariance)) * lootMultiplier * waterExtraMult;
        waterCurrentML += waterGained;

        int gainedRounded = Mathf.RoundToInt(waterGained);
        runMoneyEarned += gainedRounded;
        dayMoneyEarned += gainedRounded;

        bool foundCoin = Random.value < coinDropChance;
        if (foundCoin)
        {
            coconutCoins++;
            Log(Loc("log_coin_found", coconutCoins));
        }
        else
        {
            Log(Loc("log_money_earned", FormatWater(waterGained)));
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
        if (recaudacionPanel != null) recaudacionPanel.SetActive(true);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);

        SetCursorVisible(true);

        RefreshAllUI();
    }

    void ShowHittingUI()
    {
        currentPhase = Phase.Hitting;
        dayCoconutsKilled = 0;
        dayMoneyEarned = 0;
        if (machete != null) machete.SetActive(true);

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
            CoconutSpawner.Instance.StartNewDay();
        }
    }

    public void ToggleUpgradeTreePanel()
    {
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(!upgradeTreePanel.activeSelf);
        if (upgradeTreePanel != null && upgradeTreePanel.activeSelf) RecenterUpgradeTree();
        RefreshAllUI();
    }

    public void ToggleTiendaPanel()
    {
        if (tiendaPanel != null) tiendaPanel.SetActive(!tiendaPanel.activeSelf);
        RefreshAllUI();
    }

    public void ToggleDeudaPanel()
    {
        if (deudaPanel != null) deudaPanel.SetActive(!deudaPanel.activeSelf);
        RefreshAllUI();
    }
    public void PayDebt()
    {
        if (waterCurrentML < waterTargetML) return;

        waterCurrentML -= waterTargetML;

        billCycle++;
        waterTargetML *= waterTargetGrowth;
        daysLeft = dayLimitBase + (billCycle - 1);
        RecalculateAllStats();

        Log("Pedido entregado. Nuevo pedido: " + FormatWater(waterTargetML));
        RefreshAllUI();

        OfferPerkChoice();
    }

    public void ContinueToNextDay()
    {
        daysLeft--;

        if (daysLeft <= 0 && waterCurrentML < waterTargetML)
        {
            ResolveBankruptcy();
            RefreshAllUI();
            return;
        }

        if (daysLeft <= 0)
        {
            daysLeft = 1;
        }

        stamina = GetStaminaMax();
        ShowHittingUI();
        RefreshAllUI();
    }

    void ResolveBankruptcy()
    {
        if (machete != null) machete.SetActive(false);

        int gained = Mathf.Max(1, Mathf.RoundToInt(billCycle / 2f));
        legacyPoints += gained;
        totalLegacyPointsEarned += gained;
        legacyMultiplier = 1f + totalLegacyPointsEarned * 0.1f;

        money = 0;
        billCycle = 1;
        waterCurrentML = 0f;
        waterTargetML = 2000f;
        daysLeft = dayLimitBase;

        totalCoconutsKilled = 0;

        unlockedSkillIds.Clear();
        skillDamageBonus = 0f;
        skillStaminaBonus = 0f;
        skillSwingIntervalReduction = 0f;
        skillHitRadiusBonus = 0f;
        skillWaterMultiplierBonus = 0f;
        skillStaminaCostReduction = 0f;

        equippedMacheteIndex = 0;

        perkDamageBonus = 0f;
        perkStaminaMaxBonus = 0f;
        perkWaterMultiplierBonus = 0f;
        perkSwingIntervalReduction = 0f;

        coinDropChance = 0.005f;

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.startingCoconuts = 4;
            CoconutSpawner.Instance.spawnInterval = 5f;
        }

        RecalculateAllStats();

        if (bankruptcyMessageText != null)
        {
            bankruptcyMessageText.text = Loc("log_bankruptcy", gained);
        }
        Log(Loc("log_bankruptcy", gained));

        EnterBetweenRunsPhase();
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
            case SkillEffect.StaminaCostReduction: skillStaminaCostReduction += node.effectValue; break;
            case SkillEffect.WaterMultiplierBonus: skillWaterMultiplierBonus += node.effectValue; break;
            case SkillEffect.ExtraStartingCoconut:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(node.effectValue);
                break;
            case SkillEffect.SpawnIntervalReduction:
                if (CoconutSpawner.Instance != null)
                    CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - node.effectValue);
                break;
            case SkillEffect.CoinChanceBonus:
                coinDropChance += node.effectValue;
                break;
            case SkillEffect.CritChanceBonus:
                skillCritChanceBonus += node.effectValue;
                break;
        }
        RecalculateAllStats();
    }

    void OfferPerkChoice()
    {
        currentPhase = Phase.PerkChoice;
        if (machete != null) machete.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(true);

        SetCursorVisible(true);

        currentPerkChoices = PickRandomPerks(3);

        SetPerkLabel(perkOption1Name, perkOption1Desc, currentPerkChoices[0]);
        SetPerkLabel(perkOption2Name, perkOption2Desc, currentPerkChoices[1]);
        SetPerkLabel(perkOption3Name, perkOption3Desc, currentPerkChoices[2]);
    }

    void SetPerkLabel(TMP_Text nameLabel, TMP_Text descLabel, PerkOption perk)
    {
        if (perk == null) return;
        if (nameLabel != null) nameLabel.text = perk.perkName;
        if (descLabel != null) descLabel.text = perk.description;
    }

    PerkOption[] PickRandomPerks(int count)
    {
        List<PerkOption> pool = new List<PerkOption>(perkPool);
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

    public void ChoosePerk(int index)
    {
        if (index < 0 || index >= currentPerkChoices.Length || currentPerkChoices[index] == null) return;

        ApplyPerk(currentPerkChoices[index]);
        Log(Loc("log_perk_chosen", currentPerkChoices[index].perkName));

        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);

        stamina = GetStaminaMax();
        ShowHittingUI();
        RefreshAllUI();
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

        legacyPoints -= item.cost;
        purchasedLegacyItems.Add(itemName);
        ApplyLegacyEffect(item);
        Log(Loc("log_legacy_bought", item.itemName));
        RefreshAllUI();
    }
    public float GetCritChance()
    {
        float chance = skillCritChanceBonus + perkCritChanceBonus + legacyCritChanceBonus;

        return Mathf.Clamp01(chance);
    }
    void ApplyLegacyEffect(LegacyItem item)
    {
        switch (item.effect)
        {
            case LegacyEffect.PermanentDamageBonus: legacyDamageBonus += item.value; break;
            case LegacyEffect.PermanentStaminaMaxBonus: legacyStaminaBonus += item.value; break;
            case LegacyEffect.PermanentMoneyMultiplierBonus: legacyWaterMultiplierBonus += item.value; break;
            case LegacyEffect.PermanentStartingCoconutBonus:if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(item.value);break;
            case LegacyEffect.PermanentLegacyMultiplierBonus: legacyMultiplier += item.value; break;
            case LegacyEffect.PermanentCritChanceBonus:legacyCritChanceBonus += item.value;break;
            case LegacyEffect.PermanentCoinChanceBonus:coinDropChance += item.value;break;

        }
        RecalculateAllStats();
    }
    public void Log(string msg)
    {
        if (logText != null) logText.text = msg;
    }

    void RefreshAllUI()
    {
        UpdateMoneyUI();

        if (runKillsText != null) runKillsText.text = Loc("collection_coconuts_killed", dayCoconutsKilled);
        if (runMoneyText != null) runMoneyText.text = Loc("collection_money_earned", FormatWater(dayMoneyEarned));

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
            if (!readyToDeliver && daysLeft <= 1)
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

    void EnsureDefaultData()
    {
        if (macheteOptions == null || macheteOptions.Count == 0)
        {
            macheteOptions = new List<MacheteData>
            {
                new MacheteData { macheteName = "Machete Oxidado", description = "El que ya tienes.", baseDamage = 3f, baseSwingInterval = 1.2f, baseHitRadius = 1.5f, staminaCostMultiplier = 1f, unlockCost = 0 },
                new MacheteData { macheteName = "Machete Normal", description = "Mas daño, mas rapido, mas rango.", baseDamage = 5f, baseSwingInterval = 1.4f, baseHitRadius = 1.6f, staminaCostMultiplier = 0.95f, unlockCost = 4000 },
                new MacheteData { macheteName = "Machete de Acero", description = "Un salto grande de poder.", baseDamage = 8f, baseSwingInterval = 1f, baseHitRadius = 1.75f, staminaCostMultiplier = 1.3f, unlockCost = 18000 },
                new MacheteData { macheteName = "Machete de Oro", description = "Valioso.", baseDamage = 13f, baseSwingInterval = 0.75f, baseHitRadius = 1.9f, staminaCostMultiplier = 0.85f, unlockCost = 70000 },
                new MacheteData { macheteName = "Machete de Obsidiana", description = "Muy duro.", baseDamage = 19f, baseSwingInterval = 0.68f, baseHitRadius = 2f, staminaCostMultiplier = 0.98f, unlockCost = 180000 },
                new MacheteData { macheteName = "Machete de Cobrador", description = "El mejor de todos.", baseDamage = 28f, baseSwingInterval = 0.6f, baseHitRadius = 2.15f, staminaCostMultiplier = 0.75f, unlockCost = 450000 },
            };
        }

        if (skillTree == null || skillTree.Count == 0)
        {
            skillTree = new List<SkillNode>
            {
               new SkillNode { id = "fuerza_1", nodeName = "Mas Fuerza I", description = "+2 de dano. Abre el resto del arbol.", cost = 50, prerequisiteIds = new string[0], effect = SkillEffect.DamageFlatBonus, effectValue = 2f },
        new SkillNode { id = "fuerza_2", nodeName = "Mas Fuerza II", description = "+3 de dano", cost = 900, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.DamageFlatBonus, effectValue = 3f },
        new SkillNode { id = "fuerza_3", nodeName = "Mas Fuerza III", description = "+5 de dano", cost = 2800, prerequisiteIds = new [] { "fuerza_2" }, effect = SkillEffect.DamageFlatBonus, effectValue = 5f },
        new SkillNode { id = "velocidad_1", nodeName = "Manos Rapidas I", description = "Golpea mas seguido", cost = 150, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.SwingIntervalReduction, effectValue = 0.15f },
        new SkillNode { id = "velocidad_2", nodeName = "Manos Rapidas II", description = "Golpea aun mas seguido", cost = 1300, prerequisiteIds = new [] { "velocidad_1" }, effect = SkillEffect.SwingIntervalReduction, effectValue = 0.2f },
        new SkillNode { id = "suerte_1", nodeName = "Buen Ojo I", description = "+15% de agua por coco", cost = 500, prerequisiteIds = new [] { "fuerza_2" }, effect = SkillEffect.MoneyMultiplierBonus, effectValue = 0.15f },
        new SkillNode { id = "suerte_2", nodeName = "Buen Ojo II", description = "+20% de agua por coco", cost = 3200, prerequisiteIds = new [] { "suerte_1" }, effect = SkillEffect.MoneyMultiplierBonus, effectValue = 0.2f },
        new SkillNode { id = "radio_1", nodeName = "Golpe Amplio I", description = "Mas radio de golpe", cost = 1200, prerequisiteIds = new [] { "velocidad_1" }, effect = SkillEffect.HitRadiusBonus, effectValue = 0.2f },
        new SkillNode { id = "radio_2", nodeName = "Golpe Amplio II", description = "Aun mas radio de golpe", cost = 2200, prerequisiteIds = new [] { "radio_1" }, effect = SkillEffect.HitRadiusBonus, effectValue = 0.3f },
        new SkillNode { id = "resistencia_1", nodeName = "Aguante I", description = "+6 segundos de resistencia", cost = 500, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 6f },
        new SkillNode { id = "resistencia_2", nodeName = "Aguante II", description = "+6 segundos de resistencia", cost = 1400, prerequisiteIds = new [] { "resistencia_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 6f },
        new SkillNode { id = "resistencia_3", nodeName = "Aguante III", description = "+6 segundos de resistencia", cost = 2400, prerequisiteIds = new [] { "eficiencia_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 6f },
        new SkillNode { id = "eficiencia_1", nodeName = "Golpe Eficiente", description = "Cada golpe gasta menos energia", cost = 750, prerequisiteIds = new [] { "resistencia_2" }, effect = SkillEffect.StaminaCostReduction, effectValue = 0.15f },
        new SkillNode { id = "cocos_1", nodeName = "Cosecha Inicial", description = "+1 coco al iniciar el dia", cost = 400, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.ExtraStartingCoconut, effectValue = 1f },
        new SkillNode { id = "cocos_2", nodeName = "Mejores vendedores", description = "+1 coco al iniciar el dia", cost = 950, prerequisiteIds = new [] { "aparicion_1" }, effect = SkillEffect.ExtraStartingCoconut, effectValue = 2f },
        new SkillNode { id = "cocos_3", nodeName = "Cocos locos", description = "+1 coco al iniciar el dia", cost = 2300, prerequisiteIds = new [] { "aparicion_2" }, effect = SkillEffect.ExtraStartingCoconut, effectValue = 3f },
        new SkillNode { id = "aparicion_1", nodeName = "Cosecha Rapida", description = "Los cocos aparecen mas seguido", cost = 600, prerequisiteIds = new [] { "cocos_1" }, effect = SkillEffect.SpawnIntervalReduction, effectValue = 0.5f },
        new SkillNode { id = "aparicion_2", nodeName = "Cosecha Papidisima", description = "Los cocos aparecen mas seguido", cost = 1300, prerequisiteIds = new [] { "cocos_2" }, effect = SkillEffect.SpawnIntervalReduction, effectValue = 0.7f },
        new SkillNode { id = "aparicion_3", nodeName = "Cosecha Veloz", description = "Los cocos aparecen mas seguido", cost = 2800, prerequisiteIds = new [] { "cocos_3" }, effect = SkillEffect.SpawnIntervalReduction, effectValue = 1f },
        new SkillNode { id = "leche_1", nodeName = "Coco Lechero I", description = "+15% de agua de coco por golpe", cost = 500, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.WaterMultiplierBonus, effectValue = 0.15f },
        new SkillNode { id = "leche_2", nodeName = "Coco Lechero II", description = "+20% de agua de coco por golpe", cost = 1500, prerequisiteIds = new [] { "leche_1" }, effect = SkillEffect.WaterMultiplierBonus, effectValue = 0.2f },
        new SkillNode { id = "leche_3", nodeName = "Coco Lechero III", description = "+30% de agua de coco por golpe", cost = 3400, prerequisiteIds = new [] { "leche_2" }, effect = SkillEffect.WaterMultiplierBonus, effectValue = 0.3f },
        new SkillNode { id = "moneda_1", nodeName = "Suerte de Moneda I", description = "+0.25% de probabilidad de encontrar una moneda por golpe", cost = 1000, prerequisiteIds = new [] { "velocidad_2" }, effect = SkillEffect.CoinChanceBonus, effectValue = 0.0025f },
        new SkillNode { id = "moneda_2", nodeName = "Suerte de Moneda II", description = "+0.5% de probabilidad de encontrar una moneda por golpe", cost = 6000, prerequisiteIds = new [] { "moneda_1" }, effect = SkillEffect.CoinChanceBonus, effectValue = 0.005f },
        new SkillNode { id = "moneda_3", nodeName = "Suerte de Moneda III", description = "+1% de probabilidad de encontrar una moneda por golpe", cost = 20000, prerequisiteIds = new [] { "moneda_2" }, effect = SkillEffect.CoinChanceBonus, effectValue = 0.01f },
        new SkillNode { id = "critico_1",nodeName = "Golpe Certero I",description = "+5% de probabilidad de golpe crítico.",cost = 1000,prerequisiteIds = new[] { "velocidad_2" },effect = SkillEffect.CritChanceBonus,effectValue = 0.05f},
        new SkillNode { id = "critico_2",nodeName = "Golpe Certero II",description = "+7% de probabilidad de golpe crítico.",cost = 3500,prerequisiteIds = new[] { "critico_1" },effect = SkillEffect.CritChanceBonus,effectValue = 0.07f},
        new SkillNode { id = "critico_3",nodeName = "Golpe Certero III",description = "+8% de probabilidad de golpe crítico.",cost = 9000,prerequisiteIds = new[] { "critico_2" },effect = SkillEffect.CritChanceBonus,effectValue = 0.08f},
        new SkillNode { id = "critico_4",nodeName = "Golpe Certero IV",description = "+10% de probabilidad de golpe crítico.",cost = 20000,prerequisiteIds = new[] { "critico_3" },effect = SkillEffect.CritChanceBonus,effectValue = 0.10f},
            };
        }

        if (perkPool == null || perkPool.Count == 0)
        {
            perkPool = new List<PerkOption>
            {
                new PerkOption { perkName = "Manos Firmes",description = "+4 daño.",effect = PerkEffect.DamageBonus,value = 4f},
                new PerkOption { perkName = "Segundo Aire",description = "+8 stamina máxima.",effect = PerkEffect.StaminaMaxBonus,value = 8f},
                new PerkOption { perkName = "Buen Trato",description = "+15% agua obtenida.",effect = PerkEffect.WaterMultiplierBonus,value = 0.15f},
                new PerkOption { perkName = "Reflejos",description = "-0.08 segundos entre golpes.", effect = PerkEffect.SwingIntervalReduction,value = 0.08f},
                new PerkOption { perkName = "Golpe Certero",description = "+8% de probabilidad de crítico.",effect = PerkEffect.CritChanceBonus,value = 0.08f},
                new PerkOption { perkName = "Cosecha Extra",description = "+2 cocos al comenzar el siguiente ciclo.",effect = PerkEffect.ExtraCoconutNextCycle,value = 2f},
                new PerkOption { perkName = "Coco Generoso",description = "+25% agua, pero los golpes son 10% más lentos.",effect = PerkEffect.WaterMultiplierBonus,value = 0.25f},
                new PerkOption { perkName = "Golpe de Suerte",description = "+15% crítico y +10% agua.",effect = PerkEffect.CritChanceBonus,value = 0.15f}
            };
        }

        if (legacyShop == null || legacyShop.Count == 0)
        {
            legacyShop = new List<LegacyItem>
            {
                new LegacyItem { itemName = "Anillo del Machetero",description = "+1 daño permanente.",cost = 1,effect = LegacyEffect.PermanentDamageBonus,value = 1f},
                new LegacyItem { itemName = "Pulsera de Aguante",description = "+5 stamina máxima permanente.",cost = 2,effect = LegacyEffect.PermanentStaminaMaxBonus,value = 5f},
                new LegacyItem { itemName = "Amuleto del Cobrador",description = "+10% agua obtenida permanentemente.",cost = 3,effect = LegacyEffect.PermanentMoneyMultiplierBonus,value = 0.10f},
                new LegacyItem { itemName = "Cesta Grande",description = "+1 coco inicial cada día.",cost = 4,effect = LegacyEffect.PermanentStartingCoconutBonus,value = 1f},
                new LegacyItem { itemName = "Lente del Afortunado",description = "+3% probabilidad de crítico permanente.",cost = 5,effect = LegacyEffect.PermanentCritChanceBonus,value = 0.03f},
                new LegacyItem { itemName = "Moneda Antigua",description = "+0.5% probabilidad de encontrar monedas.",cost = 7,effect = LegacyEffect.PermanentCoinChanceBonus,value = 0.005f}
            };
        }

        if (coconutUnlocks == null || coconutUnlocks.Count == 0)
        {
            coconutUnlocks = new List<CoconutUnlockThreshold>
            {
                new CoconutUnlockThreshold { coconutName = "Coco Verde",       killsRequired = 0 },
                new CoconutUnlockThreshold { coconutName = "Coco Maduro",      killsRequired = 10 },
                new CoconutUnlockThreshold { coconutName = "Coco Correoso",    killsRequired = 25 },
                new CoconutUnlockThreshold { coconutName = "Coco Fibroso",     killsRequired = 45 },
                new CoconutUnlockThreshold { coconutName = "Coco Petreo",      killsRequired = 70 },
                new CoconutUnlockThreshold { coconutName = "Coco Curtido",     killsRequired = 100 },
                new CoconutUnlockThreshold { coconutName = "Coco Blindado",    killsRequired = 140 },
                new CoconutUnlockThreshold { coconutName = "Coco de Hierro",   killsRequired = 185 },
                new CoconutUnlockThreshold { coconutName = "Coco de Acero",    killsRequired = 235 },
                new CoconutUnlockThreshold { coconutName = "Coco de Titanio",  killsRequired = 290 },
                new CoconutUnlockThreshold { coconutName = "Coco de Diamante", killsRequired = 350 },
                new CoconutUnlockThreshold { coconutName = "Coco Legendario",  killsRequired = 420 },
                new CoconutUnlockThreshold { coconutName = "Coco Mitico",      killsRequired = 500 },
                new CoconutUnlockThreshold { coconutName = "Coco Ancestral",   killsRequired = 600 },
                new CoconutUnlockThreshold { coconutName = "Coco Supremo",     killsRequired = 720 },
            };
        }
    }
}