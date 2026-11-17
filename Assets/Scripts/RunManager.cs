using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SS = StatSource;
using ST = StatType;
using static MacheteData;

public enum RunPhase { MainMenu, Hitting, Shop, PerkChoice, BetweenRuns }

public class RunManager : MonoBehaviour
{
    static RunManager instance;
    public static RunManager Instance
    {
        get
        {
            if (instance == null) instance = ManagerBootstrap.Ensure<RunManager>();
            return instance;
        }
    }

    static ResourcesManager Res => ResourcesManager.Instance;
    static StatsManager Stats => StatsManager.Instance;
    static StaminaManager Stamina => StaminaManager.Instance;
    static CombatManager Combat => CombatManager.Instance;
    static MacheteShopManager Machetes => MacheteShopManager.Instance;
    static SkillTreeManager Skills => SkillTreeManager.Instance;
    static PerkManager Perks => PerkManager.Instance;
    static LegacyManager Legacy => LegacyManager.Instance;
    static DebtManager Debt => DebtManager.Instance;

    public RunPhase Phase { get; private set; } = RunPhase.MainMenu;
    public bool ForcedDecision { get; private set; }
    public bool SkipNextDayDecrement { get; set; }
    public int RunDayCount { get; set; }

    int recordAtRunStart = 1;
    RunPhase phaseBeforeMenu = RunPhase.MainMenu;

    public bool IsHitting => Phase == RunPhase.Hitting;
    public int DaysToPlay => Mathf.Max(0, SkipNextDayDecrement ? Debt.DaysLeft : Debt.DaysLeft - 1);
    public bool IsOrderUrgent => Res.Water < Debt.TargetWater && DaysToPlay <= 1;

