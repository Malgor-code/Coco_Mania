using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum Phase { Hitting, Shop, PerkChoice }

    public static GameManager Instance;

    [Header("Fase actual")]
    public Phase currentPhase = Phase.Hitting;

    [Header("Economia")]
    public int money = 0;
    public int daysLeft = 6;
    public int dayLimitBase = 6;
    public int billAmount = 40;
    public int billCycle = 1;
    private bool billPaid = false;

    [Header("Coco / dano")]
    public int coconutHpMaxBase = 10;

    [Header("Progreso (para desbloquear tipos de coco)")]
    public int totalCoconutsKilled = 0;

    [Header("Energia (stamina)")]
    public float stamina = 20f;
    public float staminaMaxBase = 20f;

    [Header("Niveles de mejora - combate (se resetean en bancarrota)")]
    public int dmgLevel = 1;
    public int stamLevel = 1;
    public int efficiencyLevel = 1;
    public int swingSpeedLevel = 1;
    public int hitRadiusLevel = 1;

    [Header("Niveles de mejora - cocos (se resetean en bancarrota)")]
    public int extraCoconutLevel = 1;
    public int spawnSpeedLevel = 1;

    [Header("Balance economico (partidas largas, ~3 horas)")]
    public int baseUpgradeCost = 12;
    public float costGrowthRate = 1.42f;
    public float globalCostGrowth = 1.015f;
    public int totalUpgradesPurchased = 0;

    [Header("Ingresos por coco")]
    public int baseCoinsPerKill = 4;
    public int coinVariance = 4;
    public float billCycleIncomeBoost = 0.08f;

    [Header("Machetes seleccionables (permanente)")]
    public List<MacheteData> macheteOptions = new List<MacheteData>();
    public int equippedMacheteIndex = 0;
    private HashSet<int> unlockedMachetes = new HashSet<int> { 0 };

    [Header("Arbol de Habilidades (permanente, Puntos de Habilidad)")]
    public List<SkillNode> skillTree = new List<SkillNode>();
    public int skillPoints = 0;
    private HashSet<string> unlockedSkillIds = new HashSet<string>();

    private float skillDamageBonus = 0f;
    private float skillStaminaBonus = 0f;
    private float skillSwingIntervalReduction = 0f;
    private float skillHitRadiusBonus = 0f;
    private float skillMoneyMultiplierBonus = 0f;

    [Header("Perks (1 de 3 al pagar cada cuenta)")]
    public List<PerkOption> perkPool = new List<PerkOption>();
    private PerkOption[] currentPerkChoices = new PerkOption[3];

    private float perkDamageBonus = 0f;
    private float perkStaminaMaxBonus = 0f;
    private float perkMoneyMultiplierBonus = 0f;
    private float perkSwingIntervalReduction = 0f;

    [Header("Legado (permanente, Puntos de Legado)")]
    public int legacyPoints = 0;
    public int totalLegacyPointsEarned = 0;
    public float legacyMultiplier = 1f;
    public List<LegacyItem> legacyShop = new List<LegacyItem>();
    private HashSet<string> purchasedLegacyItems = new HashSet<string>();

    private float legacyDamageBonus = 0f;
    private float legacyStaminaBonus = 0f;
    private float legacyExtraMoneyMult = 0f;

    // ---------------- UI ----------------
    [Header("UI - Gameplay (solo stamina)")]
    public GameObject gameplayUIPanel;
    public Slider staminaSlider;

    [Header("UI - Tienda / Fin de energia")]
    public GameObject shopUIPanel;
    public TMP_Text shopMoneyText;
    public TMP_Text shopDaysLeftText;
    public TMP_Text shopBillText;
    public Button payDebtButton;
    public Button continueButton;

    [Header("UI - Panel de Mejoras")]
    public GameObject upgradesPanel;
    public TMP_Text dmgCostText;
    public TMP_Text stamCostText;
    public TMP_Text efficiencyCostText;
    public TMP_Text extraCoconutCostText;
    public TMP_Text spawnSpeedCostText;
    public TMP_Text swingSpeedCostText;
    public TMP_Text hitRadiusCostText;

    [Header("UI - Panel de Machetes")]
    public GameObject macheteSelectPanel;

    [Header("UI - Panel de Habilidades")]
    public GameObject skillTreePanel;
    public TMP_Text skillPointsText;

    [Header("UI - Panel de Legado")]
    public GameObject legacyShopPanel;
    public TMP_Text legacyPointsText;

    [Header("UI - Eleccion de Perk")]
    public GameObject perkChoiceUIPanel;
    public TMP_Text perkOption1Name, perkOption1Desc;
    public TMP_Text perkOption2Name, perkOption2Desc;
    public TMP_Text perkOption3Name, perkOption3Desc;

    [Header("UI - Log")]
    public TMP_Text logText;

    void Awake()
    {
        Instance = this;
        EnsureDefaultData();
    }

    void Start()
    {
        daysLeft = dayLimitBase;
        RecalculateAllStats();
        ShowHittingUI();
        RefreshAllUI();
    }

    void Update()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = GetStaminaMax();
            staminaSlider.value = stamina;
        }
    }

    public bool IsHittingPhase() => currentPhase == Phase.Hitting;

    public int GetCoconutHpMax()
    {
        return Mathf.RoundToInt(coconutHpMaxBase + billCycle * 3);
    }
    void RecalculateAllStats()
    {
        MacheteData m = GetEquippedMachete();
        float baseDmg = m != null ? m.baseDamage : 3f;
        float baseSwing = m != null ? m.baseSwingInterval : 1.2f;
        float baseRadius = m != null ? m.baseHitRadius : 1.5f;
        float staminaCostMult = m != null ? m.staminaCostMultiplier : 1f;

        int damage = Mathf.RoundToInt(baseDmg + (dmgLevel - 1) * 2 + skillDamageBonus + perkDamageBonus + legacyDamageBonus);

        float efficiencyReduction = (efficiencyLevel - 1) * 0.1f;
        float swingStaminaCost = Mathf.Max(0.3f, staminaCostMult - efficiencyReduction);

        if (MacheteController.Instance != null)
        {
            MacheteController.Instance.damageOverride = damage;
            MacheteController.Instance.swingStaminaCostOverride = swingStaminaCost;
            MacheteController.Instance.swingInterval = Mathf.Max(0.3f,
                baseSwing - (swingSpeedLevel - 1) * 0.05f - skillSwingIntervalReduction - perkSwingIntervalReduction);
            MacheteController.Instance.hitRadius = baseRadius + (hitRadiusLevel - 1) * 0.15f + skillHitRadiusBonus;
        }
    }

    public float GetStaminaMax()
    {
        return staminaMaxBase + (stamLevel - 1) * 5 + skillStaminaBonus + perkStaminaMaxBonus + legacyStaminaBonus;
    }

    // Llamado por MacheteController en cada golpe
    public void UseStaminaForSwing(float cost)
    {
        if (currentPhase != Phase.Hitting) return;

        stamina -= cost;
        if (stamina <= 0f)
        {
            stamina = 0f;
            EnterShopPhase();
        }
    }
    public void OnCoconutDestroyed(float lootMultiplier = 1f)
    {
        totalCoconutsKilled++;
        float cycleBoost = 1f + (billCycle - 1) * billCycleIncomeBoost;
        float extraMult = 1f + skillMoneyMultiplierBonus + perkMoneyMultiplierBonus + legacyExtraMoneyMult;
        int earned = Mathf.RoundToInt((baseCoinsPerKill + Random.Range(0, coinVariance + 1)) * legacyMultiplier * lootMultiplier * cycleBoost * extraMult);
        money += earned;
        Log("+$" + earned);
    }

    void EnterShopPhase()
    {
        currentPhase = Phase.Shop;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (shopUIPanel != null) shopUIPanel.SetActive(true);
        if (upgradesPanel != null) upgradesPanel.SetActive(false);
        if (macheteSelectPanel != null) macheteSelectPanel.SetActive(false);
        if (skillTreePanel != null) skillTreePanel.SetActive(false);
        if (legacyShopPanel != null) legacyShopPanel.SetActive(false);
        RefreshAllUI();
    }

    void ShowHittingUI()
    {
        currentPhase = Phase.Hitting;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(true);
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.StartNewDay();
        }
    }

    public void ToggleUpgradesPanel()
    {
        if (upgradesPanel != null) upgradesPanel.SetActive(!upgradesPanel.activeSelf);
    }

    public void ToggleMacheteSelectPanel()
    {
        if (macheteSelectPanel != null) macheteSelectPanel.SetActive(!macheteSelectPanel.activeSelf);
    }

    public void ToggleSkillTreePanel()
    {
        if (skillTreePanel != null) skillTreePanel.SetActive(!skillTreePanel.activeSelf);
    }

    public void ToggleLegacyShopPanel()
    {
        if (legacyShopPanel != null) legacyShopPanel.SetActive(!legacyShopPanel.activeSelf);
    }

    public void PayDebt()
    {
        if (billPaid || money < billAmount) return;
        money -= billAmount;
        billPaid = true;
        Log("Deuda pagada.");
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
                RefreshAllUI();
                return; 
            }
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
            skillPoints += 1; 
            Log("Nuevo ciclo. Cuenta: $" + billAmount);
        }
        else
        {
            int gained = Mathf.Max(1, Mathf.RoundToInt(billCycle / 2f));
            legacyPoints += gained;
            totalLegacyPointsEarned += gained;
            legacyMultiplier = 1f + totalLegacyPointsEarned * 0.1f;

            money = 0;
            billCycle = 1;
            billAmount = 40;
            daysLeft = dayLimitBase;
            billPaid = false;

            dmgLevel = 1;
            stamLevel = 1;
            efficiencyLevel = 1;
            swingSpeedLevel = 1;
            hitRadiusLevel = 1;
            extraCoconutLevel = 1;
            spawnSpeedLevel = 1;
            totalUpgradesPurchased = 0;

            perkDamageBonus = 0f;
            perkStaminaMaxBonus = 0f;
            perkMoneyMultiplierBonus = 0f;
            perkSwingIntervalReduction = 0f;

            if (CoconutSpawner.Instance != null)
            {
                CoconutSpawner.Instance.startingCoconuts = 4;
                CoconutSpawner.Instance.spawnInterval = 5f;
            }

            Log("Bancarrota. +" + gained + " puntos de legado.");
        }

        RecalculateAllStats();
    }
    int CostFor(int level)
    {
        float perLevel = Mathf.Pow(costGrowthRate, level - 1);
        float global = Mathf.Pow(globalCostGrowth, totalUpgradesPurchased);
        return Mathf.RoundToInt(baseUpgradeCost * perLevel * global);
    }

    public void BuyDamage()
    {
        int c = CostFor(dmgLevel);
        if (money < c) return;
        money -= c; dmgLevel++; totalUpgradesPurchased++;
        RecalculateAllStats();
        Log("Machete mejorado.");
        RefreshAllUI();
    }

    public void BuyStaminaMax()
    {
        int c = CostFor(stamLevel);
        if (money < c) return;
        money -= c; stamLevel++; totalUpgradesPurchased++;
        RecalculateAllStats();
        Log("Energia maxima: " + GetStaminaMax());
        RefreshAllUI();
    }

    public void BuyEfficiency()
    {
        int c = CostFor(efficiencyLevel);
        if (money < c) return;
        money -= c; efficiencyLevel++; totalUpgradesPurchased++;
        RecalculateAllStats();
        Log("Machete mas eficiente.");
        RefreshAllUI();
    }

    public void BuySwingSpeed()
    {
        int c = CostFor(swingSpeedLevel);
        if (money < c) return;
        money -= c; swingSpeedLevel++; totalUpgradesPurchased++;
        RecalculateAllStats();
        Log("Golpes mas rapidos.");
        RefreshAllUI();
    }

    public void BuyHitRadius()
    {
        int c = CostFor(hitRadiusLevel);
        if (money < c) return;
        money -= c; hitRadiusLevel++; totalUpgradesPurchased++;
        RecalculateAllStats();
        Log("Radio de golpe mas grande.");
        RefreshAllUI();
    }

    public void BuyExtraCoconut()
    {
        int c = CostFor(extraCoconutLevel);
        if (money < c) return;
        money -= c; extraCoconutLevel++; totalUpgradesPurchased++;
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts += 1;
        Log("Mas cocos al iniciar el dia.");
        RefreshAllUI();
    }

    public void BuySpawnSpeed()
    {
        int c = CostFor(spawnSpeedLevel);
        if (money < c) return;
        money -= c; spawnSpeedLevel++; totalUpgradesPurchased++;
        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - 0.5f);
        }
        Log("Los cocos aparecen mas seguido.");
        RefreshAllUI();
    }
    public MacheteData GetMacheteData(int index)
    {
        if (index < 0 || index >= macheteOptions.Count) return null;
        return macheteOptions[index];
    }

    public MacheteData GetEquippedMachete() => GetMacheteData(equippedMacheteIndex);

    public bool IsMacheteUnlocked(int index) => unlockedMachetes.Contains(index);

    public void TrySelectMachete(int index)
    {
        if (index < 0 || index >= macheteOptions.Count) return;

        if (!unlockedMachetes.Contains(index))
        {
            int cost = macheteOptions[index].unlockCost;
            if (money < cost) return;
            money -= cost;
            unlockedMachetes.Add(index);
            Log("Desbloqueaste: " + macheteOptions[index].macheteName);
        }

        equippedMacheteIndex = index;
        RecalculateAllStats();
        Log("Equipado: " + macheteOptions[index].macheteName);
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
        if (skillPoints < node.cost) return false;

        if (node.prerequisiteIds != null)
        {
            foreach (var pre in node.prerequisiteIds)
            {
                if (!string.IsNullOrEmpty(pre) && !unlockedSkillIds.Contains(pre)) return false;
            }
        }
        return true;
    }

    public void UnlockSkill(string id)
    {
        SkillNode node = GetSkillNode(id);
        if (node == null || !CanUnlockSkill(node)) return;

        skillPoints -= node.cost;
        unlockedSkillIds.Add(id);
        ApplySkillEffect(node);
        Log("Habilidad desbloqueada: " + node.nodeName);
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
    public void ResetSkillTree()
    {
        int refund = 0;
        foreach (var id in unlockedSkillIds)
        {
            SkillNode node = GetSkillNode(id);
            if (node != null) refund += node.cost;
        }

        skillPoints += refund;
        unlockedSkillIds.Clear();

        skillDamageBonus = 0f;
        skillStaminaBonus = 0f;
        skillSwingIntervalReduction = 0f;
        skillHitRadiusBonus = 0f;
        skillMoneyMultiplierBonus = 0f;

        RecalculateAllStats();
        Log("Arbol de habilidades reiniciado.");
        RefreshAllUI();
    }
    void OfferPerkChoice()
    {
        currentPhase = Phase.PerkChoice;
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
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
            if (pool.Count == 0)
            {
                result[i] = null;
                continue;
            }
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
        Log("Perk elegido: " + currentPerkChoices[index].perkName);

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
        LegacyItem item = GetLegacyItem(itemName);
        if (item == null) return;
        if (purchasedLegacyItems.Contains(itemName)) return;
        if (legacyPoints < item.cost) return;

        legacyPoints -= item.cost;
        purchasedLegacyItems.Add(itemName);
        ApplyLegacyEffect(item);
        Log("Legado adquirido: " + item.itemName);
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
        if (shopMoneyText != null) shopMoneyText.text = "$" + money;
        if (shopDaysLeftText != null) shopDaysLeftText.text = daysLeft + " dias para pagar";
        if (shopBillText != null) shopBillText.text = billPaid ? "Pagada" : ("$" + billAmount);
        if (payDebtButton != null) payDebtButton.interactable = !billPaid && money >= billAmount;

        if (dmgCostText != null) dmgCostText.text = "$" + CostFor(dmgLevel);
        if (stamCostText != null) stamCostText.text = "$" + CostFor(stamLevel);
        if (efficiencyCostText != null) efficiencyCostText.text = "$" + CostFor(efficiencyLevel);
        if (extraCoconutCostText != null) extraCoconutCostText.text = "$" + CostFor(extraCoconutLevel);
        if (spawnSpeedCostText != null) spawnSpeedCostText.text = "$" + CostFor(spawnSpeedLevel);
        if (swingSpeedCostText != null) swingSpeedCostText.text = "$" + CostFor(swingSpeedLevel);
        if (hitRadiusCostText != null) hitRadiusCostText.text = "$" + CostFor(hitRadiusLevel);

        if (skillPointsText != null) skillPointsText.text = skillPoints + " Puntos de Habilidad";
        if (legacyPointsText != null) legacyPointsText.text = legacyPoints + " Puntos de Legado";
    }
    void EnsureDefaultData()
    {
        if (macheteOptions == null || macheteOptions.Count == 0)
        {
            macheteOptions = new List<MacheteData>
            {
                new MacheteData { macheteName = "Machete de Palma", description = "Equilibrado. El de siempre.", baseDamage = 3f, baseSwingInterval = 1.2f, baseHitRadius = 1.5f, staminaCostMultiplier = 1f, unlockCost = 0 },
                new MacheteData { macheteName = "Machete Pesado", description = "Mucho mas daño, pero golpea mas lento y cansa mas.", baseDamage = 6f, baseSwingInterval = 1.6f, baseHitRadius = 1.3f, staminaCostMultiplier = 1.3f, unlockCost = 250 },
                new MacheteData { macheteName = "Machete Rapido", description = "Golpea muy seguido y en area amplia, pero pega menos fuerte.", baseDamage = 2f, baseSwingInterval = 0.7f, baseHitRadius = 1.8f, staminaCostMultiplier = 0.8f, unlockCost = 250 },
            };
        }

        if (skillTree == null || skillTree.Count == 0)
        {
            skillTree = new List<SkillNode>
            {
                new SkillNode { id = "fuerza_1", nodeName = "Mas Fuerza I", description = "+2 de dano", cost = 1, prerequisiteIds = new string[0], effect = SkillEffect.DamageFlatBonus, effectValue = 2f },
                new SkillNode { id = "fuerza_2", nodeName = "Mas Fuerza II", description = "+3 de dano", cost = 2, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.DamageFlatBonus, effectValue = 3f },
                new SkillNode { id = "velocidad_1", nodeName = "Manos Rapidas", description = "Golpea mas seguido", cost = 2, prerequisiteIds = new [] { "fuerza_1" }, effect = SkillEffect.SwingIntervalReduction, effectValue = 0.1f },
                new SkillNode { id = "resistencia_1", nodeName = "Aguante I", description = "+5 de energia maxima", cost = 1, prerequisiteIds = new string[0], effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 5f },
                new SkillNode { id = "resistencia_2", nodeName = "Aguante II", description = "+8 de energia maxima", cost = 2, prerequisiteIds = new [] { "resistencia_1" }, effect = SkillEffect.StaminaMaxFlatBonus, effectValue = 8f },
                new SkillNode { id = "suerte_1", nodeName = "Buen Ojo", description = "+15% de dinero por coco", cost = 2, prerequisiteIds = new string[0], effect = SkillEffect.MoneyMultiplierBonus, effectValue = 0.15f },
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