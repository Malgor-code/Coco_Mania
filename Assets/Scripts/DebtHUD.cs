using UnityEngine;
using TMPro;

public class DebtHUD : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text debtText;
    public TMP_Text daysText;

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color urgentColor = new Color(1f, 0.25f, 0.25f);

    static string L(string key, params object[] args)
    {
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(key, args)
            : key;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (debtText != null)
            debtText.text = L("debt_total", GameManager.FormatWater(gm.waterTargetML));

        if (daysText != null)
        {
            int days = gm.currentPhase == GameManager.Phase.Hitting
                ? gm.daysLeft
                : gm.DaysToPlay;

            daysText.text = days.ToString();
            daysText.color = gm.IsOrderUrgent ? urgentColor : normalColor;
        }
    }
}