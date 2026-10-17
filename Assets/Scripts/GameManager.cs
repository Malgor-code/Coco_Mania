using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using static MacheteData;
using Unity.VisualScripting;

public partial class GameManager : MonoBehaviour
{
    public enum Phase { MainMenu, Hitting, Shop, PerkChoice, BetweenRuns }
    public enum CoconutAbility
    {
        Ninguna,
        Generoso,
        Resistente,
        FibraDura,
        Pesado,
        Tenaz,
        Armadura,
        Rebote,
        Fortificado,
        Implacable,
        FragilValioso,
        Regeneracion,
        Camuflaje,
        Maldicion,
        Supremo
    }

    public static GameManager Instance;

    [System.Serializable]
    public class CoconutUnlockThreshold
    {
        [HideInInspector] public string coconutName;
        [HideInInspector] public int cycleRequired;
        [HideInInspector] public CoconutAbility ability = CoconutAbility.Ninguna;

        public Sprite icon;
        public GameObject prefab;
    }

    [System.Serializable]
    public class PerkIconEntry
    {
        public string perkName;
        public Sprite icon;
    }

    void Awake()
    {
        Instance = this;
        earlyDayDuration = 5f;
        EnsureDefaultData();
    }

    void Start()
    {
        if (gameplayUIPanel != null) gameplayUIPanel.SetActive(false);
        if (recaudacionPanel != null) recaudacionPanel.SetActive(false);
        if (upgradeTreePanel != null) upgradeTreePanel.SetActive(false);
        if (tiendaPanel != null) tiendaPanel.SetActive(false);
        if (rewardMachinePanel != null) rewardMachinePanel.SetActive(false);
        if (deudaPanel != null) deudaPanel.SetActive(false);
        if (perkChoiceUIPanel != null) perkChoiceUIPanel.SetActive(false);
        if (betweenRunsPanel != null) betweenRunsPanel.SetActive(false);
        if (machete != null) machete.SetActive(false);
        if (indicacionesPanel != null) indicacionesPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        SetCursorVisible(true);
        LoadGame();
        RecalculateAllStats();
        UpdateMoneyUI();
        if (bankruptcyButton != null) bankruptcyButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && currentPhase != Phase.MainMenu && !CoconutUnlockPanel.BlocksPause && !StoryManager.BlocksPause)
        {
            if (isPaused) ResumeGame(); else PauseGame();
        }

        if (currentPhase == Phase.Hitting)
        {
            if (dayElapsed >= staminaStartDelay)
            {
                stamina -= Time.deltaTime;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    EnterRecaudacionPhase();
                }
            }

            bool shouldBeActive = skillLastBreathSwingReduction > 0f && stamina <= GetStaminaMax() * 0.2f;
            if (shouldBeActive != lastBreathSwingActive)
            {
                lastBreathSwingActive = shouldBeActive;
                RecalculateAllStats();
            }
            dayElapsed += Time.deltaTime;
        }

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = GetStaminaMax();
            staminaSlider.value = stamina;
        }

        UpdateMoneyUI();
        HandleUpgradeTreePanZoom();
    }

    void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
    }
    public bool IsHittingPhase() => currentPhase == Phase.Hitting;
    public bool TimedSpawnUnlocked => unlockedSkillIds.Contains("aparicion_1");
    public bool StreakUnlocked => unlockedSkillIds.Contains("racha_1");
    public int DaysToPlay => Mathf.Max(0, skipNextDayDecrement ? daysLeft : daysLeft - 1);
    public bool IsOrderUrgent => waterCurrentML < waterTargetML && DaysToPlay <= 1;

    int GetDayLimitBase() => dayLimitBase + legacyExtraDays;

    public int GetCoconutHpMax()
    {
        return coconutHpMaxBase;
    }

    string Loc(string key) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
    string Loc(string key, params object[] args) => LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key, args) : key;

    public static string FormatWater(float ml)
    {
        if (ml >= 1000f) return (ml / 1000f).ToString("0.0") + " L";
        return Mathf.RoundToInt(ml) + " mL";
    }

    public bool PlayWaterDropEffect(Vector3 worldPosition, float waterAmountML, GameObject deathFx = null)
    {
        if (WaterJarGroup.Instance == null) return false;
        return WaterJarGroup.Instance.HandleCoconutWater(worldPosition, waterAmountML, deathFx);
    }

    public void Log(string msg)
    {
        if (logText != null) logText.text = msg;
    }

    void BetaLog(string evt)
    {
        try
        {
            string line = System.DateTime.Now.ToString("HH:mm:ss") + "," + evt
                + ",run_t=" + (Time.time - runStartTime).ToString("F0")
                + ",cycle=" + billCycle + ",days=" + daysLeft
                + ",water=" + waterCurrentML.ToString("F0") + ",target=" + waterTargetML.ToString("F0")
                + ",machete=" + equippedMacheteIndex + ",skills=" + unlockedSkillIds.Count
                + ",swings_per_kill=" + (dayCoconutsKilled > 0 ? (daySwings / (float)dayCoconutsKilled).ToString("F1") : "0")
                + ",legacy=" + totalLegacyPointsEarned + ",hpscale=" + GetLegacyHpScale().ToString("F2")
                + ",kills=" + totalCoconutsKilled + "\n";
            System.IO.File.AppendAllText(System.IO.Path.Combine(Application.persistentDataPath, "beta_log.csv"), line);
        }
        catch { }
    }
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        SetCursorVisible(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        SetCursorVisible(currentPhase != Phase.Hitting);
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
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
}