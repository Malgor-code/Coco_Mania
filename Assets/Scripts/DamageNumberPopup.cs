using System.Collections;
using UnityEngine;
using TMPro;

public enum PopupStyle { Damage, Water, Coin }

public class DamageNumberPopup : MonoBehaviour
{
    public TMP_Text label;

    [Header("Movimiento")]
    [Tooltip("Distancia total que sube = riseSpeed * lifetime (sube rapido al inicio y se frena).")]
    public float riseSpeed = 1.3f;
    public float lifetime = 0.9f;
    [Tooltip("Cuanto se desvia hacia un lado al subir (aleatorio, para que no se apilen).")]
    public float driftX = 0.35f;
    public const float DeathStagger = 0.5f;
    [Header("Pop-in (rebote al aparecer)")]
    public float popDuration = 0.18f;
    [Tooltip("Mas alto = mas rebote. 1.7 es el clasico.")]
    public float popOvershoot = 2.2f;

    [Header("Desvanecimiento")]
    [Range(0f, 1f)] public float fadeStart = 0.55f;

    [Header("Balanceo (roll) de la camara")]
    public float wobbleDegrees = 8f;

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color bigColor = new Color(1f, 0.85f, 0.2f);
    public Color waterColor = new Color(0.35f, 0.8f, 1f);

    [Header("Moneda (arcoiris)")]
    public float rainbowSpeed = 1.5f;
    [Tooltip("Diferencia de color entre letras consecutivas.")]
    public float rainbowCharOffset = 0.12f;

    [Header("Escalas")]
    public float bigScale = 1.4f;
    public float waterScale = 0.85f;
    public float coinScale = 1.1f;

    public void Setup(string text, bool big)
    {
        Setup(text, PopupStyle.Damage, big, 0f);
    }

    public void Setup(string text, PopupStyle style, bool big, float delay)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        StartCoroutine(Animate(text, style, big, delay));
    }

    IEnumerator Animate(string text, PopupStyle style, bool big, float delay)
    {
        if (label == null)
        {
            Destroy(gameObject);
            yield break;
        }

        bool rainbow = style == PopupStyle.Coin;

        float scaleMul = 1f;
        Color baseColor = normalColor;
        switch (style)
        {
            case PopupStyle.Damage:
                baseColor = big ? bigColor : normalColor;
                scaleMul = big ? bigScale : 1f;
                break;
            case PopupStyle.Water:
                baseColor = waterColor;
                scaleMul = waterScale;
                break;
            case PopupStyle.Coin:
                baseColor = Color.white;
                scaleMul = coinScale;
                break;
        }

        label.text = text;
        label.color = baseColor;
        transform.localScale = Vector3.zero;
        label.enabled = false;

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        label.enabled = true;

        Vector3 start = transform.position;
        Camera cam = Camera.main;
        float side = Random.Range(-driftX, driftX);
        float phase = Random.value * 10f;
        bool lively = big || style == PopupStyle.Coin;

        float t = 0f;
        while (t < lifetime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / lifetime);
            float rise = 1f - (1f - p) * (1f - p);
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            Vector3 pos = start + Vector3.up * (riseSpeed * lifetime * rise) + right * (side * rise);
            transform.position = pos;
            float pop = t < popDuration ? EaseOutBack(t / popDuration, popOvershoot) : 1f;
            float shrink = p > 0.8f ? Mathf.Lerp(1f, 0.6f, (p - 0.8f) / 0.2f) : 1f;
            float pulse = style == PopupStyle.Coin ? 1f + 0.08f * Mathf.Sin(t * 18f) : 1f;
            transform.localScale = Vector3.one * (scaleMul * pop * shrink * pulse);

            if (cam != null)
            {
                float wobble = lively ? Mathf.Sin(t * 25f + phase) * wobbleDegrees * (1f - p) : 0f;
                transform.rotation = Quaternion.LookRotation(pos - cam.transform.position) * Quaternion.Euler(0f, 0f, wobble);
            }

            float alpha = p < fadeStart ? 1f : 1f - (p - fadeStart) / (1f - fadeStart);

            if (rainbow)
            {
                ApplyRainbow(t, alpha);
            }
            else
            {
                Color c = baseColor;
                c.a = alpha;
                label.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
    public static void SpawnDeathSequence(DamageNumberPopup prefab, Vector3 position,
                                      string damageText, string waterText, string coinText,
                                      bool big = false, float step = DeathStagger)
{
    if (prefab == null) return;

    float delay = 0f;

    if (!string.IsNullOrEmpty(damageText))
    {
        SpawnOne(prefab, position, damageText, PopupStyle.Damage, big, delay);
        delay += step;
    }
    if (!string.IsNullOrEmpty(waterText))
    {
        SpawnOne(prefab, position, waterText, PopupStyle.Water, false, delay);
        delay += step;
    }
    if (!string.IsNullOrEmpty(coinText))
    {
        SpawnOne(prefab, position, coinText, PopupStyle.Coin, false, delay);
    }
}

static void SpawnOne(DamageNumberPopup prefab, Vector3 pos, string text,
                     PopupStyle style, bool big, float delay)
{
    DamageNumberPopup p = Instantiate(prefab, pos, Quaternion.identity);
    p.Setup(text, style, big, delay);
}
    void ApplyRainbow(float time, float alpha)
    {
        label.ForceMeshUpdate();
        TMP_TextInfo info = label.textInfo;
        byte a = (byte)(Mathf.Clamp01(alpha) * 255f);

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            Color32[] colors = info.meshInfo[ch.materialReferenceIndex].colors32;
            int v = ch.vertexIndex;

            float hue = Mathf.Repeat(time * rainbowSpeed + i * rainbowCharOffset, 1f);
            Color32 c = Color.HSVToRGB(hue, 0.85f, 1f);
            c.a = a;

            colors[v] = c;
            colors[v + 1] = c;
            colors[v + 2] = c;
            colors[v + 3] = c;
        }

        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    static float EaseOutBack(float x, float overshoot)
    {
        x = Mathf.Clamp01(x);
        float c3 = overshoot + 1f;
        float m = x - 1f;
        return 1f + c3 * m * m * m + overshoot * m * m;
    }
}