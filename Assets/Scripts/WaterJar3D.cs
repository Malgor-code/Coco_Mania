using UnityEngine;
using System.Collections.Generic;

// Un jarron individual. Solo dibuja el liquido, la superficie animada y el
// "punch" al recibir agua - no sabe nada de GameManager ni de otros jarrones.
// Eso lo maneja WaterJarGroup, que le va diciendo cuanto mostrar (SetFillTarget)
// y cuando le llego agua (Arrive).
public class WaterJar3D : MonoBehaviour
{
    [Header("Liquido (hijo del jarron)")]
    [Tooltip("Objeto hijo del jarron (cilindro/cubo/malla) que representa el liquido. Su altura crece de 0 a 100% segun el fill que le manda WaterJarGroup. NO puede ser el mismo objeto que tiene este script.")]
    public Transform liquid;
    [Tooltip("Altura (eje Y, espacio local del PADRE del liquido) de la base del liquido cuando esta vacio.")]
    public float liquidBottomLocalY = 0f;
    [Tooltip("Cuanta altura ocupa el liquido cuando este jarron esta al 100%.")]
    public float liquidFullHeight = 1f;
    [Tooltip("Altura de la malla con escala Y = 1. Cube = 1, Cylinder de Unity = 2.")]
    public float meshHeightAtScale1 = 1f;
    [Tooltip("Activalo si el pivote de tu malla esta en la BASE. Si el pivote esta en el centro (Cube/Cylinder de Unity), dejalo apagado.")]
    public bool pivotAtBottom = false;
    [Tooltip("Que tan rapido sube/baja el nivel visual, en fracciones del jarron por segundo.")]
    public float levelSpeed = 3f;

    [Header("Superficie de agua en movimiento (vista desde arriba)")]
    public bool animateSurface = true;
    [Tooltip("Material de la superficie. Si esta vacio se copia el del liquido.")]
    public Material surfaceMaterial;
    [Tooltip("Radio de la superficie en unidades locales del padre del liquido. 0 = se calcula solo desde la malla del liquido.")]
    public float surfaceRadius = 0f;
    public int surfaceRings = 14;
    public int surfaceSegments = 40;
    [Tooltip("Altura del oleaje continuo, como fraccion del radio.")]
    [Range(0f, 0.3f)] public float waveHeight = 0.06f;
    public float waveFrequency = 5f;
    public float waveSpeed = 1.6f;
    [Tooltip("Altura de la onda que nace cuando llega una gota/particula, como fraccion del radio.")]
    [Range(0f, 0.5f)] public float rippleHeight = 0.14f;
    public float rippleSpeed = 1.6f;
    public float rippleLife = 1.3f;
    public float rippleWidth = 0.15f;
    public float rippleFrequency = 22f;
    public int maxRipples = 16;

    [Header("Visibilidad del agua")]
    [Tooltip("Fuerza un color/alpha propios en el material del liquido y la superficie (crea una copia del material - no toca el original). Apagalo si preferis dejar tu material tal cual.")]
    public bool forceLiquidColor = true;
    public Color liquidColor = new Color(0.05f, 0.55f, 0.95f, 0.95f);
    public Color surfaceColor = new Color(0.35f, 0.85f, 1f, 1f);
    [Tooltip("Brillo emisivo extra (si tu shader soporta Emission) para que el agua se note aunque la escena este oscura.")]
    [Range(0f, 1f)] public float emissionIntensity = 0.35f;

    [Header("Boca del jarron (a donde apuntan las gotas/particulas)")]
    public Transform dropletTarget;

    [Header("Feedback al recibir agua")]
    public float punchScale = 0.05f;
    public float punchRecoverSpeed = 8f;

    private float fillTarget01 = 0f;
    private float displayed01 = 0f;
    private float punch = 0f;
    private Vector3 baseScale;

    // Superficie
    private struct Ripple { public Vector2 center; public float startTime; }
    private readonly List<Ripple> ripples = new List<Ripple>();
    private GameObject surfaceGO;
    private Mesh surfaceMesh;
    private Vector3[] surfaceVerts;
    private Vector2[] baseXZ;
    private float[] baseR;
    private float surfaceRadiusResolved = 0.5f;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Start()
    {
        if (animateSurface && liquid != null) BuildSurface();
        ApplyVisibility();
        ApplyLiquid(0f);
    }

