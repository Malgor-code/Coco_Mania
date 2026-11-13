using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CoconutVisuals : MonoBehaviour
{
    [Header("Feedback al golpe")]
    public float hitFeedbackDuration = 0.18f;
    public float squashAmount = 0.2f;
    public Color hitFlashColor = Color.white;
    [Range(0f, 3f)] public float hitFlashEmission = 1.5f;
    [Range(1f, 6f)] public float hitFlashTintBoost = 3f;

    [Header("Parpadeo de vida baja")]
    [Range(0f, 1f)] public float lowHpBlinkThreshold = 0.25f;
    public Color lowHpBlinkColor = Color.red;
    public float lowHpBlinksPerSecond = 4f;

    const string EmissionKeyword = "_EMISSION";
    const string EmissionProperty = "_EmissionColor";

    Renderer rend;
    Material mainMat;
    readonly List<Material> mats = new List<Material>();
    Color restColor = Color.white;
    Vector3 baseScale = Vector3.one;
    float darken, alpha = 1f, lastHpPercent = 1f;
    bool dead;
    Coroutine hitCo, flashCo, blinkCo;

    public void Init(Renderer r)
    {
        rend = r;
        baseScale = transform.localScale;
        if (rend == null) return;

        foreach (var m in rend.materials)
        {
            m.EnableKeyword(EmissionKeyword);
            mats.Add(m);
        }
        mainMat = mats.Count > 0 ? mats[0] : null;
        if (mainMat != null) { restColor = mainMat.color; restColor.a = 1f; }
        SetEmission(0f);
    }

    Color RestTint()
    {
        Color c = Color.Lerp(restColor, Color.black, darken);
        c.a = alpha;
        return c;
    }

    void SetColor(Color c) { if (mainMat != null) mainMat.color = c; }

    void SetEmission(float t)
    {
        Color e = hitFlashColor * (hitFlashEmission * t);
        foreach (var m in mats)
            if (m != null && m.HasProperty(EmissionProperty)) m.SetColor(EmissionProperty, e);
    }

    void ApplyRest()
    {
        if (hitCo == null && flashCo == null) SetColor(RestTint());
    }

    public void SetBaseScale(Vector3 s) { baseScale = s; transform.localScale = s; }
    public void SetDarken(float t) { darken = Mathf.Clamp01(t); ApplyRest(); }
    public void SetAlpha(float a) { alpha = Mathf.Clamp01(a); ApplyRest(); }
    public void HideRenderer() { if (rend != null) rend.enabled = false; }

    public void ResetState()
    {
        StopAll();
        dead = false;
        darken = 0f;
        alpha = 1f;
        transform.localScale = baseScale;
        SetEmission(0f);
        SetColor(RestTint());
        if (rend != null) rend.enabled = true;
    }

    public void SetDead() { dead = true; StopAll(); }

    void StopAll()
    {
        if (hitCo != null) StopCoroutine(hitCo);
        if (flashCo != null) StopCoroutine(flashCo);
        if (blinkCo != null) StopCoroutine(blinkCo);
        hitCo = flashCo = blinkCo = null;
    }

    public void PlayHit(float hpPercent)
    {
        if (dead) return;
        lastHpPercent = hpPercent;
        if (hitCo != null) StopCoroutine(hitCo);
        if (blinkCo != null) { StopCoroutine(blinkCo); blinkCo = null; }
        hitCo = StartCoroutine(HitRoutine());
    }

    public void Flash(Color color, float duration)
    {
        if (dead || mainMat == null) return;
        if (flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(FlashRoutine(color, duration));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        SetColor(color);
        yield return new WaitForSeconds(duration);
        flashCo = null;
        if (!dead) ApplyRest();
    }

    IEnumerator HitRoutine()
    {
        Color flashTint = hitFlashColor * hitFlashTintBoost;
        flashTint.a = 1f;
        SetEmission(1f);
        SetColor(flashTint);
        transform.localScale = baseScale * (1f - squashAmount);

        for (float t = 0f; t < hitFeedbackDuration; t += Time.deltaTime)
        {
            float p = Mathf.Clamp01(t / hitFeedbackDuration);
            SetEmission(1f - p);
            transform.localScale = baseScale * Mathf.LerpUnclamped(1f - squashAmount, 1f, EaseOutBack(p));
            SetColor(Color.Lerp(flashTint, RestTint(), p));
            yield return null;
        }

        transform.localScale = baseScale;
        SetEmission(0f);
        SetColor(RestTint());
        hitCo = null;

        if (!dead && lastHpPercent <= lowHpBlinkThreshold)
            blinkCo = StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        float half = lowHpBlinksPerSecond > 0f ? 1f / (lowHpBlinksPerSecond * 2f) : 0.15f;
        bool on = false;
        while (!dead)
        {
            on = !on;
            if (flashCo == null) SetColor(on ? lowHpBlinkColor : RestTint());
            yield return new WaitForSeconds(half);
        }
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    void OnDestroy()
    {
        foreach (var m in mats) if (m != null) Destroy(m);
    }
}