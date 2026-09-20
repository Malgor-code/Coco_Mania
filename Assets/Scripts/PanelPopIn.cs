using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class PanelPopIn : MonoBehaviour
{
    public float duration = 0.18f;
    public float startScale = 0.85f;

    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Coroutine routine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        float t = 0f;
        transform.localScale = originalScale * startScale;
        canvasGroup.alpha = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);

            transform.localScale = Vector3.LerpUnclamped(originalScale * startScale, originalScale, eased);
            canvasGroup.alpha = eased;

            yield return null;
        }

        transform.localScale = originalScale;
        canvasGroup.alpha = 1f;
    }
}