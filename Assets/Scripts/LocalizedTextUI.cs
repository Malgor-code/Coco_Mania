using UnityEngine;
using TMPro;

public class LocalizedTextUI : MonoBehaviour
{
    public string key;

    private TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
        }
        Refresh();
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (label != null && LocalizationManager.Instance != null && !string.IsNullOrEmpty(key))
        {
            label.text = LocalizationManager.Instance.Get(key);
        }
    }
}