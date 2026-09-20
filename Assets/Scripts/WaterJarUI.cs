using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WaterJarUI : MonoBehaviour
{
    [Header("Referencias")]
    public Image waterFillImage;
    public RectTransform jarContainer;

    [Header("Suavizado del llenado")]
    public float fillSmoothSpeed = 1.2f;
    private float displayedFill01 = 0f;

    [Header("Vaiven tipo liquido (para que no se sienta una barra estatica)")]
    public float waveAmplitude = 0.015f;
    public float waveSpeed = 2.5f;

    [Header("Color segun cercania al pedido")]
    public Color normalColor = new Color(0.85f, 0.7f, 0.25f);
    public Color fullColor = new Color(0.4f, 1f, 0.5f);
    public float colorLerpSpeed = 4f;
    private Color displayedColor;

    [Header("Pop en la jarra cada vez que ganas agua")]
    public float popStrength = 0.12f;
    public float popDuration = 0.25f;
    private Coroutine popRoutine;
    private Vector3 jarOriginalScale = Vector3.one;
    private float lastKnownWater = -1f;

    void Start()
    {
        if (jarContainer != null) jarOriginalScale = jarContainer.localScale;
        displayedColor = normalColor;
    }

    void Update()
    {
        if (GameManager.Instance == null || waterFillImage == null) return;

        float current = GameManager.Instance.waterCurrentML;
        float target = Mathf.Max(1f, GameManager.Instance.waterTargetML);
        float realFill01 = Mathf.Clamp01(current / target);

        if (lastKnownWater >= 0f && current > lastKnownWater + 0.01f)
        {
            PlayPop();
        }
        lastKnownWater = current;

        displayedFill01 = Mathf.MoveTowards(displayedFill01, realFill01, fillSmoothSpeed * Time.unscaledDeltaTime);

        float wave = Mathf.Sin(Time.unscaledTime * waveSpeed) * waveAmplitude * displayedFill01;
        waterFillImage.fillAmount = Mathf.Clamp01(displayedFill01 + wave);

        Color targetColor = realFill01 >= 1f ? fullColor : normalColor;
        displayedColor = Color.Lerp(displayedColor, targetColor, colorLerpSpeed * Time.unscaledDeltaTime);
        waterFillImage.color = displayedColor;
    }

    void PlayPop()
    {
        if (jarContainer == null) return;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine());
    }

    IEnumerator PopRoutine()
    {
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            float scaleMult = 1f + Mathf.Sin(p * Mathf.PI) * popStrength;
            jarContainer.localScale = jarOriginalScale * scaleMult;
            yield return null;
        }
        jarContainer.localScale = jarOriginalScale;
    }
}