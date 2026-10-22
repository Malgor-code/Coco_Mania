using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameFX
{
    public static event System.Action<string> Sfx;
    public static void PlaySfx(string id) { if (Sfx != null) Sfx(id); }

    public static float EaseOutCubic(float k) { k = Mathf.Clamp01(k); float u = 1f - k; return 1f - u * u * u; }
    public static float EaseInCubic(float k) { k = Mathf.Clamp01(k); return k * k * k; }
    public static float EaseOutBack(float k)
    {
        k = Mathf.Clamp01(k);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = k - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
    public static float Punch(float k) { k = Mathf.Clamp01(k); return Mathf.Sin(k * Mathf.PI * 4f) * (1f - k); }

    public static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static MaterialPropertyBlock tintBlock;
    public static void Tint(GameObject go, Color c)
    {
        if (tintBlock == null) tintBlock = new MaterialPropertyBlock();
        tintBlock.Clear();
        tintBlock.SetColor("_BaseColor", c);
        tintBlock.SetColor("_Color", c);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(tintBlock);
    }

    public static void ClearTint(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(null);
    }

    public static void ProjectExtents(Bounds b, Vector3 origin, Vector3 axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        Vector3 c = b.center, e = b.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            float d = Vector3.Dot(corner - origin, axis);
            if (d < min) min = d;
            if (d > max) max = d;
        }
    }

    public static bool WorldToUI(RectTransform root, Camera cam, Vector3 world, out Vector2 local)
    {
        local = Vector2.zero;
        if (cam == null || root == null) return false;
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z <= 0f) return false;
        Camera uiCam = null;
        var canvas = root.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) uiCam = canvas.worldCamera;
        }
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(sp.x, sp.y), uiCam, out local);
    }

    static readonly HashSet<string> logged = new HashSet<string>();

    public static GameObject Prep(GameObject go, Vector3 pos, Quaternion rot, string label)
    {
        if (go == null) return go;
        Transform t = go.transform;
        if ((t.position - pos).sqrMagnitude > 1e-4f && logged.Add("pos:" + label))
            Debug.LogWarning("MinigameFX: Spawn de '" + label + "' NO respeto la posicion pedida (pidio " + pos + ", quedo en " +
                             t.position + ", padre: " + (t.parent != null ? t.parent.name : "ninguno") + "). Se corrige.");
        t.SetPositionAndRotation(pos, rot);

        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || !mb.enabled) continue;
            if (logged.Add("script:" + label + ":" + mb.GetType().Name))
                Debug.Log("MinigameFX: se desactiva el script " + mb.GetType().Name + " de '" + label + "' para que no mueva el objeto.");
            mb.enabled = false;
        }
        foreach (var b in go.GetComponentsInChildren<Behaviour>(true))
            if (b != null && b.enabled && b.GetType().Name == "NavMeshAgent") b.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        return go;
    }

    class P
    {
        public Transform tr;
        public Renderer rend;
        public Vector3 vel;
        public float life, max, size, gravity;
    }

    class T
    {
        public TMP_Text txt;
        public Vector2 start;
        public Color col;
        public float t, dur, rise;
    }

    static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.35f, 0.35f), new Color(1f, 0.85f, 0.2f), new Color(0.4f, 0.9f, 0.4f),
        new Color(0.35f, 0.7f, 1f), new Color(0.9f, 0.45f, 1f), Color.white
    };

    static Material sharedMat;
    static Mesh sphereMesh;
    static MaterialPropertyBlock mpb;

    readonly List<P> live = new List<P>();
    readonly Stack<P> pool = new Stack<P>();
    readonly List<T> texts = new List<T>();
    GameObject holder;
    Image flashImg;
    Color flashCol;
    float flashT, flashDur, freeze;

    static Material GetMat()
    {
        if (sharedMat != null) return sharedMat;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        sharedMat = new Material(sh);
        return sharedMat;
    }

    static Mesh GetSphere()
    {
        if (sphereMesh != null) return sphereMesh;
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereMesh = g.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(g);
        return sphereMesh;
    }

    P Take()
    {
        if (live.Count > 400) return null;
        P p = pool.Count > 0 ? pool.Pop() : null;
        if (p == null || p.tr == null)
        {
            if (holder == null) holder = new GameObject("MinigameFX");
            var go = new GameObject("fx");
            go.transform.SetParent(holder.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetSphere();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GetMat();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            p = new P { tr = go.transform, rend = mr };
        }
        p.tr.gameObject.SetActive(true);
        return p;
    }

    void Emit(Vector3 pos, Color col, Vector3 vel, float size, float life, float gravity)
    {
        P p = Take();
        if (p == null) return;
        p.tr.position = pos;
        p.vel = vel;
        p.life = p.max = Mathf.Max(0.05f, life);
        p.size = size;
        p.gravity = gravity;
        p.tr.localScale = Vector3.one * size;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        mpb.Clear();
        mpb.SetColor("_Color", col);
        mpb.SetColor("_BaseColor", col);
        p.rend.SetPropertyBlock(mpb);
        live.Add(p);
    }

    public void Burst(Vector3 pos, Color col, int count, float unit, float speedK = 4f, float sizeK = 0.12f,
                      float life = 0.7f, float gravK = 9f, Vector3 bias = default(Vector3), float spread = 1f)
    {
        unit = Mathf.Max(0.01f, unit);
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere * spread + bias;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
            Vector3 v = dir.normalized * (unit * speedK * Random.Range(0.4f, 1f));
            Emit(pos, col, v, unit * sizeK * Random.Range(0.6f, 1.2f), life * Random.Range(0.7f, 1.2f), unit * gravK);
        }
    }

    public void Confetti(Vector3 pos, float unit, int count = 40)
    {
        unit = Mathf.Max(0.01f, unit);
        for (int i = 0; i < count; i++)
        {
            Vector3 v = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 1.6f), Random.Range(-0.6f, 0.6f)) * (unit * 4f);
            Emit(pos, ConfettiColors[Random.Range(0, ConfettiColors.Length)], v,
                 unit * 0.1f * Random.Range(0.7f, 1.3f), Random.Range(1.0f, 1.6f), unit * 6f);
        }
    }

    public void Pop(RectTransform root, Vector2 pos, string text, Color col, int size = 48, float rise = 90f, float dur = 0.8f)
    {
        if (root == null) return;
        var t = MiniUI.Label(root, text, size, pos, new Vector2(700, 140), col);
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        t.transform.localScale = Vector3.zero;
        texts.Add(new T { txt = t, start = pos, col = col, t = 0f, dur = Mathf.Max(0.1f, dur), rise = rise });
    }

    public void Flash(RectTransform root, Color c, float dur = 0.2f)
    {
        if (root == null) return;
        if (flashImg == null)
        {
            flashImg = MiniUI.Img("Flash", root, c, Vector2.zero, new Vector2(4000f, 4000f));
            flashImg.raycastTarget = false;
        }
        flashImg.transform.SetAsLastSibling();
        flashCol = c;
        flashDur = Mathf.Max(0.05f, dur);
        flashT = flashDur;
        flashImg.color = c;
    }

    public void HitStop(float seconds) { freeze = Mathf.Max(freeze, seconds); }

    public float Gate(float dt)
    {
        if (freeze > 0f) { freeze -= dt; return 0f; }
        return dt;
    }

    public void Tick(float dt)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            P p = live[i];
            p.life -= dt;
            if (p.life <= 0f || p.tr == null)
            {
                if (p.tr != null) { p.tr.gameObject.SetActive(false); pool.Push(p); }
                live.RemoveAt(i);
                continue;
            }
            p.vel += Vector3.down * (p.gravity * dt);
            p.tr.position += p.vel * dt;
            float k = p.life / p.max;
            p.tr.localScale = Vector3.one * (p.size * (k < 0.35f ? k / 0.35f : 1f));
        }

        for (int i = texts.Count - 1; i >= 0; i--)
        {
            T t = texts[i];
            if (t.txt == null) { texts.RemoveAt(i); continue; }
            t.t += dt;
            float k = t.t / t.dur;
            if (k >= 1f) { Object.Destroy(t.txt.gameObject); texts.RemoveAt(i); continue; }
            float s = EaseOutBack(Mathf.Min(1f, k * 5f)) * (1f + 0.08f * Punch(Mathf.Min(1f, k * 3f)));
            t.txt.transform.localScale = Vector3.one * s;
            t.txt.rectTransform.anchoredPosition = t.start + Vector2.up * (t.rise * EaseOutCubic(k));
            Color c = t.col;
            c.a = k < 0.6f ? t.col.a : t.col.a * (1f - (k - 0.6f) / 0.4f);
            t.txt.color = c;
        }

        if (flashImg != null)
        {
            flashT -= dt;
            Color c = flashCol;
            c.a = flashT > 0f ? flashCol.a * (flashT / flashDur) : 0f;
            flashImg.color = c;
        }
    }

    public void Clear()
    {
        if (holder != null) Object.Destroy(holder);
        holder = null;
        live.Clear();
        pool.Clear();
        foreach (var t in texts) if (t.txt != null) Object.Destroy(t.txt.gameObject);
        texts.Clear();
        if (flashImg != null) Object.Destroy(flashImg.gameObject);
        flashImg = null;
        freeze = 0f;
        flashT = 0f;
    }
}