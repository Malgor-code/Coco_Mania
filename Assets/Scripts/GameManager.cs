using System.Collections.Generic;
using UnityEngine;
using static MacheteData;
using SS = StatSource;
using ST = StatType;

public enum GameState
{
    MainMenu,
    OnGame,
    OnPause,
    OnGameOver,  
    OnMinigame
}

[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] bool logTransitions = false;

    public GameState Current { get; private set; } = GameState.MainMenu;
    public GameState Previous { get; private set; } = GameState.MainMenu;
    GameState stateBeforePause = GameState.MainMenu;

    public static bool IsPaused => Instance != null && Instance.Current == GameState.OnPause;
    public static bool IsOnGame => Instance != null && Instance.Current == GameState.OnGame;
    public bool CanPause => Current != GameState.MainMenu && Current != GameState.OnPause;
    bool silentPause;
    public static bool IsSilentPause => Instance != null && Instance.silentPause;
    public void PauseSilent() { if (!CanPause) return; silentPause = true; Pause(); }
    public void ResumeSilent() { if (!silentPause) return; Resume(); }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Time.timeScale = 1f; 
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    public static void Request(GameState next) { if (Instance != null) Instance.SetState(next); }
    public static void Force(GameState next) { if (Instance != null) Instance.ForceState(next); }
    public bool SetState(GameState next)
    {
        if (next == GameState.OnPause) { Pause(); return true; }
        if (Current == GameState.OnPause && next != GameState.MainMenu)
        {
            if (next == stateBeforePause) return false;
            if (!IsValidTransition(stateBeforePause, next)) { WarnInvalid(stateBeforePause, next); return false; }
            stateBeforePause = next;
            return true;
        }

        if (next == Current) return false;
        if (!IsValidTransition(Current, next)) { WarnInvalid(Current, next); return false; }
        Apply(next);
        return true;
    }
    public void ForceState(GameState next)
    {
        if (next == Current) return;
        Apply(next);
    }

    public void Pause()
    {
        if (!CanPause) return;
        stateBeforePause = Current;
        Apply(GameState.OnPause);
    }

    public void Resume()
    {
        if (Current != GameState.OnPause) return;
        Apply(stateBeforePause);
    }

    public void PauseGame() => Pause();
    public void ResumeGame() => Resume();
    void Apply(GameState next)
    {
        if (next != GameState.OnPause) silentPause = false;
        Previous = Current;
        Current = next;
        if (logTransitions) Debug.Log($"[GameState] {Previous} -> {Current}");
        EventBus.Publish(new GameStateChanged(Previous, Current));
    }

    static bool IsValidTransition(GameState from, GameState to)
    {
        switch (from)
        {
            case GameState.MainMenu: return to == GameState.OnGame || to == GameState.OnGameOver;
            case GameState.OnGame: return to == GameState.OnMinigame || to == GameState.OnGameOver || to == GameState.MainMenu;
            case GameState.OnMinigame: return to == GameState.OnGame || to == GameState.MainMenu;
            case GameState.OnGameOver: return to == GameState.OnGame || to == GameState.MainMenu;
            default: return false;
        }
    }

    static void WarnInvalid(GameState from, GameState to)
    {
        Debug.LogWarning($"[GameState] Transicion no permitida: {from} -> {to}");
    }

    public enum Phase { MainMenu, Hitting, Shop, PerkChoice, BetweenRuns }

    static StatsManager Stats => StatsManager.Instance;
    static ResourcesManager Res => ResourcesManager.Instance;
    static StaminaManager Stam => StaminaManager.Instance;
    static CombatManager Combat => CombatManager.Instance;
    static MacheteShopManager Machetes => MacheteShopManager.Instance;
    static SkillTreeManager Skills => SkillTreeManager.Instance;
    static PerkManager Perks => PerkManager.Instance;
    static LegacyManager Legacy => LegacyManager.Instance;
    static DebtManager Debt => DebtManager.Instance;
    static RunManager Run => RunManager.Instance;
    static GameUI Ui => GameUI.Instance;
    static float Src(SS s, ST t) => Stats.GetSource(s, t);
    static void Src(SS s, ST t, float v) => Stats.Set(s, t, v);

    public event System.Action OnDayStarted;
    public event System.Action OnRunReset;
    public event System.Action OnRunStarted;
    public event System.Action OnRecaudacionShown;

    void OnEnable()
    {
        EventBus.Subscribe<DayStarted>(RelayDayStarted);
        EventBus.Subscribe<RunReset>(RelayRunReset);
        EventBus.Subscribe<RunStarted>(RelayRunStarted);
        EventBus.Subscribe<RecaudacionShown>(RelayRecaudacionShown);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<DayStarted>(RelayDayStarted);
        EventBus.Unsubscribe<RunReset>(RelayRunReset);
        EventBus.Unsubscribe<RunStarted>(RelayRunStarted);
        EventBus.Unsubscribe<RecaudacionShown>(RelayRecaudacionShown);
    }

    void RelayDayStarted(DayStarted e) => OnDayStarted?.Invoke();
    void RelayRunReset(RunReset e) => OnRunReset?.Invoke();
    void RelayRunStarted(RunStarted e) => OnRunStarted?.Invoke();
    void RelayRecaudacionShown(RecaudacionShown e) => OnRecaudacionShown?.Invoke();
    public Phase currentPhase => (Phase)(int)Run.Phase;
    public bool IsHittingPhase() => Run.IsHitting;
    public int DaysToPlay => Run.DaysToPlay;
    public bool IsOrderUrgent => Run.IsOrderUrgent;
    public int runDayCount { get => Run.RunDayCount; set => Run.RunDayCount = value; }
    public bool isPaused => IsPaused;
    public bool HasRunToResume() => Run.HasRunToResume();
    public void BeginNewGame() => Run.BeginNewGame();
    public void StartFreshRun() => Run.StartFreshRun();
    public void ResumeRun() => Run.ResumeRun();
    public void PauseToMainMenu() => Run.PauseToMainMenu();
    public void RestartRun() => Run.RestartRun();
    public void GoToMainMenu() => Run.GoToMainMenu();
    public void QuitGame() => Run.QuitGame();
    public void ContinueToNextDay() => Run.ContinueToNextDay();
    public void PayDebt() => Run.PayDebt();
    public void DeclareBankruptcy() => Run.DeclareBankruptcy();
    public void ContinueAfterBankruptcy() => Run.ContinueAfterBankruptcy();
    public bool TimedSpawnUnlocked => Skills.TimedSpawnUnlocked;
    public bool StreakUnlocked => Skills.StreakUnlocked;
    public int GetCoconutHpMax() => Combat.coconutHpMax;
    public static string FormatWater(float ml) => ResourcesManager.FormatWater(ml);
    public void Log(string msg) => GameLog.Say(msg);
    public void AddWater(float ml) => Res.AddWater(ml);
    public int killsAtDayStart { get => Combat.killsAtDayStart; set => Combat.killsAtDayStart = value; }

    public bool PlayWaterDropEffect(Vector3 worldPosition, float waterAmountML, GameObject deathFx = null)
    {
        if (WaterJarGroup.Instance == null) return false;
        return WaterJarGroup.Instance.HandleCoconutWater(worldPosition, waterAmountML, deathFx);
    }

    public void ToggleUpgradeTreePanel() => Ui.ToggleUpgradeTreePanel();
    public void ToggleTiendaPanel() => Ui.ToggleTiendaPanel();
    public void ToggleRewardMachinePanel() => Ui.ToggleRewardMachinePanel();
    public void ToggleDeudaPanel() => Ui.ToggleDeudaPanel();
    public void ChoosePerk(int index) => Ui.ChoosePerk(index);
    public void RefreshAllUI() => Ui.RefreshAllUI();
    public void RecenterUpgradeTree() => Ui.RecenterUpgradeTree();
    public CoconutTypeSO GetNextLockedCoconut() => Ui.GetNextLockedCoconut();
    public float GetNextUnlockProgress01() => Ui.GetNextUnlockProgress01();
    public GameObject machete => Ui.machete;
    public GameObject gameplayUIPanel => Ui.gameplayUIPanel;
    public GameObject recaudacionPanel => Ui.recaudacionPanel;
    public GameObject upgradeTreePanel => Ui.upgradeTreePanel;
    public GameObject tiendaPanel => Ui.tiendaPanel;
    public GameObject rewardMachinePanel => Ui.rewardMachinePanel;
    public GameObject deudaPanel => Ui.deudaPanel;
    public GameObject perkChoiceUIPanel => Ui.perkChoiceUIPanel;
    public GameObject betweenRunsPanel => Ui.betweenRunsPanel;
    public GameObject indicacionesPanel => Ui.indicacionesPanel;
    public CoconutUnlockPanel unlockPanel => Ui.unlockPanel;
    public void BuyNextMachete() => Machetes.BuyNextMachete();
    public void UnlockSkill(string id) => Skills.UnlockSkill(id);
    public void BuyLegacyItem(string itemName) => Legacy.BuyLegacyItem(itemName);
    public bool CanUseGacha() => Res.Coins >= 1;
    bool IsMacheteOwned(int index) => Machetes.IsOwned(index); 
    public void RefreshCombatStats() => Machetes.RecalculateStats();
    public float GetSkillMoneyMultiplierBonusPercent() => Src(SS.Skill, ST.WaterMult) * 100f;
    public float GetSkillWaterMultiplierBonusPercent() => Src(SS.Skill, ST.WaterMult) * 100f;
    public float waterTargetML => Debt.TargetWater;  
    public int billCycle { get => Debt.BillCycle; set => Debt.BillCycle = value; }
    public int daysLeft { get => Debt.DaysLeft; set => Debt.DaysLeft = value; }
    public int bestCycleReached { get => Debt.BestCycleReached; set => Debt.BestCycleReached = value; }
    public float waterCurrentML { get => Res.Water; set => Res.SetWater(value); }
    public int coconutCoins { get => Res.Coins; set => Res.SetCoins(value); }
    public float stamina { get => Stam.Current; set => Stam.Current = value; }
    public float staminaMaxBase { get => Stats.staminaMaxBase; set => Stats.staminaMaxBase = value; }
    public float staminaStartDelay { get => Stam.startDelay; set => Stam.startDelay = value; }
    public float legacyMultiplier { get => Stats.legacyMultiplier; set => Stats.legacyMultiplier = value; }
    public float coinDropChance { get => Stats.coinDropChance; set => Stats.coinDropChance = value; }
    public float baseWaterPerKill { get => Combat.baseWaterPerKill; set => Combat.baseWaterPerKill = value; }
    public float waterVariance { get => Combat.waterVariance; set => Combat.waterVariance = value; }
    public float executeHpThreshold { get => Combat.executeHpThreshold; set => Combat.executeHpThreshold = value; }
    public float earlyDayDuration { get => Combat.earlyDayDuration; set => Combat.earlyDayDuration = value; }
    public float jackpotBaseMultiplier { get => Combat.jackpotBaseMultiplier; set => Combat.jackpotBaseMultiplier = value; }
    public int streakThreshold { get => Combat.streakThreshold; set => Combat.streakThreshold = value; }
    public float streakDamageBonusPerHit { get => Combat.streakDamageBonusPerHit; set => Combat.streakDamageBonusPerHit = value; }
    public float streakWaterBonusPerHit { get => Combat.streakWaterBonusPerHit; set => Combat.streakWaterBonusPerHit = value; }
    public int streakMaxStacks { get => Combat.streakMaxStacks; set => Combat.streakMaxStacks = value; }
    public int totalCoconutsKilled { get => Combat.totalCoconutsKilled; set => Combat.totalCoconutsKilled = value; }
    public int runCoconutsKilled { get => Combat.runCoconutsKilled; set => Combat.runCoconutsKilled = value; }
    public int runMoneyEarned { get => Combat.runMoneyEarned; set => Combat.runMoneyEarned = value; }
    public int bestRunKills { get => Combat.bestRunKills; set => Combat.bestRunKills = value; }
    public int dayCoconutsKilled { get => Combat.dayCoconutsKilled; set => Combat.dayCoconutsKilled = value; }
    public int dayMoneyEarned { get => Combat.dayMoneyEarned; set => Combat.dayMoneyEarned = value; }
    public int daySwings { get => Combat.daySwings; set => Combat.daySwings = value; }
    public int dayHits { get => Combat.dayHits; set => Combat.dayHits = value; }
    public int dayCoinsFound { get => Combat.dayCoinsFound; set => Combat.dayCoinsFound = value; }
    public bool lastKillFoundCoin { get => Combat.lastKillFoundCoin; set => Combat.lastKillFoundCoin = value; }
    public bool lastKillHadJackpot { get => Combat.lastKillHadJackpot; set => Combat.lastKillHadJackpot = value; }
    public float skillCritChanceBonus { get => Src(SS.Skill, ST.CritChance); set => Src(SS.Skill, ST.CritChance, value); }
    public float perkDamageBonus { get => Src(SS.Perk, ST.Damage); set => Src(SS.Perk, ST.Damage, value); }
    public float perkStaminaMaxBonus { get => Src(SS.Perk, ST.StaminaMax); set => Src(SS.Perk, ST.StaminaMax, value); }
    public float perkWaterMultiplierBonus { get => Src(SS.Perk, ST.WaterMult); set => Src(SS.Perk, ST.WaterMult, value); }
    public float perkSwingIntervalReduction { get => Src(SS.Perk, ST.SwingReduction); set => Src(SS.Perk, ST.SwingReduction, value); }
    public float perkCritChanceBonus { get => Src(SS.Perk, ST.CritChance); set => Src(SS.Perk, ST.CritChance, value); }
    public float relicDamageBonus { get => Src(SS.Relic, ST.Damage); set => Src(SS.Relic, ST.Damage, value); }
    public float relicStaminaMaxBonus { get => Src(SS.Relic, ST.StaminaMax); set => Src(SS.Relic, ST.StaminaMax, value); }
    public float relicWaterMultiplierBonus { get => Src(SS.Relic, ST.WaterMult); set => Src(SS.Relic, ST.WaterMult, value); }
    public float relicSwingIntervalReduction { get => Src(SS.Relic, ST.SwingReduction); set => Src(SS.Relic, ST.SwingReduction, value); }
    public float relicHitRadiusBonus { get => Src(SS.Relic, ST.HitRadius); set => Src(SS.Relic, ST.HitRadius, value); }
    public float relicCritChanceBonus { get => Src(SS.Relic, ST.CritChance); set => Src(SS.Relic, ST.CritChance, value); }
    public float legacyDamageBonus { get => Src(SS.Legacy, ST.Damage); set => Src(SS.Legacy, ST.Damage, value); }
    public float legacyStaminaBonus { get => Src(SS.Legacy, ST.StaminaMax); set => Src(SS.Legacy, ST.StaminaMax, value); }
    public float legacyWaterMultiplierBonus { get => Src(SS.Legacy, ST.WaterMult); set => Src(SS.Legacy, ST.WaterMult, value); }
    public float legacyCritChanceBonus { get => Src(SS.Legacy, ST.CritChance); set => Src(SS.Legacy, ST.CritChance, value); }
    public float GetCritChance() => Stats.CritChance;
    public float GetCritMultiplier() => Stats.CritMultiplier;
    public float GetStaminaMax() => Stats.StaminaMax;
    public void ApplyTemporaryCurse(float durationSeconds) => Stats.ApplyTemporaryCurse(durationSeconds);
    public void ApplyStaminaPenalty(float amount) => Stam.ApplyPenalty(amount);
    public void RegisterSwingResult(bool hit) => Combat.RegisterSwingResult(hit);
    public float GetStreakDamageMultiplier() => Combat.GetStreakDamageMultiplier();
    public float GetStreakWaterMultiplier() => Combat.GetStreakWaterMultiplier();
    public float GetLastBreathMultiplier() => Combat.GetLastBreathMultiplier();
    public float GetExecuteDamageMultiplier(float targetHpPercent) => Combat.GetExecuteDamageMultiplier(targetHpPercent);
    public float GetMultiHitMultiplier(int targetsHit) => Combat.GetMultiHitMultiplier(targetsHit);
    public int OnCoconutDestroyed(float lootMultiplier, out float waterGained) => Combat.OnCoconutDestroyed(lootMultiplier, out waterGained);
    public int OnCoconutDestroyed(float lootMultiplier = 1f) => Combat.OnCoconutDestroyed(lootMultiplier);
    public void ApplyChainSplash(Vector3 position, int killingDamage, GameObject source) => Combat.ApplyChainSplash(position, killingDamage, source);
    public List<MacheteData> macheteOptions => Machetes.Options;
    public int equippedMacheteIndex { get => Machetes.EquippedIndex; set => Machetes.EquippedIndex = value; }
    public MacheteData GetMacheteData(int index) => Machetes.Get(index);
    public MacheteData GetCurrentMachete() => Machetes.Current;
    public MacheteData GetNextMachete() => Machetes.Next;
    public bool CanAffordAnyMachete() => Machetes.CanAffordAny();
    public List<SkillNode> skillTree => Skills.Nodes;
    public SkillNode GetSkillNode(string id) => Skills.GetNode(id);
    public bool IsSkillUnlocked(string id) => Skills.IsUnlocked(id);
    public bool CanUnlockSkillPublic(string id) => Skills.CanUnlock(id);
    public bool CanAffordAnySkill() => Skills.CanAffordAny();
    public List<PerkOption> perkPool => Perks.Pool;
    public int maxCopiesPerPerk { get => Perks.maxCopiesPerPerk; set => Perks.maxCopiesPerPerk = value; }
    public int legacyPoints { get => Legacy.Points; set => Legacy.Points = value; }
    public int totalLegacyPointsEarned { get => Legacy.TotalEarned; set => Legacy.TotalEarned = value; }
    public List<LegacyItem> legacyShop => Legacy.Items;
    public LegacyItem GetLegacyItem(string itemName) => Legacy.GetItem(itemName);
    public bool IsLegacyItemPurchased(string itemName) => Legacy.IsPurchased(itemName);
    public float GetLegacyHpScale() => Legacy.GetHpScale(billCycle);
    public float GetLegacyLootScale() => Legacy.GetLootScale(billCycle);
}