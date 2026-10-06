using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardMachinePanel : MonoBehaviour
{
    [Header("Referencias")]
    public RewardMachineManager machine;
    [Tooltip("El GameObject raiz que se activa/desactiva con OpenPanel/ClosePanel/TogglePanel.")]
    public GameObject panelRoot;

    [Header("UI - Cabecera")]
    public TMP_Text titleText;
    public TMP_Text coinsText;
    public TMP_Text pullCostText;

    [Header("UI - Botones principales")]
    public Button pullButton;
    public Button collectionButton;
    public Button closeButton;

    [Header("UI - Probabilidades")]
    public TMP_Text probabilitiesText;

    [Header("UI - Pity / garantia")]
    public TMP_Text pityEpicText;
    public TMP_Text pityLegendaryText;

    [Header("UI - Resultado de la ultima tirada")]
    public GameObject resultPanel;
    [Tooltip("Opcional. Si lo asignas, el resultado se desvanece suavemente en vez de desaparecer de golpe.")]
    public CanvasGroup resultCanvasGroup;
    [Tooltip("Segundos que el resultado queda visible antes de empezar a desvanecerse.")]
    public float resultAutoHideDelay = 5f;
    [Tooltip("Duracion del desvanecimiento (solo se usa si 'Result Canvas Group' esta asignado).")]
    public float resultFadeOutDuration = 0.6f;
    public Image resultIcon;
    public TMP_Text resultNameText;
    public TMP_Text resultRarityText;
    public TMP_Text resultDescriptionText;

    [Header("Sub-panel: Coleccion")]
    public RewardCollectionUI collectionUI;

    [Header("Animacion / sonido (opcionales, todo funciona igual si se dejan vacios)")]
    public Animator pullAnimator;
    public string pullAnimTrigger = "Pull";
    public AudioSource audioSource;
    public AudioClip pullSound;
    [Tooltip("Opcional: un sonido distinto por rareza, en orden Common, Uncommon, Rare, Epic, Legendary.")]
    public AudioClip[] raritySounds;
    [Tooltip("Cuanto se espera (en segundos) despues de disparar la animacion antes de mostrar el resultado.")]
    public float pullAnimDuration = 1.2f;

    [Header("Colores de rareza (para el texto de resultado)")]
    public Color commonColor = Color.white;
    public Color uncommonColor = new Color(0.3f, 0.85f, 0.3f);
    public Color rareColor = new Color(0.3f, 0.55f, 1f);
    public Color epicColor = new Color(0.65f, 0.25f, 0.85f);
    public Color legendaryColor = new Color(1f, 0.65f, 0f);

    private bool isPulling = false;
    private Coroutine resultHideRoutine;

    void Start()
    {
        if (pullButton != null) pullButton.onClick.AddListener(OnPullPressed);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (collectionButton != null) collectionButton.onClick.AddListener(OpenCollectionTab);

        if (machine != null) machine.OnStateChanged += RefreshUI;

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
    }

    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (resultHideRoutine != null) { StopCoroutine(resultHideRoutine); resultHideRoutine = null; }
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;
        if (panelRoot.activeSelf) ClosePanel(); else OpenPanel();
    }

    public void OpenCollectionTab()
    {
        if (collectionUI == null) return;
        if (collectionUI.IsOpen) collectionUI.Hide(); else collectionUI.Show();
    }

    void OnPullPressed()
    {
        if (isPulling || machine == null) return;
        StartCoroutine(PullRoutine());
    }

    IEnumerator PullRoutine()
    {
        isPulling = true;
        if (pullButton != null) pullButton.interactable = false;

        if (audioSource != null && pullSound != null) audioSource.PlayOneShot(pullSound);
        if (pullAnimator != null && !string.IsNullOrEmpty(pullAnimTrigger)) pullAnimator.SetTrigger(pullAnimTrigger);

        RewardData lastResult = null;
        bool lastWasDuplicate = false;
        void Handler(RewardData d, bool dup) { lastResult = d; lastWasDuplicate = dup; }

        machine.OnPullResult += Handler;
        machine.Pull();
        machine.OnPullResult -= Handler;

        if (pullAnimDuration > 0f) yield return new WaitForSeconds(pullAnimDuration);

        ShowResult(lastResult, lastWasDuplicate);
        RefreshUI();

        isPulling = false;
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
                ? data.description + "\n\n(Duplicado -> se te devolvio la moneda)"
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
        yield return new WaitForSeconds(Mathf.Max(0f, resultAutoHideDelay));

        if (resultCanvasGroup != null)
        {
            float startAlpha = resultCanvasGroup.alpha;
            float t = 0f;
            while (t < resultFadeOutDuration)
            {
                t += Time.deltaTime;
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

    void RefreshUI()
    {
        if (machine == null) return;

        int coins = GameManager.Instance != null ? GameManager.Instance.coconutCoins : 0;

        if (coinsText != null) coinsText.text = "Monedas: " + coins;
        if (pullCostText != null) pullCostText.text = "Costo por tirada: " + machine.pullCost;
        if (pullButton != null) pullButton.interactable = !isPulling && machine.CanPull();

        if (probabilitiesText != null)
        {
            probabilitiesText.text =
                $"Comun {machine.commonChance:0}%    Poco comun {machine.uncommonChance:0}%      Raro {machine.rareChance:0}%     Epico {machine.epicChance:0}%    Legendario {machine.legendaryChance:0}%";
        }

        if (pityEpicText != null)
            pityEpicText.text = $"Garantia Epica: {machine.currentRun.pullsSinceEpic} / {machine.pityEpicThreshold}";

        if (pityLegendaryText != null)
            pityLegendaryText.text = $"Garantia Legendaria: {machine.currentRun.pullsSinceLegendary} / {machine.pityLegendaryThreshold}";
    }
}