    void Update()
    {
        displayed01 = Mathf.MoveTowards(displayed01, fillTarget01, levelSpeed * Time.deltaTime);
        ApplyLiquid(displayed01);

        punch = Mathf.Lerp(punch, 0f, punchRecoverSpeed * Time.deltaTime);
        transform.localScale = baseScale * (1f + punch);
    }

    // ------------------------------------------------------------- API

    public void SetFillTarget(float fill01)
    {
        fillTarget01 = Mathf.Clamp01(fill01);
    }

    // Salta directo, sin animar (para el reset de dia nuevo).
    public void SetFillImmediate(float fill01)
    {
        fillTarget01 = Mathf.Clamp01(fill01);
        displayed01 = fillTarget01;
        ripples.Clear();
    }

    public Vector3 TargetPosition()
    {
        return dropletTarget != null ? dropletTarget.position : transform.position + Vector3.up;
    }

    public void Arrive(Vector3 worldPos)
    {
        punch = Mathf.Min(punch + punchScale, punchScale * 3f);
        AddRipple(worldPos);
    }

    // ---------------------------------------------------------- Liquido

    void ApplyLiquid(float fill01)
    {
        if (liquid == null) return;

        bool show = fill01 > 0.002f;
        if (liquid.gameObject.activeSelf != show) liquid.gameObject.SetActive(show);
        if (!show)
        {
            UpdateSurface(fill01, 0f);
            return;
        }

        float height = liquidFullHeight * fill01;

        // La tapa del cilindro queda un poco por debajo de la superficie animada,
        // asi las olas nunca se hunden dentro de la malla del liquido.
        float dip = (animateSurface && surfaceGO != null)
            ? (waveHeight * 1.1f + rippleHeight * 1.5f) * surfaceRadiusResolved
            : 0f;
        float liquidHeight = Mathf.Max(0.0005f, height - dip);

        Vector3 s = liquid.localScale;
        s.y = liquidHeight / Mathf.Max(0.0001f, meshHeightAtScale1);
        liquid.localScale = s;

        Vector3 p = liquid.localPosition;
        p.y = pivotAtBottom ? liquidBottomLocalY : liquidBottomLocalY + liquidHeight * 0.5f;
        liquid.localPosition = p;

        UpdateSurface(fill01, liquidBottomLocalY + height);
    }

    // -------------------------------------------------------- Superficie

    void BuildSurface()
    {
        Transform parent = liquid.parent != null ? liquid.parent : transform;

        float radius = surfaceRadius;
        if (radius <= 0f)
        {
            MeshFilter lmf = liquid.GetComponent<MeshFilter>();
            if (lmf != null && lmf.sharedMesh != null)
            {
                Vector3 ext = lmf.sharedMesh.bounds.extents;
                Vector3 sc = liquid.localScale;
                radius = Mathf.Min(ext.x * Mathf.Abs(sc.x), ext.z * Mathf.Abs(sc.z)) * 0.98f;
            }
            else radius = 0.5f;
        }
        surfaceRadiusResolved = radius;

        surfaceGO = new GameObject("WaterSurface");
        surfaceGO.transform.SetParent(parent, false);
        surfaceGO.transform.localScale = Vector3.one * radius;
        surfaceGO.layer = liquid.gameObject.layer;

        MeshFilter mf = surfaceGO.AddComponent<MeshFilter>();
        MeshRenderer mr = surfaceGO.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Material mat = surfaceMaterial;
        if (mat == null)
        {
            Renderer lr = liquid.GetComponent<Renderer>();
            mat = new Material(lr != null && lr.sharedMaterial != null ? lr.sharedMaterial : GetFallbackShaderMaterial());
        }
        else
        {
            mat = new Material(mat);
        }
        mr.material = mat;
        if (forceLiquidColor) TintMaterial(mat, surfaceColor);

        int rings = Mathf.Max(3, surfaceRings);
        int segs = Mathf.Max(8, surfaceSegments);
        int vCount = 1 + rings * segs;

        surfaceVerts = new Vector3[vCount];
        baseXZ = new Vector2[vCount];
        baseR = new float[vCount];
        Vector2[] uvs = new Vector2[vCount];

        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int ring = 1; ring <= rings; ring++)
        {
            float rr = ring / (float)rings;
            for (int s = 0; s < segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * rr;
                float z = Mathf.Sin(a) * rr;
                int idx = 1 + (ring - 1) * segs + s;
                surfaceVerts[idx] = new Vector3(x, 0f, z);
                baseXZ[idx] = new Vector2(x, z);
                baseR[idx] = rr;
                uvs[idx] = new Vector2(0.5f + x * 0.5f, 0.5f + z * 0.5f);
            }
        }

