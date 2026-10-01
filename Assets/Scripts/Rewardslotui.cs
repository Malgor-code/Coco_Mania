using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardSlotUI : MonoBehaviour
{
    public Image iconImage;
    [Tooltip("Se muestra cuando la ranura esta vacia (ej. un '+' o un marco).")]
    public GameObject emptyIndicator;
    [Tooltip("Opcional: dias restantes de un temporal ('2 d').")]
    public TMP_Text daysText;
    public Button button;

    private int index;
    private Action<int> onClick;

    public void Setup(int index, EquippedSlot slot, RewardData data, bool canUnequip, Action<int> onClick)
    {
        this.index = index;
        this.onClick = onClick;

        bool empty = slot == null || slot.IsEmpty;

        if (emptyIndicator != null) emptyIndicator.SetActive(empty);

        if (iconImage != null)
        {
            iconImage.sprite = (!empty && data != null) ? data.icon : null;
            iconImage.enabled = !empty && data != null && data.icon != null;
        }

        if (daysText != null)
            daysText.text = (!empty && slot.isTemporary) ? slot.daysRemaining + " d" : "";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = canUnequip;
            if (canUnequip) button.onClick.AddListener(() => this.onClick?.Invoke(this.index));
        }
    }
}