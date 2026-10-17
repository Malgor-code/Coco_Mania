using UnityEngine;

public partial class GameManager
{
    [Header("Eventos (minijuegos cada N dias)")]
    [Tooltip("Dias de golpeo jugados en esta partida. Lo usa EventManager para saber cuando toca evento.")]
    public int runDayCount = 0;
    public void AddWater(float ml)
    {
        waterCurrentML += ml;
    }
    void BeginEventPhase()
    {
        currentPhase = Phase.Shop;
        if (machete != null) machete.SetActive(false);
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        SetCursorVisible(true);
        Log("¡Evento especial!");
    }
    void StartDayAfterEvent()
    {
        stamina = GetStaminaMax();
        ShowHittingUI();
        RefreshAllUI();
    }
}