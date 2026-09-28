using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardFragmentShopUI : MonoBehaviour, IRewardSubPanel
{
    public GameObject panelRoot;
    public RewardMachineManager machine;
    [Tooltip("El Transform con un Layout Group donde se listan las recompensas elegibles del nivel seleccionado.")]
    public Transform gridParent;
    public RewardItemUI itemPrefab;
    public Button closeButton;
    public TMP_Text fragmentsText;

    [Header("Botones de nivel (los 4 precios fijos del diseño)")]
    [Tooltip("10 fragmentos -> cualquier recompensa de rareza Comun.")]
    public Button tierCommonButton;
    [Tooltip("25 fragmentos -> cualquier recompensa de categoria Permanente (Reliquia).")]
    public Button tierPermanentButton;
    [Tooltip("75 fragmentos -> cualquier Objeto Fisico.")]
    public Button tierPhysicalButton;
    [Tooltip("150 fragmentos -> cualquier recompensa Legendaria.")]
    public Button tierLegendaryButton;

    [Header("Confirmacion de compra (opcional)")]
    public GameObject confirmPanel;
    public TMP_Text confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private RewardData pendingChoice;
    private int pendingPrice;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;


    void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        if (tierCommonButton != null)
            tierCommonButton.onClick.AddListener(() => ShowTier(null, RewardRarity.Common, machine.shopPriceCommon));
        if (tierPermanentButton != null)
            tierPermanentButton.onClick.AddListener(() => ShowTier(RewardCategory.PermanentRelic, null, machine.shopPricePermanent));
        if (tierPhysicalButton != null)
            tierPhysicalButton.onClick.AddListener(() => ShowTier(RewardCategory.PhysicalItem, null, machine.shopPricePhysical));
        if (tierLegendaryButton != null)
            tierLegendaryButton.onClick.AddListener(() => ShowTier(null, RewardRarity.Legendary, machine.shopPriceLegendary));

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ConfirmPurchase);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(() => { if (confirmPanel != null) confirmPanel.SetActive(false); });

        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshFragmentsText();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void RefreshFragmentsText()
    {
        if (fragmentsText != null && machine != null) fragmentsText.text = "Fragmentos: " + machine.persistent.fragments;
    }

    void ShowTier(RewardCategory? category, RewardRarity? rarity, int price)
    {
        if (gridParent == null || itemPrefab == null || machine == null) return;

        foreach (Transform child in gridParent) Destroy(child.gameObject);

        List<RewardData> pool = machine.GetShopPool(category, rarity);
        foreach (var data in pool)
        {
            RewardItemUI item = Instantiate(itemPrefab, gridParent);
            item.Setup(data, true, true, chosen => RequestPurchase(chosen, price));
        }
    }

    void RequestPurchase(RewardData data, int price)
    {
        pendingChoice = data;
        pendingPrice = price;

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            if (confirmText != null) confirmText.text = $"¿Comprar '{data.displayName}' por {price} fragmentos?";
        }
        else
        {
            ConfirmPurchase();
        }
    }

    void ConfirmPurchase()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (pendingChoice == null || machine == null) return;

        machine.BuyFromFragmentShop(pendingChoice.id, pendingPrice);
        RefreshFragmentsText();
        pendingChoice = null;
    }
}