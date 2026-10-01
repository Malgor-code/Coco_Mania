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
    [Tooltip("Boton para colocar el objeto en una ranura. Solo responde si interactable=true al llamar Setup().")]
    public Button selectButton;
    [Tooltip("Opcional: muestra 'x3' cuando tienes varias copias (temporales).")]
    public TMP_Text countText;
    [Tooltip("Opcional: marca que se muestra cuando el objeto ya esta colocado en una ranura.")]
    public GameObject equippedMarker;
    [Tooltip("Opcional: se atenua cuando el objeto esta desbloqueado pero no lo tienes en este intento.")]
    public CanvasGroup canvasGroup;
    public float unavailableAlpha = 0.45f;

    private RewardData data;
    private Action<RewardData> onClick;

    public void Setup(RewardData data, bool revealed, bool interactable, Action<RewardData> onClick, int count = 0, bool equipped = false)
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
        if (countText != null) countText.text = count > 1 ? "x" + count : "";
        if (equippedMarker != null) equippedMarker.SetActive(equipped);
        if (canvasGroup != null) canvasGroup.alpha = (revealed && !interactable && !equipped) ? unavailableAlpha : 1f;

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