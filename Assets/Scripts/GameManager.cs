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

    [Header("Dinero (para mejoras/tienda - NO es lo que se entrega en el pedido)")]
    public int money = 0;

    [Header("Pedidos de agua de coco (reemplaza el viejo sistema de 'deuda en dinero')")]
    [Tooltip("Cuanta agua de coco (en mL) llevas acumulada para el pedido actual")]
    public float waterCurrentML = 0f;
    [Tooltip("Cuanta agua de coco (en mL) pide el cliente este ciclo")]
    public float waterTargetML = 2000f;
    [Tooltip("Cuanto crece el pedido de agua en cada ciclo nuevo (1.6 = +60%)")]
    public float waterTargetGrowth = 1.6f;
    private bool orderFulfilled = false;

    [Header("Cuanta agua suelta cada coco al morir")]
    public float baseWaterPerKill = 150f;
    public float waterVariance = 100f;

    [Header("Dias / ciclos")]
    public int daysLeft = 6;
    public int dayLimitBase = 6;
    public int billCycle = 1; // ciclo de pedidos (nombre interno historico, sin efecto en el jugador)

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
    public int runMoneyEarned = 0;

    [Header("Energia (stamina) - se mide en SEGUNDOS, se gasta por tiempo, no por golpe")]
    public float stamina = 15f;
    public float staminaMaxBase = 15f;

    [Header("Ingresos por coco (dinero)")]
    public int baseCoinsPerKill = 4;
    public int coinVariance = 4;
    public float billCycleIncomeBoost = 0.08f;
    [Header("Machete: progresion lineal (dinero, se resetea en bancarrota)")]
    public List<MacheteData> macheteOptions = new List<MacheteData>();
    public int equippedMacheteIndex = 0;

    [Header("Arbol de Mejoras (dinero, ramificado, se resetea en bancarrota)")]
    public List<SkillNode> skillTree = new List<SkillNode>();
    private HashSet<string> unlockedSkillIds = new HashSet<string>();

    private float skillDamageBonus = 0f;
    private float skillStaminaBonus = 0f;
    private float skillSwingIntervalReduction = 0f;
    private float skillHitRadiusBonus = 0f;
    private float skillMoneyMultiplierBonus = 0f;
    private float skillStaminaCostReduction = 0f;
    private float skillWaterMultiplierBonus = 0f;
    [Header("Perks (1 de 3 al pagar cada cuenta)")]
    public List<PerkOption> perkPool = new List<PerkOption>();
    private PerkOption[] currentPerkChoices = new PerkOption[3];

    private float perkDamageBonus = 0f;
    private float perkStaminaMaxBonus = 0f;
    private float perkMoneyMultiplierBonus = 0f;
    private float perkSwingIntervalReduction = 0f;
    [Header("Legado (permanente, solo se gasta tras una bancarrota)")]
    public int legacyPoints = 0;
    public int totalLegacyPointsEarned = 0;
    public float legacyMultiplier = 1f;
    public List<LegacyItem> legacyShop = new List<LegacyItem>();
    private HashSet<string> purchasedLegacyItems = new HashSet<string>();

    private float legacyDamageBonus = 0f;
    private float legacyStaminaBonus = 0f;
    private float legacyExtraMoneyMult = 0f;
    [Header("UI - Gameplay (solo stamina)")]
    public GameObject gameplayUIPanel;
    public Slider staminaSlider;

    [Header("UI - Dinero actual (opcional, siempre visible en pantalla)")]
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

    [Header("UI - Panel del Pedido (antes 'Deuda')")]
    public GameObject deudaPanel;
    [Tooltip("Texto opcional para mostrar tu dinero actual dentro de este panel (informativo, ya no se gasta aqui)")]
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

    [Header("Gamefeel: contador de dinero 'rodando' en vez de saltar de golpe")]
    public float moneyCounterSpeed = 6f;
    private float displayedMoney = 0f;

    void UpdateMoneyUI()
    {
        if (!Mathf.Approximately(displayedMoney, money))
        {
            float diff = money - displayedMoney;
            float step = diff * moneyCounterSpeed * Time.unscaledDeltaTime;
            if (Mathf.Abs(step) < 1f) step = Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), 1f);
            displayedMoney += step;

            bool overshot = (diff > 0f && displayedMoney > money) || (diff < 0f && displayedMoney < money);
            if (overshot) displayedMoney = money;
        }

        if (moneyText != null) moneyText.text = Loc("collection_current_money", Mathf.RoundToInt(displayedMoney));
    }

    // "1.2 L" / "850 mL" segun la cantidad, para que se lea natural
    public static string FormatWater(float ml)
    {
        if (ml >= 1000f) return (ml / 1000f).ToString("0.0") + " L";
        return Mathf.RoundToInt(ml) + " mL";
    }

    public bool IsHittingPhase() => currentPhase == Phase.Hitting;

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

    // Ya NO crece con billCycle: la dificultad viene de los TIPOS de coco
    // (CoconutTypeData.hpMultiplier), no de un multiplicador por ciclo.
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

    public float GetStaminaMax()
    {
        return staminaMaxBase + skillStaminaBonus + perkStaminaMaxBonus + legacyStaminaBonus;
    }

    public float GetSkillMoneyMultiplierBonusPercent()
    {
        return skillMoneyMultiplierBonus * 100f;
    }

    // Devuelve el proximo tipo de coco todavia NO desbloqueado (el de menor
    // killsRequired entre los que superan totalCoconutsKilled). Null si ya
    // los desbloqueaste todos.
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

    // 0 a 1: que tan cerca estas de desbloquear GetNextLockedCoconut().
    // 1 = ya se desbloqueo (o no hay mas tipos por desbloquear).
    public float GetNextUnlockProgress01()
    {
        CoconutUnlockThreshold next = GetNextLockedCoconut();
        if (next == null || next.killsRequired <= 0) return 1f;
        return Mathf.Clamp01((float)totalCoconutsKilled / next.killsRequired);
    }

    // Llamado por CoconutTarget al morir un coco. Reparte DINERO (para
    // mejoras) y AGUA DE COCO (para el pedido) por separado.
    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained)
    {
        totalCoconutsKilled++;
        runCoconutsKilled++;

        float cycleBoost = 1f + (billCycle - 1) * billCycleIncomeBoost;
        float extraMult = 1f + skillMoneyMultiplierBonus + perkMoneyMultiplierBonus + legacyExtraMoneyMult;
        int earned = Mathf.RoundToInt((baseCoinsPerKill + Random.Range(0, coinVariance + 1)) * legacyMultiplier * lootMultiplier * cycleBoost * extraMult);

        money += earned;
        runMoneyEarned += earned;

        float waterExtraMult = 1f + skillWaterMultiplierBonus;
        waterGained = (baseWaterPerKill + Random.Range(0f, waterVariance)) * lootMultiplier * waterExtraMult;
        waterCurrentML += waterGained;

        Log(Loc("log_money_earned", earned));
        return earned;
    }

    // Sobrecarga sin agua, por si algun script viejo todavia llama sin el "out"
    public int OnCoconutDestroyed(float lootMultiplier = 1f)
    {
        return OnCoconutDestroyed(lootMultiplier, out _);
    }

    void EnterRecaudacionPhase()
    {
        currentPhase = Phase.Shop;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(true);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        RefreshAllUI();
    }

    void ShowHittingUI()
    {
        currentPhase = Phase.Hitting;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(true);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);

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

    // Antes "PayDebt": ahora entrega el agua acumulada si alcanza el pedido.
    public void PayDebt()
    {
        if (orderFulfilled || waterCurrentML < waterTargetML) return;
        waterCurrentML -= waterTargetML;
        orderFulfilled = true;
        Log("Pedido entregado.");
        RefreshAllUI();
    }

    public void ContinueToNextDay()
    {
        daysLeft--;

        if (daysLeft <= 0)
        {
            bool fulfilledSuccessfully = orderFulfilled;
            ResolveOrderDeadline();

            if (fulfilledSuccessfully)
            {
                OfferPerkChoice();
            }

            RefreshAllUI();
            return;
        }

        stamina = GetStaminaMax();
        ShowHittingUI();
        RefreshAllUI();
    }

    void ResolveOrderDeadline()
    {
        if (orderFulfilled)
        {
            billCycle++;
            waterTargetML *= waterTargetGrowth;
            daysLeft = dayLimitBase + (billCycle - 1);
            orderFulfilled = false;
            Log("Nuevo pedido: " + FormatWater(waterTargetML));
            RecalculateAllStats();
        }
        else
        {
            // No se entrego el pedido a tiempo -> bancarrota, igual que antes
            // pero ahora disparado por el AGUA, no por dinero.
            int gained = Mathf.Max(1, Mathf.RoundToInt(billCycle / 2f));
            legacyPoints += gained;
            totalLegacyPointsEarned += gained;
            legacyMultiplier = 1f + totalLegacyPointsEarned * 0.1f;

            money = 0;
            billCycle = 1;
            waterCurrentML = 0f;
            waterTargetML = 2000f;
            daysLeft = dayLimitBase;
            orderFulfilled = false;

            // El progreso de desbloqueo de tipos de coco TAMBIEN se resetea:
            // es progreso de ESTA partida, no permanente entre bancarrotas.
            totalCoconutsKilled = 0;

            unlockedSkillIds.Clear();
            skillDamageBonus = 0f;
            skillStaminaBonus = 0f;
            skillSwingIntervalReduction = 0f;
            skillHitRadiusBonus = 0f;
            skillMoneyMultiplierBonus = 0f;
            skillStaminaCostReduction = 0f;
            skillWaterMultiplierBonus = 0f;

            equippedMacheteIndex = 0;

            perkDamageBonus = 0f;
            perkStaminaMaxBonus = 0f;
            perkMoneyMultiplierBonus = 0f;
            perkSwingIntervalReduction = 0f;

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
    }

    void EnterBetweenRunsPhase()
    {
        currentPhase = Phase.BetweenRuns;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(true);
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

        if (money < next.unlockCost) return;
        money -= next.unlockCost;
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
        if (money < node.cost) return false;

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

        money -= node.cost;
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
            case SkillEffect.MoneyMultiplierBonus: skillMoneyMultiplierBonus += node.effectValue; break;
            case SkillEffect.StaminaCostReduction: skillStaminaCostReduction += node.effectValue; break;
            case SkillEffect.WaterMultiplierBonus: skillWaterMultiplierBonus += node.effectValue; break;
            case SkillEffect.ExtraStartingCoconut:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(node.effectValue);
                break;
            case SkillEffect.SpawnIntervalReduction:
                if (CoconutSpawner.Instance != null)
                    CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - node.effectValue);
                break;
        }
        RecalculateAllStats();
    }

    void OfferPerkChoice()
    {
        currentPhase = Phase.PerkChoice;
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(true);

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
            case PerkEffect.MoneyMultiplierBonus: perkMoneyMultiplierBonus += perk.value; break;
            case PerkEffect.SwingIntervalReduction: perkSwingIntervalReduction += perk.value; break;
            case PerkEffect.ExtraCoconutNextCycle:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(perk.value);
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

    void ApplyLegacyEffect(LegacyItem item)
    {
        switch (item.effect)
        {
            case LegacyEffect.PermanentDamageBonus: legacyDamageBonus += item.value; break;
            case LegacyEffect.PermanentStaminaMaxBonus: legacyStaminaBonus += item.value; break;
            case LegacyEffect.PermanentMoneyMultiplierBonus: legacyExtraMoneyMult += item.value; break;
            case LegacyEffect.PermanentStartingCoconutBonus:
                if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += Mathf.RoundToInt(item.value);
                break;
            case LegacyEffect.PermanentLegacyMultiplierBonus: legacyMultiplier += item.value; break;
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

        if (runKillsText != null) runKillsText.text = Loc("collection_coconuts_killed", runCoconutsKilled);
        if (runMoneyText != null) runMoneyText.text = Loc("collection_money_earned", runMoneyEarned);

        // Progreso de desbloqueo del proximo tipo de coco.
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

        if (deudaMoneyText != null) deudaMoneyText.text = Loc("collection_current_money", money);

        // Primer texto: cuanto FALTA para completar el pedido.
        if (deudaBillText != null)
        {
            if (orderFulfilled) deudaBillText.text = "¡Pedido listo!";
            else
            {
                float remaining = Mathf.Max(0f, waterTargetML - waterCurrentML);
                deudaBillText.text = "Faltan " + FormatWater(remaining);
            }
        }

        // Segundo texto: dias restantes, con aviso urgente el ultimo dia
        // para que no te agarre de sorpresa la bancarrota.
        if (deudaDaysLeftText != null)
        {
            if (orderFulfilled)
            {
                deudaDaysLeftText.text = "Pedido cumplido";
                deudaDaysLeftText.color = normalDaysColor;
            }
            else if (daysLeft <= 1)
            {
                deudaDaysLeftText.text = "¡PAGALO YA!";
                deudaDaysLeftText.color = urgentColor;
            }
            else
            {
                deudaDaysLeftText.text = daysLeft + " dias restantes";
                deudaDaysLeftText.color = normalDaysColor;
            }
        }

        if (payDebtButton != null) payDebtButton.interactable = !orderFulfilled && waterCurrentML >= waterTargetML;

        MacheteData current = GetCurrentMachete();
        MacheteData next = GetNextMachete();
        if (macheteStatusText != null) macheteStatusText.text = Loc("shop_current_machete", current != null ? current.macheteName : "-");
        if (next != null)
        {
            if (macheteUpgradeCostText != null) macheteUpgradeCostText.text = Loc("shop_next_machete", next.macheteName, next.unlockCost);
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = money >= next.unlockCost;
        }
        else
        {
            if (macheteUpgradeCostText != null) macheteUpgradeCostText.text = Loc("shop_max_machete");
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = false;
        }

        if (legacyPointsText != null) legacyPointsText.text = Loc("betweenruns_legacy_points", legacyPoints);
    }

    void EnsureDefaultData()
    {
        if (macheteOptions == null || macheteOptions.Count == 0)
        {
            macheteOptions = new List<MacheteData>
            {
                new MacheteData { macheteName = "Machete Oxidado", description = "El que ya tienes.", baseDamage = 3f, baseSwingInterval = 1.2f, baseHitRadius = 1.5f, staminaCostMultiplier = 1f, unlockCost = 0 },
                new MacheteData { macheteName = "Machete Normal", description = "Mas daño, mas rapido, mas rango.", baseDamage = 5f, baseSwingInterval = 1.05f, baseHitRadius = 1.6f, staminaCostMultiplier = 0.95f, unlockCost = 300 },
                new MacheteData { macheteName = "Machete de Acero", description = "Un salto grande de poder.", baseDamage = 8f, baseSwingInterval = 0.9f, baseHitRadius = 1.75f, staminaCostMultiplier = 0.9f, unlockCost = 1500 },
                new MacheteData { macheteName = "Machete de Oro", description = "El mejor de todos.", baseDamage = 13f, baseSwingInterval = 0.75f, baseHitRadius = 1.9f, staminaCostMultiplier = 0.85f, unlockCost = 6000 },
            };
        }

        if (skillTree == null || skillTree.Count == 0)
        {
            skillTree = new List<SkillNode>
            {
               new SkillNode { id = "fuerza_1", nodeName = "Mas Fuerza I", description = "+2 de dano. Abre el resto del arbol.", cost = 1, prerequisiteIds = new string[0], effect = SkillEffect.DamageFlatBonus, effectValue = 2f },
        new SkillNode { id = "fuerza_2", nodeName = "Mas Fuerza II", description = "+3 de dano", cost = 35, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.DamageFlatBonus, effectValue = 3f },
        new SkillNode { id = "fuerza_3", nodeName = "Mas Fuerza III", description = "+5 de dano", cost = 120, prerequisiteIds = new [] { "fuerza_2" }, effect = SkillEffect.DamageFlatBonus, effectValue = 5f },
        new SkillNode { id = "velocidad_1", nodeName = "Manos Rapidas I", description = "Golpea mas seguido", cost = 5, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.SwingIntervalReduction, effectValue = 0.15f },
        new SkillNode { id = "velocidad_2", nodeName = "Manos Rapidas II", description = "Golpea aun mas seguido", cost = 55, prerequisiteIds = new [] { "velocidad_1" }, effect = SkillEffect.SwingIntervalReduction, effectValue = 0.2f },
        new SkillNode { id = "suerte_1", nodeName = "Buen Ojo I", description = "+15% de dinero por coco", cost = 20, prerequisiteIds = new [] { "fuerza_2" }, effect = SkillEffect.MoneyMultiplierBonus, effectValue = 0.15f },
        new SkillNode { id = "suerte_2", nodeName = "Buen Ojo II", description = "+20% de dinero por coco", cost = 140, prerequisiteIds = new [] { "suerte_1" }, effect = SkillEffect.MoneyMultiplierBonus, effectValue = 0.2f },
        new SkillNode { id = "radio_1", nodeName = "Golpe Amplio I", description = "Mas radio de golpe", cost = 50, prerequisiteIds = new [] { "velocidad_1" }, effect = SkillEffect.HitRadiusBonus, effectValue = 0.2f },
        new SkillNode { id = "radio_2", nodeName = "Golpe Amplio II", description = "Aun mas radio de golpe", cost = 95, prerequisiteIds = new [] { "radio_1" }, effect = SkillEffect.HitRadiusBonus, effectValue = 0.3f },
        new SkillNode { id = "resistencia_1", nodeName = "Aguante I", description = "+6 segundos de resistencia", cost = 20, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 6f },
        new SkillNode { id = "resistencia_2", nodeName = "Aguante II", description = "+6 segundos de resistencia", cost = 60, prerequisiteIds = new [] { "resistencia_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 6f },
        new SkillNode { id = "resistencia_3", nodeName = "Aguante III", description = "+6 segundos de resistencia", cost = 105, prerequisiteIds = new [] { "eficiencia_1", "cocos_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 6f },
        new SkillNode { id = "eficiencia_1", nodeName = "Golpe Eficiente", description = "Cada golpe gasta menos energia", cost = 30, prerequisiteIds = new [] { "resistencia_2" }, effect = SkillEffect.StaminaCostReduction, effectValue = 0.15f },
        new SkillNode { id = "cocos_1", nodeName = "Cosecha Inicial", description = "+1 coco al iniciar el dia", cost = 15, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.ExtraStartingCoconut, effectValue = 1f },
        new SkillNode { id = "cocos_2", nodeName = "Mejores vendedores", description = "+1 coco al iniciar el dia", cost = 40, prerequisiteIds = new [] { "aparicion_1" }, effect = SkillEffect.ExtraStartingCoconut, effectValue = 2f },
        new SkillNode { id = "cocos_3", nodeName = "Cocos locos", description = "+1 coco al iniciar el dia", cost = 100, prerequisiteIds = new [] { "aparicion_2" }, effect = SkillEffect.ExtraStartingCoconut, effectValue = 3f },
        new SkillNode { id = "aparicion_1", nodeName = "Cosecha Rapida", description = "Los cocos aparecen mas seguido", cost = 24, prerequisiteIds = new [] { "cocos_1" }, effect = SkillEffect.SpawnIntervalReduction, effectValue = 0.5f },
        new SkillNode { id = "aparicion_2", nodeName = "Cosecha Papidisima", description = "Los cocos aparecen mas seguido", cost = 55, prerequisiteIds = new [] { "cocos_2" }, effect = SkillEffect.SpawnIntervalReduction, effectValue = 0.7f },
        new SkillNode { id = "aparicion_3", nodeName = "Cosecha Veloz", description = "Los cocos aparecen mas seguido", cost = 120, prerequisiteIds = new [] { "cocos_3" }, effect = SkillEffect.SpawnIntervalReduction, effectValue = 1f },
        new SkillNode { id = "leche_1", nodeName = "Coco Lechero I", description = "+15% de agua de coco por golpe", cost = 20, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.WaterMultiplierBonus, effectValue = 0.15f },
        new SkillNode { id = "leche_2", nodeName = "Coco Lechero II", description = "+20% de agua de coco por golpe", cost = 65, prerequisiteIds = new [] { "leche_1" }, effect = SkillEffect.WaterMultiplierBonus, effectValue = 0.2f },
        new SkillNode { id = "leche_3", nodeName = "Coco Lechero III", description = "+30% de agua de coco por golpe", cost = 150, prerequisiteIds = new [] { "leche_2" }, effect = SkillEffect.WaterMultiplierBonus, effectValue = 0.3f },
            };
        }

        if (perkPool == null || perkPool.Count == 0)
        {
            perkPool = new List<PerkOption>
            {
                new PerkOption { perkName = "Manos Firmes", description = "+2 de dano por el resto del ciclo", effect = PerkEffect.DamageBonus, value = 2f },
                new PerkOption { perkName = "Segundo Aire", description = "+5 de energia maxima por el resto del ciclo", effect = PerkEffect.StaminaMaxBonus, value = 5f },
                new PerkOption { perkName = "Buen Trato", description = "+10% de dinero por el resto del ciclo", effect = PerkEffect.MoneyMultiplierBonus, value = 0.1f },
                new PerkOption { perkName = "Reflejos", description = "Golpea un poco mas seguido por el resto del ciclo", effect = PerkEffect.SwingIntervalReduction, value = 0.05f },
                new PerkOption { perkName = "Cosecha Extra", description = "+2 cocos al iniciar el proximo dia", effect = PerkEffect.ExtraCoconutNextCycle, value = 2f },
            };
        }

        if (legacyShop == null || legacyShop.Count == 0)
        {
            legacyShop = new List<LegacyItem>
            {
                new LegacyItem { itemName = "Anillo del Machetero", description = "+1 de dano PERMANENTE", cost = 1, effect = LegacyEffect.PermanentDamageBonus, value = 1f },
                new LegacyItem { itemName = "Pulsera de Aguante", description = "+5 de energia maxima PERMANENTE", cost = 1, effect = LegacyEffect.PermanentStaminaMaxBonus, value = 5f },
                new LegacyItem { itemName = "Amuleto del Cobrador", description = "+10% de dinero PERMANENTE", cost = 2, effect = LegacyEffect.PermanentMoneyMultiplierBonus, value = 0.1f },
                new LegacyItem { itemName = "Cesta Grande", description = "+1 coco al iniciar cada dia, PERMANENTE", cost = 2, effect = LegacyEffect.PermanentStartingCoconutBonus, value = 1f },
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