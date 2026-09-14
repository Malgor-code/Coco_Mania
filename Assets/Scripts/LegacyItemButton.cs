using UnityEngine;
using TMPro;

public class LegacyItemButton : MonoBehaviour
{
    public string itemName;
    public TMP_Text label;

    public void OnClick()
    {
        if (GameManager.Instance != null) GameManager.Instance.BuyLegacyItem(itemName);
    }

    void Update()
    {
        if (GameManager.Instance == null || label == null) return;

        LegacyItem item = GameManager.Instance.GetLegacyItem(itemName);
        if (item == null) return;

        bool purchased = GameManager.Instance.IsLegacyItemPurchased(itemName);
        label.text = purchased ? item.itemName + "\n(adquirido)" : item.itemName + "\nCosto: " + item.cost + " legado";
    }
}