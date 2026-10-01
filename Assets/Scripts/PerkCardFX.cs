using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PerkCardFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public Image icon;  
    public Image glow;  
    public Image flash;   

    [Header("Aparicion")]
    public float appearDuration = 0.45f;
    public float startYOffset = -180f;
    public float startRotation = 12f;

    [Header("Hover")]
    public float hoverScale = 1.1f;
    public float hoverTilt = 3f;
    public float hoverSpeed = 14f;

    [Header("Icono")]
    public float bobAmount = 6f;
    public float bobSpeed = 2.5f;

    [Header("Audio (opcional)")]
    public AudioClip appearSfx;
    public AudioClip hoverSfx;
    public AudioClip selectSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    Vector3 baseCardScale = Vector3.one;
    Vector3 baseIconScale = Vector3.one;
    RectTransform rt;
    CanvasGroup group;
    Vector2 basePos;
    Vector2 iconBasePos;
    float appearScale = 1f;
    float appearRot = 0f;
    float hoverT = 0f;
    bool hovering = false;
    bool locked = false;
    float seed;

    void Awake()
    {
        rt = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        seed = Random.value * 10f;

        baseCardScale = rt.localScale;                    
        if (icon != null)
        {
            iconBasePos = icon.rectTransform.anchoredPosition;
            baseIconScale = icon.rectTransform.localScale;  
        }
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float target = (hovering && !locked) ? 1f : 0f;
        hoverT = Mathf.MoveTowards(hoverT, target, hoverSpeed * dt);
        float smooth = hoverT * hoverT * (3f - 2f * hoverT);
        float s = appearScale * Mathf.Lerp(1f, hoverScale, smooth);
        rt.localScale = baseCardScale * s;
        rt.localRotation = Quaternion.Euler(0f, 0f, appearRot + Mathf.Sin(Time.unscaledTime * 6f + seed) * hoverTilt * smooth);

        if (icon != null)
        {
            float bob = Mathf.Sin(Time.unscaledTime * bobSpeed + seed) * bobAmount;
            icon.rectTransform.anchoredPosition = iconBasePos + new Vector2(0f, bob);
            float iconS = 1f + 0.12f * smooth;
            icon.rectTransform.localScale = baseIconScale * iconS;
        }

        if (glow != null)
        {
            Color c = glow.color;
            c.a = Mathf.Lerp(0.15f, 0.9f, smooth) * (0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f + seed));
            glow.color = c;
        }
    }
    public void PlayAppear(float delay)
    {
        StopAllCoroutines();
        locked = false;
        hovering = false;
        StartCoroutine(AppearRoutine(delay));
    }

    public float PlaySelected()
    {
        StopAllCoroutines();
        locked = true;
        StartCoroutine(SelectedRoutine());
        return 0.45f;
    }

    public void PlayDismiss()
    {
        StopAllCoroutines();
        locked = true;
        StartCoroutine(DismissRoutine());
    }
    public void OnPointerEnter(PointerEventData e)
    {
        if (locked) return;
        hovering = true;
        Play(hoverSfx);
    }

    public void OnPointerExit(PointerEventData e) { hovering = false; }

    void OnDisable() { hovering = false; hoverT = 0f; }
    IEnumerator AppearRoutine(float delay)
    {
        basePos = rt.anchoredPosition.y < -5000f ? basePos : GetRestPos();
        appearScale = 0f;
        appearRot = startRotation * (Random.value < 0.5f ? -1f : 1f);
        group.alpha = 0f;
        group.interactable = false;
        rt.anchoredPosition = basePos + new Vector2(0f, startYOffset);

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        Play(appearSfx);
        float startRot = appearRot;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / appearDuration;
            float k = Mathf.Clamp01(t);
            float e = EaseOutBack(k);
            appearScale = e;
            appearRot = Mathf.Lerp(startRot, 0f, EaseOutBack(k));
            group.alpha = Mathf.Clamp01(k * 3f);
            rt.anchoredPosition = Vector2.LerpUnclamped(basePos + new Vector2(0f, startYOffset), basePos, e);
            yield return null;
        }
        appearScale = 1f; appearRot = 0f; group.alpha = 1f;
        rt.anchoredPosition = basePos;
        group.interactable = true;
    }

    IEnumerator SelectedRoutine()
    {
        Play(selectSfx);
        group.interactable = false;
        float t = 0f;
        float dur = 0.45f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = Mathf.Clamp01(t);
            float punch = k < 0.3f ? Mathf.Lerp(1f, 1.3f, k / 0.3f)
                                   : Mathf.Lerp(1.3f, 0f, EaseInCubic((k - 0.3f) / 0.7f));
            appearScale = punch;
            appearRot = Mathf.Sin(k * Mathf.PI * 4f) * 6f * (1f - k);
            if (flash != null)
            {
                Color c = flash.color; c.a = Mathf.Clamp01(1f - k * 2.5f); flash.color = c;
            }
            group.alpha = k < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.6f) / 0.4f);
            yield return null;
        }
        appearScale = 0f;
    }

    IEnumerator DismissRoutine()
    {
        group.interactable = false;
        float t = 0f, dur = 0.3f;
        float startRot = appearRot;
        Vector2 from = rt.anchoredPosition;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = EaseInCubic(Mathf.Clamp01(t));
            appearScale = Mathf.Lerp(1f, 0.6f, k);
            appearRot = Mathf.Lerp(startRot, startRot + 10f, k);
            group.alpha = 1f - k;
            rt.anchoredPosition = from + new Vector2(0f, -60f * k);
            yield return null;
        }
        group.alpha = 0f;
    }
    Vector2 restPos; bool restSaved;
    Vector2 GetRestPos()
    {
        if (!restSaved) { restPos = rt.anchoredPosition; restSaved = true; }
        return restPos;
    }

    void Play(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, pos, sfxVolume);
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    static float EaseInCubic(float x) => x * x * x;
}