using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageDropdown : MonoBehaviour
{
    TMP_Dropdown dropdown;

    void Awake() { dropdown = GetComponent<TMP_Dropdown>(); }

    void OnEnable()
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(LocalizationManager.NativeNames));
        dropdown.SetValueWithoutNotify((int)LocalizationManager.Current);
        dropdown.onValueChanged.AddListener(OnPicked);

        var loc = LocalizationManager.Instance;
        if (loc != null) loc.OnLanguageChanged += Sync;
    }

    void OnDisable()
    {
        dropdown.onValueChanged.RemoveListener(OnPicked);
        var loc = LocalizationManager.Instance;
        if (loc != null) loc.OnLanguageChanged -= Sync;
    }

    void OnPicked(int index)
    {
        var loc = LocalizationManager.Instance;
        if (loc != null) loc.SetLanguageIndex(index);
    }

    void Sync()
    {
        dropdown.SetValueWithoutNotify((int)LocalizationManager.Current);
    }
}