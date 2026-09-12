using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    public GameObject panel;
    public TMP_Text text;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string message)
    {
        if (panel != null) panel.SetActive(true);
        if (text != null) text.text = message;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}