    static string Loc(string key, params object[] args) =>
        LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key, args) : key;

    static int LegacyStartingMachete => Mathf.RoundToInt(Stats.GetSource(SS.Legacy, ST.StartingMachete));
    static int LegacyStartingCoconuts => Mathf.RoundToInt(Stats.GetSource(SS.Legacy, ST.StartingCoconuts));

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
    }

    void OnEnable()
    {
        EventBus.Subscribe<SkillUnlocked>(OnSkillUnlocked);
        EventBus.Subscribe<MacheteBought>(OnMacheteBought);
        EventBus.Subscribe<LegacyPurchased>(OnLegacyPurchased);
        EventBus.Subscribe<DebtPaid>(OnDebtPaid);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<SkillUnlocked>(OnSkillUnlocked);
        EventBus.Unsubscribe<MacheteBought>(OnMacheteBought);
        EventBus.Unsubscribe<LegacyPurchased>(OnLegacyPurchased);
        EventBus.Unsubscribe<DebtPaid>(OnDebtPaid);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        LoadGame();
        Machetes.RecalculateStats();
    }

    void Update()
    {
        if (!GameManager.IsOnGame || Phase != RunPhase.Hitting) return;

        StaminaManager st = Stamina;
        if (st.DayElapsed >= st.startDelay)
        {
            st.Current -= Time.deltaTime;
            if (st.Current <= 0f)
            {
                st.Current = 0f;
                EnterRecaudacionPhase();
            }
        }

        bool shouldBeActive = Stats.Get(ST.LastBreathSwing) > 0f && st.Current <= st.Max * 0.2f;
        if (shouldBeActive != Stats.lastBreathSwingActive)
        {
            Stats.lastBreathSwingActive = shouldBeActive;
            Machetes.RecalculateStats();
        }
        st.DayElapsed += Time.deltaTime;
    }

    void OnApplicationQuit() { SaveGame(); }
    void OnApplicationPause(bool paused) { if (paused) SaveGame(); }

    void OnSkillUnlocked(SkillUnlocked e) { BetaLogger.Log("skill_" + e.Id); AchievementTracker.Check(); }
    void OnMacheteBought(MacheteBought e) { BetaLogger.Log("machete_" + e.Index); AchievementTracker.Check(); }
    void OnLegacyPurchased(LegacyPurchased e) { SaveGame(); AchievementTracker.Check(); }
    void OnDebtPaid(DebtPaid e) { AchievementTracker.Check(); }

    public void BeginNewGame()
    {
        GameManager.Request(GameState.OnGame);
        recordAtRunStart = Debt.BestCycleReached;
        Stamina.Current = Stats.StaminaMax;
        Debt.DaysLeft = Debt.DaysForCycle(Debt.BillCycle);
        Res.SetWater(Mathf.Max(Res.Water, Stats.GetSource(SS.Legacy, ST.StartingWater)));
        Machetes.EquippedIndex = Mathf.Max(Machetes.EquippedIndex, LegacyStartingMachete);
        Combat.runCoconutsKilled = 0;
        Combat.runMoneyEarned = 0;
        Machetes.RecalculateStats();
        Stamina.Current = Stats.StaminaMax;
        StartDay();
        BetaLogger.StartRun();
        BetaLogger.Log("run_start");
        EventBus.Publish(new RunStarted());
    }

    void StartDay()
    {
        RunDayCount++;
        Combat.ResetDay();
        Combat.StreakForgivesLeft = Mathf.RoundToInt(Stats.GetSource(SS.Skill, ST.StreakForgives));
        Stamina.ResetDay();
        Stats.lastBreathSwingActive = false;

        EventBus.Publish(new ScreenRequested(UIScreen.Hitting));

        CoconutSpawner sp = CoconutSpawner.Instance;
        if (sp != null)
        {
            sp.OnInitialSpawnComplete -= HandleInitialSpawnComplete;
            sp.OnInitialSpawnComplete += HandleInitialSpawnComplete;
            sp.StartNewDay();
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

    void ActivateHittingPhase()
    {
        Phase = RunPhase.Hitting;
        EventBus.Publish(new ScreenRequested(UIScreen.HittingActive));
        Machetes.RecalculateStats();
        EventBus.Publish(new DayStarted(RunDayCount));
    }

    void EnterRecaudacionPhase()
    {
        Phase = RunPhase.Shop;
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.ClearAllCoconuts();
        BetaLogger.Log("day_end");

        bool isDeliveryDay = Debt.DaysLeft <= 1 && !SkipNextDayDecrement;
        if (isDeliveryDay)
        {
            OpenForcedOrderPanel();
        }
        else
        {
            EventBus.Publish(new ScreenRequested(UIScreen.Recaudacion));
            EventBus.Publish(new RecaudacionShown());
        }
        SaveGame();
    }

    public void ContinueToNextDay()
    {
        if (SkipNextDayDecrement)
        {
            SkipNextDayDecrement = false;
        }
        else
        {
            if (Debt.DaysLeft <= 1)
            {
                OpenForcedOrderPanel();
                return;
            }
            Debt.DaysLeft--;
        }

        if (EventManager.Instance != null && EventManager.Instance.ShouldTrigger(RunDayCount))
        {
            BeginEventPhase();
            EventManager.Instance.StartEvent(StartDayAfterEvent);
            return;
        }
        StartDayAfterEvent();
    }

    void BeginEventPhase()
    {
        GameManager.Request(GameState.OnMinigame);
        Phase = RunPhase.Shop;
        EventBus.Publish(new ScreenRequested(UIScreen.Event));
        GameLog.Say("¡Evento especial!");
    }

    void StartDayAfterEvent()
    {
        GameManager.Request(GameState.OnGame);
        Stamina.Current = Stats.StaminaMax;
        StartDay();
    }

    public void PayDebt()
    {
        if (Phase != RunPhase.Shop) return;
        if (!Debt.TryPay(out float delivered, out float refund, out int deliveredCycle)) return;

        ForcedDecision = false;
        Machetes.RecalculateStats();
        GameLog.Say("Pedido entregado. Nuevo pedido: " + ResourcesManager.FormatWater(Debt.TargetWater)
            + (refund > 0f ? " (reembolso +" + ResourcesManager.FormatWater(refund) + ")" : ""));
        BetaLogger.Log("order_paid");
        CheckVictory(deliveredCycle);
        QueuePerkOffer();
    }

    void OpenForcedOrderPanel()
    {
        ForcedDecision = true;
        EventBus.Publish(new ScreenRequested(UIScreen.ForcedOrder));
        GameLog.Say("Se acabó el tiempo: paga el pedido o declara bancarrota.");
    }

    public void DeclareBankruptcy()
    {
        if (!ForcedDecision) return;
        ForcedDecision = false;
        ResolveBankruptcy();
    }

    void ResolveBankruptcy()
    {
        BetaLogger.Log("bankruptcy");
        Stats.ClearCurse();
        Perks.PendingOffers = 0;

        int cycle = Debt.BillCycle;
        int gained = Mathf.CeilToInt(cycle / 2f);
        if (cycle <= recordAtRunStart)
            gained = Legacy.TotalEarned < 5 ? 1 : 0;
        Legacy.Points += gained;
        Legacy.TotalEarned += gained;
        Legacy.RecalcMultiplier();

        EventBus.Publish(new BankruptcyDeclared(gained));

        ResetRunState();
        EnterBetweenRunsPhase();
        SaveGame();
        EventBus.Publish(new RunReset());
    }

    void EnterBetweenRunsPhase()
    {
        GameManager.Request(GameState.OnGameOver);
        Phase = RunPhase.BetweenRuns;
        EventBus.Publish(new ScreenRequested(UIScreen.BetweenRuns));
    }

    public void ContinueAfterBankruptcy()
    {
        BeginNewGame();
    }

    void CheckVictory(int deliveredCycle)
    {
        if (!Debt.TryAwardVictory(deliveredCycle)) return;
        Legacy.Points += Debt.victoryLegacyBonus;
        Legacy.TotalEarned += Debt.victoryLegacyBonus;
        Legacy.RecalcMultiplier();
        GameLog.Say("¡COMPLETASTE EL CICLO " + Debt.victoryCycle + "! +" + Debt.victoryLegacyBonus + " puntos de legado. Sigue en modo infinito.");
        BetaLogger.Log("VICTORY");
        SaveGame();
    }
    void QueuePerkOffer()
    {
        Perks.PendingOffers++;
        if (Phase != RunPhase.PerkChoice) OfferPerkChoice();
    }

    void OfferPerkChoice()
    {
        Phase = RunPhase.PerkChoice;
        Perks.CurrentChoices = Perks.PickRandom(3, Debt.BillCycle);
        if (Perks.CurrentChoices[0] == null)
        {
            Perks.PendingOffers = 0;
            ShowRecaudacionAfterPerk();
            return;
        }
        EventBus.Publish(new ScreenRequested(UIScreen.PerkChoice));
    }
    public void CompletePerkChoice(int index)
    {
        PerkOption[] choices = Perks.CurrentChoices;
        if (choices == null || index < 0 || index >= choices.Length || choices[index] == null) return;

        PerkOption perk = choices[index];
        Perks.RegisterChosen(perk.perkName);
        Perks.Apply(perk);
        BetaLogger.Log("perk_" + perk.perkName);
        GameLog.Say(Loc("log_perk_chosen", perk.perkName));

        if (Perks.PendingOffers > 0) Perks.PendingOffers--;

        if (Perks.PendingOffers > 0)
        {
            OfferPerkChoice();
            return;
        }
        ShowRecaudacionAfterPerk();
    }

    void ShowRecaudacionAfterPerk()
    {
        SkipNextDayDecrement = true;
        Phase = RunPhase.Shop;
        EventBus.Publish(new ScreenRequested(UIScreen.Recaudacion));
        EventBus.Publish(new RecaudacionShown());
        SaveGame();
        AchievementTracker.Check();
    }

    public bool HasRunToResume()
    {
        return phaseBeforeMenu == RunPhase.Hitting
            || phaseBeforeMenu == RunPhase.Shop
            || phaseBeforeMenu == RunPhase.PerkChoice
            || phaseBeforeMenu == RunPhase.BetweenRuns;
    }

    public void PauseToMainMenu()
    {
        phaseBeforeMenu = Phase;
        GameManager.Force(GameState.MainMenu);

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.OnInitialSpawnComplete -= HandleInitialSpawnComplete;
            CoconutSpawner.Instance.ClearAllCoconuts();
        }

        EventBus.Publish(new ScreenRequested(UIScreen.None));
        Phase = RunPhase.MainMenu;
        SaveGame();
        AppFlowManager flow = FindFirstObjectByType<AppFlowManager>();
        if (flow != null) flow.ShowMainMenu();
    }

    public void ResumeRun()
    {
        RunPhase target = phaseBeforeMenu;
        phaseBeforeMenu = RunPhase.MainMenu;

        GameManager.Request(target == RunPhase.BetweenRuns ? GameState.OnGameOver : GameState.OnGame);
        EventBus.Publish(new ScreenRequested(UIScreen.None));

        switch (target)
        {
            case RunPhase.Hitting:
                RunDayCount = Mathf.Max(0, RunDayCount - 1);
                Stamina.Current = Stats.StaminaMax;
                StartDay();
                break;

            case RunPhase.PerkChoice:
                OfferPerkChoice();
                break;

            case RunPhase.BetweenRuns:
                EnterBetweenRunsPhase();
                break;

            case RunPhase.Shop:
            default:
                Phase = RunPhase.Shop;
                if (ForcedDecision)
                {
                    OpenForcedOrderPanel();
                }
                else
                {
                    EventBus.Publish(new ScreenRequested(UIScreen.Recaudacion));
                    EventBus.Publish(new RecaudacionShown());
                }
                break;
        }
    }

    public void StartFreshRun()
    {
        phaseBeforeMenu = RunPhase.MainMenu;
        EventBus.Publish(new ScreenRequested(UIScreen.None));
        ResetRunState();
        BeginNewGame();
    }

    public void RestartRun()
    {
        phaseBeforeMenu = RunPhase.MainMenu;
        BetaLogger.Log("run_restart");
        Phase = RunPhase.MainMenu;
        Perks.PendingOffers = 0;
        GameManager.Force(GameState.MainMenu);
        SkipNextDayDecrement = false;
        Stats.ClearCurse();

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.OnInitialSpawnComplete -= HandleInitialSpawnComplete;
            CoconutSpawner.Instance.ClearAllCoconuts();
        }

        EventBus.Publish(new ScreenRequested(UIScreen.None));
        ResetRunState();
        SaveGame();
        EventBus.Publish(new RunReset());
        ForcedDecision = false;
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        GameManager.Force(GameState.MainMenu);
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
    void ResetRunState()
    {
        Debt.ResetRun();
        Res.SetWater(Stats.GetSource(SS.Legacy, ST.StartingWater));
        Combat.ResetRun();

        Skills.ResetRun();
        Perks.ResetRun();
        Stats.ResetRunSources();

        RunDayCount = 0;
        Machetes.EquippedIndex = LegacyStartingMachete;
        Stats.coinDropChance = 0.005f + Stats.GetSource(SS.Legacy, ST.CoinChance);

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.startingCoconuts = 4 + LegacyStartingCoconuts;
            CoconutSpawner.Instance.spawnInterval = CoconutSpawner.BaseSpawnInterval;
        }
        ForcedDecision = false;
        Machetes.RecalculateStats();
    }

    void SaveGame()
    {
        SaveData d = new SaveData
        {
            legacyPoints = Legacy.Points,
            totalLegacyPointsEarned = Legacy.TotalEarned,
            coconutCoins = Res.Coins,
            bestRunKills = Combat.bestRunKills,
            bestCycleReached = Debt.BestCycleReached,
            purchasedLegacyItems = new List<string>(Legacy.Purchased)
        };
        SaveSystem.Save(d);
    }

    void LoadGame()
    {
        SaveData d = SaveSystem.Load();
        if (d == null) return;

        Legacy.Points = d.legacyPoints;
        Legacy.TotalEarned = d.totalLegacyPointsEarned;
        Res.SetCoins(d.coconutCoins);
        Combat.bestRunKills = d.bestRunKills;
        Debt.BestCycleReached = Mathf.Max(1, d.bestCycleReached);

        Stats.ClearSource(SS.Legacy);
        Machetes.EquippedIndex = 0;
        Stats.coinDropChance = 0.005f;
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.startingCoconuts = 4;

        Legacy.Purchased.Clear();
        if (d.purchasedLegacyItems != null)
        {
            foreach (string name in d.purchasedLegacyItems)
            {
                LegacyItem item = Legacy.GetItem(name);
                if (item == null) continue;
                Legacy.Purchased.Add(name);
                Legacy.ApplyEffect(item);
            }
        }
        Legacy.RecalcMultiplier();
        Machetes.RecalculateStats();
    }

    [ContextMenu("Borrar guardado")]
    void DeleteSave() { SaveSystem.Delete(); }
}