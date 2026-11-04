using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using static MacheteData;

public partial class GameManager
{
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
        runDayCount++;
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

    void ActivateHittingPhase()
    {
        currentPhase = Phase.Hitting;
        if (machete != null) machete.SetActive(true);
        RecalculateAllStats();
        OnDayStarted?.Invoke();
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
        if (EventManager.Instance != null && EventManager.Instance.ShouldTrigger(runDayCount))
        {
            BeginEventPhase();
            EventManager.Instance.StartEvent(StartDayAfterEvent);
            return;
        }
        StartDayAfterEvent();
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

    public void ToggleDeudaPanel()
    {
        if (forcedOrderDecision) return;
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

    // ---------------- Pedido / bancarrota ----------------

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
        runDayCount = 0;
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
}