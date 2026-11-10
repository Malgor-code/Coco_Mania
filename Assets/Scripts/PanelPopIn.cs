using UnityEngine;
using System.Collections;

public enum PopStyle
{
    Random,
    BackPop,   
    Elastic,
    SlideUp,   
    SlideDown,  
    SlideLeft, 
    SlideRight,   
    FlipHorizontal, 
    FlipVertical,
    SpinIn,     
    DropBounce
}

[RequireComponent(typeof(CanvasGroup))]
public class PanelPopIn : MonoBehaviour
{
    [Header("Estilo")]
    public PopStyle style = PopStyle.Random;

    [Header("Tiempos y valores")]
    public float duration = 0.4f;
    [Range(0f, 1f)] public float startScale = 0.4f;
    public float slideDistance = 500f;
    public float spinAngle = 120f;
    [Range(0.05f, 1f)] public float fadeFraction = 0.35f;

    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private Vector3 originalScale;
    private Vector2 originalPos;
    private Quaternion originalRot;
    private Coroutine routine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = transform as RectTransform;
        originalScale = transform.localScale;
        originalRot = transform.localRotation;
        if (rect != null) originalPos = rect.anchoredPosition;
    }

    void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Play());
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        ResetToOriginal();
    }

    IEnumerator Play()
    {
        PopStyle current = style;
        if (current == PopStyle.Random)
        {
            int count = System.Enum.GetValues(typeof(PopStyle)).Length;
            current = (PopStyle)Random.Range(1, count); // salta "Random"
        }

        float t = 0f;
        Apply(current, 0f);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            Apply(current, Mathf.Clamp01(t / duration));
            yield return null;
        }

        ResetToOriginal();
    }

    void Apply(PopStyle s, float p)
    {
        canvasGroup.alpha = Mathf.Clamp01(p / fadeFraction);
        Vector3 scale = originalScale;
        Vector2 pos = originalPos;
        Quaternion rot = originalRot;

        switch (s)
        {
            case PopStyle.BackPop:
                scale = originalScale * Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(p));
                break;

            case PopStyle.Elastic:
                scale = originalScale * Mathf.LerpUnclamped(startScale, 1f, EaseOutElastic(p));
                break;

            case PopStyle.SlideUp:
                pos = Vector2.LerpUnclamped(originalPos + Vector2.down * slideDistance, originalPos, EaseOutBack(p));
                break;

            case PopStyle.SlideDown:
                pos = Vector2.LerpUnclamped(originalPos + Vector2.up * slideDistance, originalPos, EaseOutBack(p));
                break;

            case PopStyle.SlideLeft:
                pos = Vector2.LerpUnclamped(originalPos + Vector2.left * slideDistance, originalPos, EaseOutBack(p));
                break;

            case PopStyle.SlideRight:
                pos = Vector2.LerpUnclamped(originalPos + Vector2.right * slideDistance, originalPos, EaseOutBack(p));
                break;

            case PopStyle.FlipHorizontal:
                scale = new Vector3(originalScale.x * EaseOutBack(p), originalScale.y, originalScale.z);
                break;

            case PopStyle.FlipVertical:
                scale = new Vector3(originalScale.x, originalScale.y * EaseOutBack(p), originalScale.z);
                break;

            case PopStyle.SpinIn:
                float e = EaseOutBack(p);
                scale = originalScale * Mathf.LerpUnclamped(startScale, 1f, e);
                rot = originalRot * Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(spinAngle, 0f, EaseOutCubic(p)));
                break;

            case PopStyle.DropBounce:
                pos = Vector2.LerpUnclamped(originalPos + Vector2.up * slideDistance, originalPos, EaseOutBounce(p));
                break;
        }

        transform.localScale = scale;
        transform.localRotation = rot;
        if (rect != null) rect.anchoredPosition = pos;
    }

    void ResetToOriginal()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        transform.localScale = originalScale;
        transform.localRotation = originalRot;
        if (rect != null) rect.anchoredPosition = originalPos;
    }

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (t < 1f / d1) return n1 * t * t;
        if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
        if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
        t -= 2.625f / d1; return n1 * t * t + 0.984375f;
    }
}