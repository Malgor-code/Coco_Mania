using UnityEngine;

public partial class GameManager
{
    AudioContext lastAudioContext = AudioContext.None;

    AudioContext ResolveAudioContext()
    {
        if (isPaused) return AudioContext.Pause;
        if (currentPhase == Phase.MainMenu) return AudioContext.MainMenu;
        if (deudaPanel != null && deudaPanel.activeSelf) return AudioContext.Deuda;
        if (upgradeTreePanel != null && upgradeTreePanel.activeSelf) return AudioContext.UpgradeTree;
        if (tiendaPanel != null && tiendaPanel.activeSelf) return AudioContext.Tienda;
        if (rewardMachinePanel != null && rewardMachinePanel.activeSelf) return AudioContext.RewardMachine;
        if (perkChoiceUIPanel != null && perkChoiceUIPanel.activeSelf) return AudioContext.PerkChoice;
        if (betweenRunsPanel != null && betweenRunsPanel.activeSelf) return AudioContext.BetweenRuns;
        if (recaudacionPanel != null && recaudacionPanel.activeSelf) return AudioContext.Recaudacion;
        if (currentPhase == Phase.Hitting) return AudioContext.Hitting;

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
}