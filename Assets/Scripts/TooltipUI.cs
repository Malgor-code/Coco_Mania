using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    public GameObject panel;

    [Header("Formato simple (Show)")]
    public TMP_Text text;

    [Header("Formato estructurado de mejora (ShowUpgradeInfo)")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text statText;
    public TMP_Text priceText;

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

    public void ShowUpgradeInfo(string title, string description, string stat, string price)
    {
        if (panel != null) panel.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (statText != null) statText.text = stat;
        if (priceText != null) priceText.text = price;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}