        List<int> tris = new List<int>();
        for (int s = 0; s < segs; s++)
        {
            int i1 = 1 + s;
            int i2 = 1 + ((s + 1) % segs);
            tris.Add(0); tris.Add(i2); tris.Add(i1);
        }
        for (int ring = 1; ring < rings; ring++)
        {
            for (int s = 0; s < segs; s++)
            {
                int a = 1 + (ring - 1) * segs + s;
                int b = 1 + (ring - 1) * segs + ((s + 1) % segs);
                int c = 1 + ring * segs + s;
                int d = 1 + ring * segs + ((s + 1) % segs);
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }
        }

        surfaceMesh = new Mesh { name = "WaterSurfaceMesh" };
        surfaceMesh.MarkDynamic();
        surfaceMesh.vertices = surfaceVerts;
        surfaceMesh.uv = uvs;
        surfaceMesh.triangles = tris.ToArray();
        surfaceMesh.RecalculateNormals();
        surfaceMesh.RecalculateBounds();
        mf.sharedMesh = surfaceMesh;

        surfaceGO.SetActive(false);
    }

    void UpdateSurface(float fill01, float topLocalY)
    {
        if (surfaceGO == null) return;

        bool show = fill01 > 0.002f;
        if (surfaceGO.activeSelf != show) surfaceGO.SetActive(show);
        if (!show) return;

        Vector3 lp = liquid.localPosition;
        surfaceGO.transform.localPosition = new Vector3(lp.x, topLocalY, lp.z);

        float t = Time.time;

        for (int i = ripples.Count - 1; i >= 0; i--)
        {
            if (t - ripples[i].startTime > rippleLife) ripples.RemoveAt(i);
        }

        for (int i = 0; i < surfaceVerts.Length; i++)
        {
            float x = baseXZ[i].x;
            float z = baseXZ[i].y;
            float r = baseR[i];
            float w = Mathf.Sin((x + z * 0.6f) * waveFrequency + t * waveSpeed) * 0.5f
                    + Mathf.Sin((z - x * 0.7f) * waveFrequency * 1.3f - t * waveSpeed * 1.2f) * 0.35f
                    + Mathf.Sin(r * waveFrequency * 0.8f - t * waveSpeed * 0.9f) * 0.25f;
            float y = w * waveHeight;
            for (int k = 0; k < ripples.Count; k++)
            {
                Ripple rp = ripples[k];
                float age = t - rp.startTime;
                float dx = x - rp.center.x;
                float dz = z - rp.center.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float delta = d - age * rippleSpeed;
                float ring = Mathf.Exp(-(delta * delta) / (2f * rippleWidth * rippleWidth));
                float fade = 1f - age / rippleLife;
                y += rippleHeight * fade * fade * ring * Mathf.Cos(delta * rippleFrequency);
            }
            float env = 1f - Mathf.SmoothStep(0.75f, 1f, r);
            surfaceVerts[i].y = y * env;
        }

        surfaceMesh.vertices = surfaceVerts;
        surfaceMesh.RecalculateNormals();
        surfaceMesh.RecalculateBounds();
    }

    void AddRipple(Vector3 worldPos)
    {
        if (surfaceGO == null) return;

        Vector3 lp = surfaceGO.transform.InverseTransformPoint(worldPos);
        Vector2 c = new Vector2(lp.x, lp.z);
        if (c.magnitude > 0.9f) c = c.normalized * 0.9f;

        if (ripples.Count >= Mathf.Max(1, maxRipples)) ripples.RemoveAt(0);
        ripples.Add(new Ripple { center = c, startTime = Time.time });
    }

    void ApplyVisibility()
    {
        if (!forceLiquidColor || liquid == null) return;

        Renderer lr = liquid.GetComponent<Renderer>();
        if (lr != null) TintMaterial(lr.material, liquidColor);
    }

    void TintMaterial(Material m, Color c)
    {
        if (m == null) return;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);

        if (emissionIntensity > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * emissionIntensity);
        }
    }

    Material GetFallbackShaderMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        return new Material(sh);
    }
}