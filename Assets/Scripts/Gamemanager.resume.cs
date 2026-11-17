using UnityEngine;
using static MacheteData;

public partial class GameManager
{
    private Phase phaseBeforeMenu = Phase.MainMenu;
    public bool HasRunToResume()
    {
        return phaseBeforeMenu == Phase.Hitting
            || phaseBeforeMenu == Phase.Shop
            || phaseBeforeMenu == Phase.PerkChoice
            || phaseBeforeMenu == Phase.BetweenRuns;
    }

    public void PauseToMainMenu()
    {
        phaseBeforeMenu = currentPhase;

        GameStateManager.Force(GameState.MainMenu);

        if (CoconutSpawner.Instance != null)
        {
            CoconutSpawner.Instance.OnInitialSpawnComplete -= HandleInitialSpawnComplete;
            CoconutSpawner.Instance.ClearAllCoconuts();
        }

        HideAllGamePanels();
        currentPhase = Phase.MainMenu;
        SetCursorVisible(true);
        SaveGame();
        AppFlowManager flow = FindFirstObjectByType<AppFlowManager>();
        if (flow != null) flow.ShowMainMenu();
    }

    public void ResumeRun()
    {
        Phase target = phaseBeforeMenu;
        phaseBeforeMenu = Phase.MainMenu;

        GameStateManager.Request(target == Phase.BetweenRuns ? GameState.OnGameOver : GameState.OnGame);
        HideAllGamePanels();

        switch (target)
        {
            case Phase.Hitting:
                runDayCount = Mathf.Max(0, runDayCount - 1);
                stamina = GetStaminaMax();
                ShowHittingUI();
                break;

            case Phase.PerkChoice:
                OfferPerkChoice();
                break;

            case Phase.BetweenRuns:
                EnterBetweenRunsPhase();
                break;

            case Phase.Shop:
            default:
                currentPhase = Phase.Shop;
                SetCursorVisible(true);
                if (forcedOrderDecision)
                {
                    OpenForcedOrderPanel();
                }
                else
                {
                    if (recaudacionPanel != null) recaudacionPanel.SetActive(true);
                    RefreshAllUI();
                    RaiseRecaudacionShown();
                }
                break;
        }
    }

    public void StartFreshRun()
    {
        phaseBeforeMenu = Phase.MainMenu;
        HideAllGamePanels();
        ResetRunState();
        BeginNewGame();
    }

    void HideAllGamePanels()
    {
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
    }
}