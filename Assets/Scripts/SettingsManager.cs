using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Resolucion")]
    public TMP_Dropdown resolutionDropdown;

    [Header("Pantalla completa")]
    public Toggle fullscreenToggle;

    [Header("Idioma (0 = Espanol, 1 = English)")]
    public TMP_Dropdown languageDropdown;

    private Resolution[] resolutions;

    void Start()
    {
        SetupResolutions();

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (languageDropdown != null)
        {
            languageDropdown.value = (LocalizationManager.Instance != null && LocalizationManager.Instance.currentLanguage == Language.English) ? 1 : 0;
            languageDropdown.onValueChanged.AddListener(SetLanguageFromDropdown);
        }
    }

    void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].width + " x " + resolutions[i].height);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;
        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetLanguageFromDropdown(int index)
    {
        if (LocalizationManager.Instance == null) return;
        LocalizationManager.Instance.SetLanguage(index == 1 ? Language.English : Language.Spanish);
    }
}