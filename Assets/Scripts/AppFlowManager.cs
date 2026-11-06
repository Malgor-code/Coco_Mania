using UnityEngine;

public class AppFlowManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    void Start()
    {
        ShowMainMenu();
    }
    public void OnPlayPressed()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.HasRunToResume()) gm.ResumeRun();
        else gm.BeginNewGame();
    }
    public void OnNewGamePressed()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.StartFreshRun();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnSettingsPressed()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnBackFromSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnQuitPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}