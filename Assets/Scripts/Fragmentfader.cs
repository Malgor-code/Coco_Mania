using System.Collections.Generic;
using UnityEngine;

public class FragmentFader : MonoBehaviour
{
    float delay;
    float duration;
    float shrinkTo = 1f;
    float timer;
    bool started;
    bool armed;
    Vector3 startScale;

    readonly List<Material> mats = new List<Material>();
    readonly List<Color> baseColors = new List<Color>();
    readonly List<string> colorProps = new List<string>();
    public static void Attach(GameObject go, float lifetime, float fadeDuration, float shrinkTo)
    {
        var f = go.GetComponent<FragmentFader>();
        if (f == null) f = go.AddComponent<FragmentFader>();
        f.Arm(lifetime, fadeDuration, shrinkTo);
    }

    public void Arm(float lifetime, float fadeDuration, float shrinkTo)
    {
        duration = Mathf.Max(0.05f, fadeDuration);
        delay = Mathf.Max(0f, lifetime - duration);
        this.shrinkTo = Mathf.Clamp01(shrinkTo);
        timer = 0f;
        started = false;
        armed = true;
    }

    void Update()
    {
        if (!armed) return;
        timer += Time.deltaTime;
        if (timer < delay) return;

        if (!started) BeginFade();

        float t = Mathf.Clamp01((timer - delay) / duration);

        for (int i = 0; i < mats.Count; i++)
        {
            if (mats[i] == null) continue;
            Color c = baseColors[i];
            c.a = baseColors[i].a * (1f - t);
            mats[i].SetColor(colorProps[i], c);
        }

        transform.localScale = startScale * Mathf.Lerp(1f, shrinkTo, t);

        if (t >= 1f) Destroy(gameObject);
    }

    void BeginFade()
    {
        started = true;
        startScale = transform.localScale;

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            foreach (var m in r.materials)
            {
                string prop = m.HasProperty("_BaseColor") ? "_BaseColor" : (m.HasProperty("_Color") ? "_Color" : null);
                if (prop == null) continue;

                MakeTransparent(m);
                mats.Add(m);
                colorProps.Add(prop);
                baseColors.Add(m.GetColor(prop));
            }
        }
    }

    static void MakeTransparent(Material m)
    {
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (m.HasProperty("_Mode"))
        {
            m.SetFloat("_Mode", 2f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    void OnDestroy()
    {
        foreach (var m in mats) if (m != null) Destroy(m);
    }
}