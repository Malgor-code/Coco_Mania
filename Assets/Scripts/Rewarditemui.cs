using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardItemUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text rarityText;
    [Tooltip("Objeto opcional que se muestra encima cuando la recompensa esta bloqueada (ej. un candado).")]
    public GameObject lockedOverlay;
    [Tooltip("Opcional: solo se usa si interactable=true al llamar Setup().")]
    public Button selectButton;

    private RewardData data;
    private Action<RewardData> onClick;

    public void Setup(RewardData data, bool revealed, bool interactable, Action<RewardData> onClick)
    {
        this.data = data;
        this.onClick = onClick;

        if (iconImage != null)
        {
            iconImage.sprite = revealed ? data.icon : null;
            iconImage.enabled = revealed && data.icon != null;
        }

        if (nameText != null) nameText.text = revealed ? data.displayName : "???";
        if (rarityText != null) rarityText.text = revealed ? data.rarity.ToString() : "???";
        if (lockedOverlay != null) lockedOverlay.SetActive(!revealed);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.interactable = interactable;
            if (interactable)
            {
                selectButton.onClick.AddListener(() => this.onClick?.Invoke(this.data));
            }
        }
    }
}