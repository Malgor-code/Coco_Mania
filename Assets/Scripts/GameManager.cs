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

    [Header("Economia (se resetea en bancarrota)")]
    public int money = 0;
    public int daysLeft = 6;
    public int dayLimitBase = 6;
    public int billAmount = 40;
    public int billCycle = 1;
    private bool billPaid = false;

    [Header("Coco / dano")]
    public int coconutHpMaxBase = 40;
    [Tooltip("Cuanto se MULTIPLICA el HP de los cocos en cada ciclo de deuda nuevo (1.35 = +35% por ciclo). Crecimiento exponencial para acompañar el ritmo de precios/mejoras a lo largo de una partida larga.")]
    public float coconutHpGrowthPerCycle = 1.35f;

    [Header("Progreso permanente (para desbloquear tipos de coco)")]
    public int totalCoconutsKilled = 0;

    [Header("Estadisticas de ESTA partida (se resetean al empezar de nuevo)")]
    public int runCoconutsKilled = 0;
    public int runMoneyEarned = 0;

    [Header("Energia (stamina) - se mide en SEGUNDOS, se gasta por tiempo, no por golpe")]
    public float stamina = 15f;
    public float staminaMaxBase = 15f;

    [Header("Ingresos por coco")]
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
    public TMP_Text runCycleText;
    public Button continueButton;

    [Header("UI - Panel del Arbol de Mejoras")]
    public GameObject upgradeTreePanel;

    [Header("UI - Pan y Zoom del Arbol de Mejoras")]
    [Tooltip("El RectTransform que contiene los nodos y se mueve/escala. Si tu panel tiene un Viewport + Content (con Mask/RectMask2D en el Viewport), asigna el Content aca. Si no tenes esa estructura, podes asignar el mismo RectTransform del upgradeTreePanel, pero entonces no se va a recortar el contenido que quede fuera del panel.")]
    public RectTransform upgradeTreeContent;
    [Tooltip("El Viewport que recorta el Content (el que tiene el Rect Mask 2D). Si lo asignas, el limite de paneo se calcula SOLO segun el tamano real del Content y del Viewport: nunca vas a poder alejarte tanto que se pierdan los botones de vista. Si lo dejas vacio, se usa el limite fijo 'Pan Limit' de abajo.")]
    public RectTransform upgradeTreeViewport;
    [Tooltip("Boton del mouse para arrastrar (pan). 2 = boton central (rueda).")]
    public int panMouseButton = 2;
    public float panSpeed = 1f;
    [Tooltip("Se usa SOLO si no asignaste Upgrade Tree Viewport arriba.")]
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

    [Header("UI - Panel de Deuda")]
    public GameObject deudaPanel;
    public TMP_Text deudaMoneyText;
    public TMP_Text deudaDaysLeftText;
    public TMP_Text deudaBillText;
    public Button payDebtButton;

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
        // La stamina se gasta por TIEMPO mientras se esta en la fase de golpear,
        // sin importar si el machete conecta o no.
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

    public int GetCoconutHpMax()
    {
        return Mathf.RoundToInt(coconutHpMaxBase * Mathf.Pow(coconutHpGrowthPerCycle, billCycle - 1));
    }

    void RecalculateAllStats()
    {
        MacheteData m = GetCurrentMachete();
        float baseDmg = m != null ? m.baseDamage : 3f;
        float baseSwing = m != null ? m.baseSwingInterval : 1.2f;
        float baseRadius = m != null ? m.baseHitRadius : 1.5f;
        float staminaCostMult = m != null ? m.staminaCostMultiplier : 1f;

        int damage = Mathf.RoundToInt(baseDmg + skillDamageBonus + perkDamageBonus + legacyDamageBonus);
        float swingStaminaCost = Mathf.Max(0.3f, staminaCostMult - skillStaminaCostReduction);

        if (MacheteController.Instance != null)
        {
            MacheteController.Instance.damageOverride = damage;
            MacheteController.Instance.swingStaminaCostOverride = swingStaminaCost;
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

    public void UseStaminaForSwing(float cost)
    {
    }

    public int OnCoconutDestroyed(float lootMultiplier = 1f)
    {
        totalCoconutsKilled++;
        runCoconutsKilled++;

        float cycleBoost = 1f + (billCycle - 1) * billCycleIncomeBoost;
        float extraMult = 1f + skillMoneyMultiplierBonus + perkMoneyMultiplierBonus + legacyExtraMoneyMult;
        int earned = Mathf.RoundToInt((baseCoinsPerKill + Random.Range(0, coinVariance + 1)) * legacyMultiplier * lootMultiplier * cycleBoost * extraMult);

        money += earned;
        runMoneyEarned += earned;
        Log(Loc("log_money_earned", earned));
        return earned;
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

    public void PayDebt()
    {
        if (billPaid || money < billAmount) return;
        money -= billAmount;
        billPaid = true;
        Log(Loc("log_debt_paid"));
        RefreshAllUI();
    }
    public void ContinueToNextDay()
    {
        daysLeft--;

        if (daysLeft <= 0)
        {
            bool paidSuccessfully = billPaid;
            ResolveBillDeadline();

            if (paidSuccessfully)
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

    void ResolveBillDeadline()
    {
        if (billPaid)
        {
            billCycle++;
            billAmount = Mathf.RoundToInt(billAmount * 1.6f);
            daysLeft = dayLimitBase + (billCycle - 1);
            billPaid = false;
            Log(Loc("log_new_cycle", billAmount));
            RecalculateAllStats();
        }
        else
        {
            int gained = Mathf.Max(1, Mathf.RoundToInt(billCycle / 2f));
            legacyPoints += gained;
            totalLegacyPointsEarned += gained;
            legacyMultiplier = 1f + totalLegacyPointsEarned * 0.1f;

            money = 0;
            billCycle = 1;
            billAmount = 10;
            daysLeft = dayLimitBase;
            billPaid = false;

            unlockedSkillIds.Clear();
            skillDamageBonus = 0f;
            skillStaminaBonus = 0f;
            skillSwingIntervalReduction = 0f;
            skillHitRadiusBonus = 0f;
            skillMoneyMultiplierBonus = 0f;
            skillStaminaCostReduction = 0f;

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
        if (runCycleText != null) runCycleText.text = Loc("collection_cycle", billCycle);

        if (deudaMoneyText != null) deudaMoneyText.text = Loc("collection_current_money", money);
        if (deudaDaysLeftText != null) deudaDaysLeftText.text = Loc("debt_days_left", daysLeft);
        if (deudaBillText != null) deudaBillText.text = billPaid ? Loc("debt_paid_label") : Loc("debt_amount_pending", billAmount);
        if (payDebtButton != null) payDebtButton.interactable = !billPaid && money >= billAmount;

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
        new SkillNode { id = "velocidad_1", nodeName = "Manos Rapidas I", description = "Golpea mas seguido", cost = 10, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.SwingIntervalReduction, effectValue = 0.15f },
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
    }
}