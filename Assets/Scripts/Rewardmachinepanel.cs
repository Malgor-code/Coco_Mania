using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardMachinePanel : MonoBehaviour
{
    [Header("Referencias")]
    public RewardMachineManager machine;
    [Tooltip("Escena 3D del gacha (camara + palmera). Es la que cambia de camara y agita.")]
    public GachaMachineScene scene;
    [Tooltip("El GameObject raiz de la UI que se activa/desactiva con OpenPanel/ClosePanel/TogglePanel.")]
    public GameObject panelRoot;

    [Header("UI - Cabecera")]
    public TMP_Text titleText;
    public TMP_Text coinsText;

    [Header("UI - Tiradas (cada boton elige el tipo y arma la palmera)")]
    public Button normalPullButton;
    public Button boostedPullButton;
    public Button premiumPullButton;
    [Tooltip("(Opcional) Textos dentro de cada boton, para mostrar el costo.")]
    public TMP_Text normalPullLabel;
    public TMP_Text boostedPullLabel;
    public TMP_Text premiumPullLabel;
    [Tooltip("Indicaciones: 'Elige una tirada' / '¡Agita el mouse!'.")]
    public TMP_Text hintText;

    [Header("UI - Otros botones")]
    public Button collectionButton;
    public Button closeButton;

    [Header("UI - Probabilidades")]
    public TMP_Text probabilitiesText;

    [Header("UI - Resultado de la ultima tirada")]
    public GameObject resultPanel;
    [Tooltip("Opcional. Si lo asignas, el resultado se desvanece suavemente en vez de desaparecer de golpe.")]
    public CanvasGroup resultCanvasGroup;
    public float resultAutoHideDelay = 5f;
    public float resultFadeOutDuration = 0.6f;
    public Image resultIcon;
    public TMP_Text resultNameText;
    public TMP_Text resultRarityText;
    public TMP_Text resultDescriptionText;

    [Header("Sub-panel: Coleccion")]
    public RewardCollectionUI collectionUI;

    [Header("Sonido del resultado (opcional)")]
    public AudioSource audioSource;
    [Tooltip("Opcional: un sonido distinto por rareza, en orden Common, Uncommon, Rare, Epic, Legendary.")]
    public AudioClip[] raritySounds;

    [Header("Colores de rareza (para el texto de resultado)")]
    public Color commonColor = Color.white;
    public Color uncommonColor = new Color(0.3f, 0.85f, 0.3f);
    public Color rareColor = new Color(0.3f, 0.55f, 1f);
    public Color epicColor = new Color(0.65f, 0.25f, 0.85f);
    public Color legendaryColor = new Color(1f, 0.65f, 0f);

    static readonly string[] RarityNames = { "Comun", "Poco comun", "Raro", "Epico", "Legendario" };

    private Coroutine resultHideRoutine;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    void Start()
    {
        if (normalPullButton != null) normalPullButton.onClick.AddListener(() => OnTierPressed(PullTier.Normal));
        if (boostedPullButton != null) boostedPullButton.onClick.AddListener(() => OnTierPressed(PullTier.Boosted));
        if (premiumPullButton != null) premiumPullButton.onClick.AddListener(() => OnTierPressed(PullTier.Premium));
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (collectionButton != null) collectionButton.onClick.AddListener(OpenCollectionTab);

        if (machine != null) machine.OnStateChanged += RefreshUI;
        if (scene != null)
        {
            scene.OnPullFinished += HandlePullFinished;
            scene.OnArmedChanged += RefreshUI;
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        RefreshUI();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    void OnDestroy()
    {
        if (machine != null) machine.OnStateChanged -= RefreshUI;
        if (scene != null)
        {
            scene.OnPullFinished -= HandlePullFinished;
            scene.OnArmedChanged -= RefreshUI;
        }
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (scene != null) scene.Enter();
        else Debug.LogWarning("[RewardMachinePanel] Falta asignar 'Scene' (GachaMachineScene).");
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (scene != null) scene.Exit();
        if (panelRoot != null) panelRoot.SetActive(false);
        if (resultHideRoutine != null) { StopCoroutine(resultHideRoutine); resultHideRoutine = null; }
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) ClosePanel(); else OpenPanel();
    }

    public void OpenCollectionTab()
    {
        if (collectionUI == null) return;
        if (collectionUI.IsOpen) collectionUI.Hide();
        else
        {
            if (scene != null) scene.Disarm();
            collectionUI.Show();
        }
        RefreshUI();
    }

    void OnTierPressed(PullTier tier)
    {
        if (scene == null || machine == null || scene.IsPulling) return;
        if (scene.IsArmed && scene.CurrentTier == tier) { scene.Disarm(); return; }

        if (resultPanel != null && resultPanel.activeSelf)
        {
            if (resultHideRoutine != null) { StopCoroutine(resultHideRoutine); resultHideRoutine = null; }
            resultPanel.SetActive(false);
        }

        scene.Arm(tier);
        RefreshUI();
    }

    void HandlePullFinished(RewardData data, bool wasDuplicate)
    {
        ShowResult(data, wasDuplicate);
        RefreshUI();
    }

    void ShowResult(RewardData data, bool wasDuplicate)
    {
        if (resultHideRoutine != null) StopCoroutine(resultHideRoutine);

        if (resultPanel != null) resultPanel.SetActive(data != null);
        if (resultCanvasGroup != null) resultCanvasGroup.alpha = 1f;

        if (data == null) return;

        if (resultIcon != null) resultIcon.sprite = data.icon;
        if (resultNameText != null) resultNameText.text = data.displayName;

        if (resultDescriptionText != null)
        {
            resultDescriptionText.text = wasDuplicate
                ? data.description + "\n\n(Duplicado -> se te devolvieron las monedas)"
                : data.description;
        }

        if (resultRarityText != null)
        {
            resultRarityText.text = data.rarity.ToString();
            resultRarityText.color = GetRarityColor(data.rarity);
        }

        int idx = (int)data.rarity;
        if (raritySounds != null && idx < raritySounds.Length && audioSource != null && raritySounds[idx] != null)
            audioSource.PlayOneShot(raritySounds[idx]);

        resultHideRoutine = StartCoroutine(AutoHideResultAfterDelay());
    }

    IEnumerator AutoHideResultAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, resultAutoHideDelay));

        if (resultCanvasGroup != null)
        {
            float startAlpha = resultCanvasGroup.alpha;
            float t = 0f;
            while (t < resultFadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                resultCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, resultFadeOutDuration > 0f ? t / resultFadeOutDuration : 1f);
                yield return null;
            }
            resultCanvasGroup.alpha = 0f;
        }

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    Color GetRarityColor(RewardRarity r)
    {
        switch (r)
        {
            case RewardRarity.Common: return commonColor;
            case RewardRarity.Uncommon: return uncommonColor;
            case RewardRarity.Rare: return rareColor;
            case RewardRarity.Epic: return epicColor;
            default: return legendaryColor;
        }
    }

    string FormatOdds(PullTier tier, string name)
    {
        float[] o = machine.GetOdds(tier);
        float total = 0f;
        foreach (float f in o) total += f;
        if (total <= 0f) return name;

        var parts = new System.Text.StringBuilder();
        for (int i = 0; i < o.Length; i++)
        {
            if (o[i] <= 0f) continue;
            if (parts.Length > 0) parts.Append("   ");
            parts.Append(RarityNames[i]).Append(' ').Append((o[i] / total * 100f).ToString("0.#")).Append('%');
        }
        return name + " (" + machine.GetCost(tier) + "): " + parts;
    }

    void RefreshUI()
    {
        if (machine == null) return;

        int coins = GameManager.Instance != null ? GameManager.Instance.coconutCoins : 0;
        bool busy = scene != null && scene.IsPulling;

        if (coinsText != null) coinsText.text = "Monedas: " + coins;

        SetTierButton(normalPullButton, normalPullLabel, PullTier.Normal, "Normal", busy);
        SetTierButton(boostedPullButton, boostedPullLabel, PullTier.Boosted, "Mejorada", busy);
        SetTierButton(premiumPullButton, premiumPullLabel, PullTier.Premium, "Premium", busy);

        if (probabilitiesText != null)
        {
            probabilitiesText.text =
                FormatOdds(PullTier.Normal, "Normal") + "\n" +
                FormatOdds(PullTier.Boosted, "Mejorada") + "\n" +
                FormatOdds(PullTier.Premium, "Premium");
        }

        if (hintText != null)
        {
            if (busy) hintText.text = "";
            else if (scene != null && scene.IsArmed) hintText.text = "¡Agita el mouse de lado a lado!";
            else hintText.text = "Elige una tirada";
        }
    }

    void SetTierButton(Button b, TMP_Text label, PullTier tier, string name, bool busy)
    {
        if (b != null) b.interactable = !busy && machine.CanPull(tier);
        if (label != null) label.text = name + "\n" + machine.GetCost(tier);
    }
}