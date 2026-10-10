using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LegacyItemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Nombre EXACTO del item en GameManager.legacyShop")]
    public string itemName;

    [Header("Visual (solo imagen)")]
    public Image iconImage;
    [Tooltip("Opcional: si lo asignas se pone como sprite del boton.")]
    public Sprite icon;
    public Color normalColor = Color.white;
    public Color cantAffordColor = new Color(1f, 1f, 1f, 0.55f);
    public Color purchasedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [Tooltip("Opcional: objeto (palomita, marco) que se activa cuando ya lo compraste.")]
    public GameObject purchasedBadge;

    private bool hovering;
    private bool lastPurchased;
    private bool lastCanAfford;

    void Awake()
    {
        if (iconImage == null) iconImage = GetComponent<Image>();
        if (icon != null && iconImage != null) iconImage.sprite = icon;
    }

    public void OnClick()
    {
        if (GameManager.Instance != null) GameManager.Instance.BuyLegacyItem(itemName);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        LegacyItem item = gm.GetLegacyItem(itemName);
        if (item == null) return;

        bool purchased = gm.IsLegacyItemPurchased(itemName);
        bool canAfford = gm.legacyPoints >= item.cost;

        if (iconImage != null)
            iconImage.color = purchased ? purchasedColor : (canAfford ? normalColor : cantAffordColor);
        if (purchasedBadge != null) purchasedBadge.SetActive(purchased);
        if (hovering && (purchased != lastPurchased || canAfford != lastCanAfford))
            ShowTooltip(item, purchased, canAfford);

        lastPurchased = purchased;
        lastCanAfford = canAfford;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        LegacyItem item = gm.GetLegacyItem(itemName);
        if (item == null) return;

        ShowTooltip(item, gm.IsLegacyItemPurchased(itemName), gm.legacyPoints >= item.cost);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (LegacyTooltip.Instance != null) LegacyTooltip.Instance.Hide();
    }

    void OnDisable()
    {
        if (hovering && LegacyTooltip.Instance != null) LegacyTooltip.Instance.Hide();
        hovering = false;
    }

    void ShowTooltip(LegacyItem item, bool purchased, bool canAfford)
    {
        if (LegacyTooltip.Instance != null) LegacyTooltip.Instance.Show(item, purchased, canAfford);
    }
}