using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static MacheteData;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    static ResourcesManager Res => ResourcesManager.Instance;
    static CombatManager Combat => CombatManager.Instance;
    static DebtManager Debt => DebtManager.Instance;
    static RunManager Run => RunManager.Instance;
    static MacheteShopManager Machetes => MacheteShopManager.Instance;
    static LegacyManager Legacy => LegacyManager.Instance;
    static PerkManager Perks => PerkManager.Instance;
    static SkillTreeManager Skills => SkillTreeManager.Instance;

    [System.Serializable]
    public class PerkIconEntry
    {
        public string perkName;
        public Sprite icon;
    }

    [Header("Machete (se ACTIVA recien al empezar a golpear, se apaga en menus/paneles)")]
    public GameObject machete;

    [Header("UI - Panel de la Maquina de Recompensas (gacha)")]
    public GameObject rewardMachinePanel;

    [Header("Desbloqueo de cocos")]
    public CoconutUnlockPanel unlockPanel;
    public bool showUnlockOnlyFirstTime = true;

    [Header("Transicion entre Mejoras/Tienda")]
    public float panelSwitchBreak = 0.12f;
    Coroutine panelSwitchRoutine;

    [Header("UI - Indicaciones (activo junto con Mejoras y Tienda)")]
    public GameObject indicacionesPanel;

    [Header("UI - Gameplay (solo stamina)")]
    public GameObject gameplayUIPanel;
    public Slider staminaSlider;

    [Header("UI - Agua actual")]
    public TMP_Text moneyText;
    public TMP_Text moneyText2;
    public TMP_Text moneyText3;

    [Header("UI - Recaudacion (hub: estadisticas + 3 botones)")]
    public GameObject recaudacionPanel;
    public TMP_Text runKillsText;
    public TMP_Text runMoneyText;
    public TMP_Text accuracyText;
    public TMP_Text dayCoinsText;
    public Button continueButton;

    [Header("UI - Recaudacion: progreso de desbloqueo del proximo tipo de coco")]
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
    [Header("Control (mando)")]
    public float gamepadPanPixelsPerSecond = 900f;
    public float gamepadZoomPerSecond = 1.2f;

    [Header("UI - Panel de Tienda (machetes)")]
    public GameObject tiendaPanel;
    public TMP_Text macheteStatusText;
    public TMP_Text macheteUpgradeCostText;
    public Button macheteUpgradeButton;

    [Header("UI - Monedas")]
    public TMP_Text coinCountText;

    [Header("UI - Panel del Pedido (antes 'Deuda')")]
    public GameObject deudaPanel;
    public TMP_Text deudaMoneyText;
    public TMP_Text deudaBillText;
    public TMP_Text deudaDaysLeftText;
    public Button payDebtButton;
    public Color urgentColor = new Color(1f, 0.25f, 0.25f);
    public Color normalDaysColor = Color.white;
    [Header("UI - Decision forzada del ultimo dia")]
    public Button bankruptcyButton;

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
    bool isChoosingPerk;

    [Header("UI - Log")]
    public TMP_Text logText;

    [Header("Gamefeel: contador de agua 'rodando' en vez de saltar de golpe")]
    public float moneyCounterSpeed = 6f;
    float displayedMoney;

    [Header("Iconos de perks (por nombre)")]
    public List<PerkIconEntry> perkIcons = new List<PerkIconEntry>();

    bool isPanningTree;
    Vector2 lastPanMousePos;
    HashSet<int> announcedUnlocks = new HashSet<int>();
    AudioContext lastAudioContext = AudioContext.None;
    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        EventBus.Subscribe<ScreenRequested>(HandleScreen);
        EventBus.Subscribe<LogMessage>(HandleLog);
        EventBus.Subscribe<BankruptcyDeclared>(HandleBankruptcy);
        EventBus.Subscribe<SkillUnlocked>(HandleSkillUnlocked);
        EventBus.Subscribe<MacheteBought>(HandleMacheteBought);
        EventBus.Subscribe<LegacyPurchased>(HandleLegacyPurchased);
        EventBus.Subscribe<GameStateChanged>(HandleGameStateChanged);
        EventBus.Subscribe<RunStarted>(HandleRunStarted);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<ScreenRequested>(HandleScreen);
        EventBus.Unsubscribe<LogMessage>(HandleLog);
        EventBus.Unsubscribe<BankruptcyDeclared>(HandleBankruptcy);
        EventBus.Unsubscribe<SkillUnlocked>(HandleSkillUnlocked);
        EventBus.Unsubscribe<MacheteBought>(HandleMacheteBought);
        EventBus.Unsubscribe<LegacyPurchased>(HandleLegacyPurchased);
        EventBus.Unsubscribe<GameStateChanged>(HandleGameStateChanged);
        EventBus.Unsubscribe<RunStarted>(HandleRunStarted);
        UnsubscribeLanguage();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        SetActive(gameplayUIPanel, false);
        SetActive(recaudacionPanel, false);
        SetActive(upgradeTreePanel, false);
        SetActive(tiendaPanel, false);
        SetActive(rewardMachinePanel, false);
        SetActive(deudaPanel, false);
        SetActive(perkChoiceUIPanel, false);
        SetActive(betweenRunsPanel, false);
        SetActive(machete, false);
        SetActive(indicacionesPanel, false);
        SetCursorVisible(true);
        UpdateMoneyUI();
        SubscribeLanguage();
        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = StaminaManager.Instance.Max;
            staminaSlider.value = StaminaManager.Instance.Current;
        }

        UpdateMoneyUI();
        HandleUpgradeTreePanZoom();
        UpdateAudioContext();
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }

    static void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
    }

    static string Loc(string key) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
    static string Loc(string key, params object[] args) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key, args) : key;

    public void Log(string msg)
    {
        if (logText != null) logText.text = msg;
    }

    void HandleLog(LogMessage e) => Log(e.Text);

    void HandleRunStarted(RunStarted e) => announcedUnlocks.Clear();

    void HandleGameStateChanged(GameStateChanged e)
    {
        if (e.Current == GameState.OnPause) SetCursorVisible(true);
        else if (e.Previous == GameState.OnPause) SetCursorVisible(Run.Phase != RunPhase.Hitting);
    }

    void HandleBankruptcy(BankruptcyDeclared e)
    {
        string msg = Loc("log_bankruptcy", e.LegacyGained);
        if (bankruptcyMessageText != null) bankruptcyMessageText.text = msg;
        Log(msg);
    }

    void HandleSkillUnlocked(SkillUnlocked e)
    {
        SkillNode node = Skills.GetNode(e.Id);
        Log(Loc("log_upgrade_bought", node != null ? node.nodeName : e.Id));
        RefreshAllUI();
    }

    void HandleMacheteBought(MacheteBought e)
    {
        if (machete != null && machete.activeInHierarchy && MacheteController.Instance != null)
            MacheteController.Instance.EquipModel(e.Index);

        MacheteData m = Machetes.Get(e.Index);
        Log(Loc("log_new_machete", m != null ? m.macheteName : ""));
        RefreshAllUI();
    }

    void HandleLegacyPurchased(LegacyPurchased e)
    {
        Log(Loc("log_legacy_bought", e.ItemName));
        RefreshAllUI();
    }
    void HandleScreen(ScreenRequested e)
    {
        switch (e.Screen)
        {
            case UIScreen.Hitting:
                SetActive(machete, false);
                SetActive(gameplayUIPanel, true);
                SetActive(recaudacionPanel, false);
                SetActive(perkChoiceUIPanel, false);
                SetActive(betweenRunsPanel, false);
                SetActive(tiendaPanel, false);
                SetActive(deudaPanel, false);
                SetActive(upgradeTreePanel, false);
                SetCursorVisible(false);
                RefreshAllUI();
                break;

            case UIScreen.HittingActive:
                SetActive(machete, true);
                break;

            case UIScreen.Recaudacion:
                SetActive(machete, false);
                SetActive(gameplayUIPanel, false);
                SetActive(perkChoiceUIPanel, false);
                SetActive(betweenRunsPanel, false);
                SetActive(upgradeTreePanel, false);
                SetActive(tiendaPanel, false);
                SetActive(rewardMachinePanel, false);
                SetActive(deudaPanel, false);
                SetActive(recaudacionPanel, true);
                SetCursorVisible(true);
                RefreshAllUI();
                AnnounceNewUnlocks();
                break;

            case UIScreen.ForcedOrder:
                SetActive(machete, false);
                SetActive(gameplayUIPanel, false);
                SetActive(recaudacionPanel, false);
                SetActive(upgradeTreePanel, false);
                SetActive(tiendaPanel, false);
                SetActive(rewardMachinePanel, false);
                SetActive(deudaPanel, true);
                SetCursorVisible(true);
                RefreshAllUI();
                AnnounceNewUnlocks();
                break;

            case UIScreen.PerkChoice:
                SetActive(machete, false);
                SetActive(recaudacionPanel, false);
                SetActive(deudaPanel, false);
                SetActive(upgradeTreePanel, false);
                SetActive(tiendaPanel, false);
                SetActive(rewardMachinePanel, false);
                SetActive(perkChoiceUIPanel, true);
                SetCursorVisible(true);
                ShowPerkChoices();
                break;

            case UIScreen.BetweenRuns:
                SetActive(machete, false);
                SetActive(gameplayUIPanel, false);
                SetActive(recaudacionPanel, false);
                SetActive(deudaPanel, false);
                SetActive(perkChoiceUIPanel, false);
                SetActive(betweenRunsPanel, true);
                SetCursorVisible(true);
                RefreshAllUI();
                break;

            case UIScreen.Event:
                SetActive(machete, false);
                SetActive(gameplayUIPanel, false);
                SetActive(recaudacionPanel, false);
                SetActive(upgradeTreePanel, false);
                SetActive(tiendaPanel, false);
                SetActive(rewardMachinePanel, false);
                SetActive(deudaPanel, false);
                SetCursorVisible(true);
                break;

            case UIScreen.None:
            default:
                SetActive(machete, false);
                SetActive(gameplayUIPanel, false);
                SetActive(recaudacionPanel, false);
                SetActive(upgradeTreePanel, false);
                SetActive(tiendaPanel, false);
                SetActive(rewardMachinePanel, false);
                SetActive(deudaPanel, false);
                SetActive(perkChoiceUIPanel, false);
                SetActive(betweenRunsPanel, false);
                SetActive(indicacionesPanel, false);
                SetCursorVisible(true);
                break;
        }
    }

    public void ToggleUpgradeTreePanel()
    {
        if (Run.ForcedDecision) return;
        bool willOpen = upgradeTreePanel != null && !upgradeTreePanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(upgradeTreePanel, new GameObject[] { tiendaPanel, rewardMachinePanel }, willOpen));
    }

    public void ToggleTiendaPanel()
    {
        if (Run.ForcedDecision) return;
        bool willOpen = tiendaPanel != null && !tiendaPanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(tiendaPanel, new GameObject[] { upgradeTreePanel, rewardMachinePanel }, willOpen));
    }

    public void ToggleRewardMachinePanel()
    {
        if (Run.ForcedDecision) return;
        bool willOpen = rewardMachinePanel != null && !rewardMachinePanel.activeSelf;
        if (panelSwitchRoutine != null) StopCoroutine(panelSwitchRoutine);
        panelSwitchRoutine = StartCoroutine(SwitchPanels(rewardMachinePanel, new GameObject[] { upgradeTreePanel, tiendaPanel }, willOpen));
    }

    public void ToggleDeudaPanel()
    {
        if (Run.ForcedDecision) return;
        if (deudaPanel != null) deudaPanel.SetActive(!deudaPanel.activeSelf);
        RefreshAllUI();
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
            yield return new WaitForSecondsRealtime(panelSwitchBreak);

        if (willOpen && panelToToggle != null)
        {
            panelToToggle.SetActive(true);
            if (panelToToggle == upgradeTreePanel) RecenterUpgradeTree();
        }

        RefreshAllUI();
    }

    void ShowPerkChoices()
    {
        isChoosingPerk = false;
        PerkOption[] choices = Perks.CurrentChoices;

        SetPerkLabel(perkOption1Name, perkOption1Desc, perkOption1Icon, choices[0]);
        SetPerkLabel(perkOption2Name, perkOption2Desc, perkOption2Icon, choices[1]);
        SetPerkLabel(perkOption3Name, perkOption3Desc, perkOption3Icon, choices[2]);
        if (perkCard1 != null) perkCard1.gameObject.SetActive(choices[0] != null);
        if (perkCard2 != null) perkCard2.gameObject.SetActive(choices[1] != null);
        if (perkCard3 != null) perkCard3.gameObject.SetActive(choices[2] != null);
        if (perkCard1 != null) perkCard1.PlayAppear(0f);
        if (perkCard2 != null) perkCard2.PlayAppear(perkCardStagger);
        if (perkCard3 != null) perkCard3.PlayAppear(perkCardStagger * 2f);
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

    public void ChoosePerk(int index)
    {
        if (isChoosingPerk) return;
        PerkOption[] choices = Perks.CurrentChoices;
        if (choices == null || index < 0 || index >= choices.Length || choices[index] == null) return;
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

        Run.CompletePerkChoice(index);
        isChoosingPerk = false;
    }

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

    static string Signed(float v) => (v >= 0f ? "+" : "") + v.ToString("0.##");

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

        return "\n" + Loc("stat_damage") + " " + Signed(dDmg)
             + "  |  " + Loc("stat_swing") + " " + Signed(dSwing) + " s"
             + "  |  " + Loc("stat_radius") + " " + Signed(dRadius);
    }

    void UpdateMoneyUI()
    {
        float water = Res.Water;
        if (!Mathf.Approximately(displayedMoney, water))
        {
            float diff = water - displayedMoney;
            float step = diff * moneyCounterSpeed * Time.unscaledDeltaTime;
            if (Mathf.Abs(step) < 1f) step = Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), 1f);
            displayedMoney += step;

            bool overshot = (diff > 0f && displayedMoney > water) || (diff < 0f && displayedMoney < water);
            if (overshot) displayedMoney = water;
        }

        string txt = Loc("collection_current_money", ResourcesManager.FormatWater(displayedMoney));
        if (moneyText != null) moneyText.text = txt;
        if (moneyText2 != null) moneyText2.text = txt;
        if (moneyText3 != null) moneyText3.text = txt;
        if (coinCountText != null) coinCountText.text = Loc("collection_coins", Res.Coins);
    }

    public void RefreshAllUI()
    {
        if (indicacionesPanel != null)
        {
            bool showIndicaciones = (upgradeTreePanel != null && upgradeTreePanel.activeSelf)
                      || (tiendaPanel != null && tiendaPanel.activeSelf)
                      || (rewardMachinePanel != null && rewardMachinePanel.activeSelf);
            indicacionesPanel.SetActive(showIndicaciones);
        }
        UpdateMoneyUI();

        CombatManager combat = Combat;
        if (runKillsText != null) runKillsText.text = Loc("collection_coconuts_killed", combat.dayCoconutsKilled);
        if (runMoneyText != null) runMoneyText.text = Loc("collection_money_earned", ResourcesManager.FormatWater(combat.dayMoneyEarned));

        if (accuracyText != null)
        {
            float accuracyPercent = combat.daySwings > 0 ? (combat.dayHits / (float)combat.daySwings) * 100f : 0f;
            accuracyText.text = Loc("run_accuracy", Mathf.RoundToInt(accuracyPercent));
        }

        if (dayCoinsText != null)
        {
            bool gotCoinsToday = combat.dayCoinsFound > 0;
            dayCoinsText.gameObject.SetActive(gotCoinsToday);
            if (gotCoinsToday)
                dayCoinsText.text = Loc(combat.dayCoinsFound == 1 ? "coins_found_one" : "coins_found_many", combat.dayCoinsFound);
        }

        CoconutTypeSO nextUnlock = GetNextLockedCoconut();
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
            if (showText) nextUnlockPercentText.text = Loc("unlock_order", nextUnlock.unlockCycle);
        }

        float water = Res.Water;
        float target = Debt.TargetWater;
        bool forced = Run.ForcedDecision;

        if (deudaMoneyText != null) deudaMoneyText.text = Loc("collection_current_money", ResourcesManager.FormatWater(water));

        bool readyToDeliver = water >= target;

        if (deudaBillText != null)
        {
            if (readyToDeliver) deudaBillText.text = Loc("deuda_ready");
            else deudaBillText.text = Loc("deuda_remaining", ResourcesManager.FormatWater(Mathf.Max(0f, target - water)));
        }

        if (deudaDaysLeftText != null)
        {
            int daysToPlay = Run.DaysToPlay;
            if (forced)
            {
                deudaDaysLeftText.text = Loc(readyToDeliver ? "deuda_last_day_pay" : "deuda_last_day_short");
                deudaDaysLeftText.color = urgentColor;
            }
            else if (!readyToDeliver && daysToPlay <= 1)
            {
                deudaDaysLeftText.text = Loc("deuda_last_day_collect");
                deudaDaysLeftText.color = urgentColor;
            }
            else
            {
                deudaDaysLeftText.text = Loc(daysToPlay == 1 ? "days_left_one" : "days_left_many", daysToPlay);
                deudaDaysLeftText.color = normalDaysColor;
            }
        }

        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(forced);
        if (payDebtButton != null) payDebtButton.interactable = readyToDeliver;

        MacheteData current = Machetes.Current;
        MacheteData next = Machetes.Next;
        if (macheteStatusText != null)
            macheteStatusText.text = Loc("shop_current_machete", current != null ? current.macheteName : "-") + PassiveLine(current);
        if (next != null)
        {
            if (macheteUpgradeCostText != null)
                macheteUpgradeCostText.text = Loc("shop_next_machete", next.macheteName, ResourcesManager.FormatWater(next.unlockCost))
                            + StatsGainLine(current, next) + PassiveLine(next);
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = water >= next.unlockCost;
        }
        else
        {
            if (macheteUpgradeCostText != null) macheteUpgradeCostText.text = Loc("shop_max_machete");
            if (macheteUpgradeButton != null) macheteUpgradeButton.interactable = false;
        }
        if (coinCountText != null) coinCountText.text = Loc("collection_coins", Res.Coins);

        if (legacyPointsText != null) legacyPointsText.text = Loc("betweenruns_legacy_points", Legacy.Points);
    }

    void SubscribeLanguage()
    {
        var loc = LocalizationManager.Instance;
        if (loc != null) loc.OnLanguageChanged += RefreshAllUI;
    }

    void UnsubscribeLanguage()
    {
        var loc = LocalizationManager.Instance;
        if (loc != null) loc.OnLanguageChanged -= RefreshAllUI;
    }

    static List<CoconutTypeSO> CoconutTypes =>
        CoconutSpawner.Instance != null ? CoconutSpawner.Instance.coconutTypes : null;

    public CoconutTypeSO GetNextLockedCoconut()
    {
        List<CoconutTypeSO> types = CoconutTypes;
        if (types == null) return null;
        int cycle = Debt.BillCycle;
        CoconutTypeSO next = null;
        foreach (var t in types)
        {
            if (t == null || cycle >= t.unlockCycle) continue;
            if (next == null || t.unlockCycle < next.unlockCycle) next = t;
        }
        return next;
    }

    void AnnounceNewUnlocks()
    {
        List<CoconutTypeSO> types = CoconutTypes;
        if (unlockPanel == null || types == null) return;
        int cycle = Debt.BillCycle;
        for (int i = 1; i < types.Count; i++)
        {
            CoconutTypeSO t = types[i];
            if (t == null || cycle < t.unlockCycle) continue;
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
        CoconutTypeSO next = GetNextLockedCoconut();
        List<CoconutTypeSO> types = CoconutTypes;
        if (next == null || types == null) return 1f;
        int prev = 1;
        foreach (var t in types)
            if (t != null && t.unlockCycle < next.unlockCycle && t.unlockCycle > prev) prev = t.unlockCycle;
        int span = next.unlockCycle - prev;
        if (span <= 0) return 1f;
        return Mathf.Clamp01((float)(Debt.BillCycle - prev) / span);
    }

    [ContextMenu("Borrar cocos vistos (para probar el panel)")]
    void ClearSeenCoconuts()
    {
        for (int i = 0; i < 15; i++) PlayerPrefs.DeleteKey("coco_visto_" + i);
        PlayerPrefs.Save();
    }

    AudioContext ResolveAudioContext()
    {
        if (GameManager.IsPaused) return AudioContext.Pause;
        if (Run.Phase == RunPhase.MainMenu) return AudioContext.MainMenu;
        if (deudaPanel != null && deudaPanel.activeSelf) return AudioContext.Deuda;
        if (upgradeTreePanel != null && upgradeTreePanel.activeSelf) return AudioContext.UpgradeTree;
        if (tiendaPanel != null && tiendaPanel.activeSelf) return AudioContext.Tienda;
        if (rewardMachinePanel != null && rewardMachinePanel.activeSelf) return AudioContext.RewardMachine;
        if (perkChoiceUIPanel != null && perkChoiceUIPanel.activeSelf) return AudioContext.PerkChoice;
        if (betweenRunsPanel != null && betweenRunsPanel.activeSelf) return AudioContext.BetweenRuns;
        if (recaudacionPanel != null && recaudacionPanel.activeSelf) return AudioContext.Recaudacion;
        if (Run.Phase == RunPhase.Hitting) return AudioContext.Hitting;

        return AudioContext.None;
    }

    void UpdateAudioContext()
    {
        if (AudioManager.Instance == null) return;

        AudioContext ctx = ResolveAudioContext();
        if (ctx == AudioContext.None || ctx == lastAudioContext) return;

        lastAudioContext = ctx;
        AudioManager.Instance.SetContext(ctx);
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
        Vector2 pad = GameInput.PanStick;
        if (pad.sqrMagnitude > 0f)
        {
            Vector2 p = upgradeTreeContent.anchoredPosition - pad * gamepadPanPixelsPerSecond * Time.unscaledDeltaTime;
            ClampContentPosition(ref p);
            upgradeTreeContent.anchoredPosition = p;
        }
        float zoomDelta = 0f;
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f) zoomDelta += scroll * zoomSpeed;
        zoomDelta += GameInput.ZoomAxis * gamepadZoomPerSecond * Time.unscaledDeltaTime;

        if (Mathf.Abs(zoomDelta) > 0.0001f)
        {
            float newScale = Mathf.Clamp(upgradeTreeContent.localScale.x + zoomDelta, minZoom, maxZoom);
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
}