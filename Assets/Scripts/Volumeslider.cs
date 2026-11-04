using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    void OnEnable()
    {
        slider.onValueChanged.RemoveListener(OnChanged);

        float v = AudioManager.Instance != null
            ? AudioManager.Instance.MasterVolume
            : PlayerPrefs.GetFloat("master_volume", 1f);
        slider.SetValueWithoutNotify(v);

        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnChanged);
        PlayerPrefs.Save();
    }

    void OnChanged(float v)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(v);
    }
}