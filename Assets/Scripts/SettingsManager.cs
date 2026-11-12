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

    [Header("Idioma (dropdown, mismo orden que el enum Language)")]
    public TMP_Dropdown languageDropdown;

    private static readonly string[] LanguageNames = { "Español", "English" };
    private List<Resolution> filteredResolutions = new List<Resolution>();

    void Start()
    {
        SetupResolutions();

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        SetupLanguageDropdown();
    }

    void SetupLanguageDropdown()
    {
        if (languageDropdown == null) return;

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string>(LanguageNames));

        int currentIndex = (LocalizationManager.Instance != null) ? (int)LocalizationManager.Instance.currentLanguage : 0;
        languageDropdown.value = currentIndex;
        languageDropdown.RefreshShownValue();

        languageDropdown.onValueChanged.AddListener(SetLanguageFromDropdown);
    }

    void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        filteredResolutions.Clear();

        Resolution[] all = Screen.resolutions;
        HashSet<string> seen = new HashSet<string>();
        List<string> options = new List<string>();
        for (int i = all.Length - 1; i >= 0; i--)
        {
            string key = all[i].width + "x" + all[i].height;
            if (seen.Contains(key)) continue;

            seen.Add(key);
            filteredResolutions.Insert(0, all[i]);
        }

        int currentIndex = 0;
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            Resolution r = filteredResolutions[i];
            options.Add(r.width + " x " + r.height);

            if (r.width == Screen.width && r.height == Screen.height)
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
        if (index < 0 || index >= filteredResolutions.Count) return;
        Resolution r = filteredResolutions[index];
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