using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum Phase { Hitting, Shop }

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
    public int damage = 3;
    public int coconutHpMaxBase = 10;

    [Header("Progreso (para desbloquear tipos de coco)")]
    public int totalCoconutsKilled = 0;

    [Header("Energia (stamina)")]
    public float stamina = 20f;
    public float staminaMax = 20f;
    public float swingStaminaCost = 1f;

    [Header("Niveles de mejora - combate")]
    public int dmgLevel = 1;
    public int stamLevel = 1;
    public int efficiencyLevel = 1;

    [Header("Niveles de mejora - cocos")]
    public int extraCoconutLevel = 1;
    public int spawnSpeedLevel = 1;

    [Header("Legado")]
    public int legacyPoints = 0;
    public float legacyMultiplier = 1f;

    [Header("UI - Gameplay (solo stamina)")]
    public GameObject gameplayUIPanel;
    public Slider staminaSlider;

    [Header("UI - Tienda / Fin de energia")]
    public GameObject shopUIPanel;
    public TMP_Text shopMoneyText;
    public TMP_Text shopDaysLeftText;
    public TMP_Text shopBillText;
    public Button payDebtButton;
    public Button mejorasButton;
    public Button continueButton;

    [Header("UI - Panel de Mejoras")]
    public GameObject upgradesPanel;
    public TMP_Text dmgCostText;
    public TMP_Text stamCostText;
    public TMP_Text efficiencyCostText;
    public TMP_Text extraCoconutCostText;
    public TMP_Text spawnSpeedCostText;

    [Header("UI - Log / Legado")]
    public TMP_Text logText;
    public TMP_Text legacyText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        daysLeft = dayLimitBase;
        ShowHittingUI();
        RefreshAllUI();
    }

    void Update()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = staminaMax;
            staminaSlider.value = stamina;
        }
    }

    public bool IsHittingPhase() => currentPhase == Phase.Hitting;

    public int GetCoconutHpMax()
    {
        return Mathf.RoundToInt(coconutHpMaxBase + billCycle * 3);
    }
    public void UseStaminaForSwing()
    {
        if (currentPhase != Phase.Hitting) return;

        stamina -= swingStaminaCost;
        if (stamina <= 0f)
        {
            stamina = 0f;
            EnterShopPhase();
        }
    }
    public void OnCoconutDestroyed(float lootMultiplier = 1f)
    {
        totalCoconutsKilled++;
        int earned = Mathf.RoundToInt((4 * legacyMultiplier + Random.Range(0, 4)) * lootMultiplier);
        money += earned;
        Log("+$" + earned);
    }

    void EnterShopPhase()
    {
        currentPhase = Phase.Shop;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (shopUIPanel != null) shopUIPanel.SetActive(true);
        if (upgradesPanel != null) upgradesPanel.SetActive(false);
        RefreshAllUI();
    }

    void ShowHittingUI()
    {
        currentPhase = Phase.Hitting;
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(true);
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
        if (upgradesPanel != null) upgradesPanel.SetActive(false);

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.StartNewDay();
        }
    }

    public void ToggleUpgradesPanel()
    {
        if (upgradesPanel != null) upgradesPanel.SetActive(!upgradesPanel.activeSelf);
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
            ResolveBillDeadline();
        }

        stamina = staminaMax;
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
            Log("Nuevo ciclo. Cuenta: $" + billAmount);
        }
        else
        {
            int gained = Mathf.Max(1, Mathf.RoundToInt(billCycle / 2f));
            legacyPoints += gained;
            legacyMultiplier = 1f + legacyPoints * 0.1f;

            money = 0;
            billCycle = 1;
            billAmount = 40;
            daysLeft = dayLimitBase;
            billPaid = false;
            damage = 3;
            staminaMax = 20;
            swingStaminaCost = 1f;
            dmgLevel = 1;
            stamLevel = 1;
            efficiencyLevel = 1;
            extraCoconutLevel = 1;
            spawnSpeedLevel = 1;

            if (CoconutSpawner.Instance != null)
            {
                CoconutSpawner.Instance.startingCoconuts = 4;
                CoconutSpawner.Instance.spawnInterval = 5f;
            }

            Log("Bancarrota. +" + gained + " puntos de legado.");
        }
    }

    int CostFor(int level)
    {
        return Mathf.RoundToInt(10 * Mathf.Pow(1.5f, level - 1));
    }

    public void BuyDamage()
    {
        int c = CostFor(dmgLevel);
        if (money < c) return;
        money -= c;
        dmgLevel++;
        damage += 2;
        Log("Machete mejorado. Dano: " + damage);
        RefreshAllUI();
    }

    public void BuyStaminaMax()
    {
        int c = CostFor(stamLevel);
        if (money < c) return;
        money -= c;
        stamLevel++;
        staminaMax += 5;
        Log("Energia maxima: " + staminaMax);
        RefreshAllUI();
    }

    public void BuyEfficiency()
    {
        int c = CostFor(efficiencyLevel);
        if (money < c) return;
        money -= c;
        efficiencyLevel++;
        swingStaminaCost = Mathf.Max(0.3f, swingStaminaCost - 0.1f);
        Log("Machete mas eficiente.");
        RefreshAllUI();
    }
    public void BuyExtraCoconut()
    {
        int c = CostFor(extraCoconutLevel);
        if (money < c) return;
        money -= c;
        extraCoconutLevel++;
        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.startingCoconuts += 1;
        }
        Log("Mas cocos al iniciar el dia.");
        RefreshAllUI();
    }
    public void BuySpawnSpeed()
    {
        int c = CostFor(spawnSpeedLevel);
        if (money < c) return;
        money -= c;
        spawnSpeedLevel++;
        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.spawnInterval = Mathf.Max(0.5f, CoconutSpawner.Instance.spawnInterval - 0.5f);
        }
        Log("Los cocos aparecen mas seguido.");
        RefreshAllUI();
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

        if (legacyText != null) legacyText.text = legacyPoints.ToString();
    }
}