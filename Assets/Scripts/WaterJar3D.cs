using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Jarron 3D que se llena con el progreso del pedido actual.
// - Las particulas de muerte del coco (Death Particles) vuelan hasta la boca del jarron
//   (ver JarParticleAttractor) y el nivel sube cuando cada particula LLEGA.
// - La superficie del agua (vista desde arriba) tiene oleaje continuo y ondas
//   que nacen donde cae cada particula.
public class WaterJar3D : MonoBehaviour
{
    public static WaterJar3D Instance;

    [Header("Liquido (hijo del jarron)")]
    [Tooltip("Objeto hijo del jarron (cilindro/cubo/malla) que representa el liquido. Su altura crece de 0 a 100% segun el progreso del pedido. NO puede ser el mismo objeto que tiene este script.")]
    public Transform liquid;
    [Tooltip("Altura (eje Y, espacio local del PADRE del liquido) de la base del liquido cuando esta vacio.")]
    public float liquidBottomLocalY = 0f;
    [Tooltip("Cuanta altura ocupa el liquido cuando el pedido esta al 100% (mismas unidades locales).")]
    public float liquidFullHeight = 1f;
    [Tooltip("Altura de la malla con escala Y = 1. Cube = 1, Cylinder de Unity = 2.")]
    public float meshHeightAtScale1 = 1f;
    [Tooltip("Activalo si el pivote de tu malla esta en la BASE. Si el pivote esta en el centro (Cube/Cylinder de Unity), dejalo apagado.")]
    public bool pivotAtBottom = false;
    [Tooltip("Que tan rapido sube/baja el nivel visual, en fracciones del jarron por segundo.")]
    public float levelSpeed = 1.5f;

    [Header("Superficie de agua en movimiento (vista desde arriba)")]
    public bool animateSurface = true;
    [Tooltip("Material de la superficie. Si esta vacio se copia el material del liquido. Para que las olas se noten desde arriba usa un material con Smoothness alto.")]
    public Material surfaceMaterial;
    [Tooltip("Radio de la superficie en unidades locales del padre del liquido. 0 = se calcula solo a partir de la malla del liquido.")]
    public float surfaceRadius = 0f;
    public int surfaceRings = 14;
    public int surfaceSegments = 40;
    [Tooltip("Altura del oleaje continuo, como fraccion del radio.")]
    [Range(0f, 0.2f)] public float waveHeight = 0.04f;
    [Tooltip("Cuantas olas caben en el jarron (mas alto = olas mas chicas).")]
    public float waveFrequency = 5f;
    public float waveSpeed = 1.6f;
    [Tooltip("Altura de la onda que nace cuando cae una particula, como fraccion del radio.")]
    [Range(0f, 0.4f)] public float rippleHeight = 0.10f;
    [Tooltip("Velocidad con la que se expande la onda (radios por segundo).")]
    public float rippleSpeed = 1.6f;
    public float rippleLife = 1.3f;
    public float rippleWidth = 0.15f;
    public float rippleFrequency = 22f;
    public int maxRipples = 16;

    [Header("Particulas de los cocos (Death Particles) -> jarron")]
    [Tooltip("Las particulas hacen su animacion normal este tiempo y despues vuelan al jarron.")]
    public float particleAttractDelay = 0.35f;
    public float particleMinSpeed = 2f;
    public float particleMaxSpeed = 14f;
    [Tooltip("Tiempo que tardan en pasar de la velocidad minima a la maxima.")]
    public float particleAccelTime = 0.6f;
    [Tooltip("Que tan rapido giran hacia el jarron (mas alto = trayectoria mas directa).")]
    public float particleSteering = 10f;
    public float particleArriveDistance = 0.25f;
    [Tooltip("Seguro: si algo sale mal, el efecto se elimina solo pasado este tiempo.")]
    public float particleMaxLifetime = 6f;

    [Header("Destino de las particulas / gotas")]
    [Tooltip("Boca del jarron: hacia donde vuelan. Si esta vacio se usa un punto 1 unidad arriba del jarron.")]
    public Transform dropletTarget;
    [Tooltip("Radio (unidades de mundo) alrededor de la boca donde caen, para que no lleguen todas al mismo punto.")]
    public float landingScatter = 0.3f;

    [Header("Gotas de respaldo (solo si el coco NO tiene Death Particles)")]
    public GameObject dropletPrefab;
    public Color dropletColor = new Color(0.75f, 0.95f, 1f, 1f);
    public float dropletSize = 0.15f;
    public float mlPerDroplet = 15f;
    public int maxDropletsPerKill = 8;
    public float flightDuration = 0.7f;
    public float flightDurationVariance = 0.15f;
    public float spawnDelaySpread = 0.12f;
    public float arcHeight = 2.5f;
    public float spawnScatter = 0.3f;

    [Header("Feedback del jarron al recibir agua")]
    public float punchScale = 0.04f;
    public float punchRecoverSpeed = 8f;

    private float visualML = 0f;
    private float displayed01 = 0f;
    private int inFlight = 0;
    private float punch = 0f;
    private Vector3 baseScale;
    private Material dropletMaterial;

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
        Instance = this;
        baseScale = transform.localScale;
    }

    void Start()
    {
        if (animateSurface && liquid != null) BuildSurface();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            float current = gm.waterCurrentML;
            float target = Mathf.Max(1f, gm.waterTargetML);

            // Si se gasto agua (mejoras / pedido), el jarron baja de inmediato.
            if (visualML > current) visualML = current;

            // Si no hay agua en vuelo, alcanza el valor real (por si algo se perdio).
            if (inFlight <= 0 && visualML < current)
                visualML = Mathf.MoveTowards(visualML, current, target * 2f * Time.deltaTime);

            float target01 = Mathf.Clamp01(visualML / target);
            displayed01 = Mathf.MoveTowards(displayed01, target01, levelSpeed * Time.deltaTime);
        }

        ApplyLiquid(displayed01);

        punch = Mathf.Lerp(punch, 0f, punchRecoverSpeed * Time.deltaTime);
        transform.localScale = baseScale * (1f + punch);
    }

    // ---------------------------------------------------------------- Liquido

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

        // La tapa del cilindro queda un poquito por debajo de la superficie animada,
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

    // -------------------------------------------------------------- Superficie

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
            if (lr != null && lr.sharedMaterial != null) mat = new Material(lr.sharedMaterial);
            else mat = new Material(GetDropletMaterial());
        }
        mr.sharedMaterial = mat;

        // Disco unitario (radio 1) con anillos concentricos; el tamano real lo da la escala.
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

            // Oleaje continuo: tres ondas viajando en direcciones distintas.
            float w = Mathf.Sin((x + z * 0.6f) * waveFrequency + t * waveSpeed) * 0.5f
                    + Mathf.Sin((z - x * 0.7f) * waveFrequency * 1.3f - t * waveSpeed * 1.2f) * 0.35f
                    + Mathf.Sin(r * waveFrequency * 0.8f - t * waveSpeed * 0.9f) * 0.25f;
            float y = w * waveHeight;

            // Ondas expansivas donde cayo cada particula.
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

            // En el borde (pegado al vidrio) el agua queda quieta.
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

    // ----------------------------------------------- API para particulas / gotas

    public Vector3 TargetPosition()
    {
        return dropletTarget != null ? dropletTarget.position : transform.position + Vector3.up;
    }

    // Punto de aterrizaje estable para una particula (segun su semilla), asi cada una
    // llega a un lugar distinto alrededor de la boca del jarron.
    public Vector3 GetLandingPoint(uint seed)
    {
        float a = ((seed & 0xFFFF) / 65535f) * Mathf.PI * 2f;
        float r = Mathf.Sqrt(((seed >> 16) & 0xFFFF) / 65535f) * landingScatter;
        return TargetPosition() + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }

    public void RegisterFlight() { inFlight++; }
    public void UnregisterFlight() { inFlight = Mathf.Max(0, inFlight - 1); }

    // Una particula/gota llego: sube el nivel, el jarron "late" y nace una onda.
    public void NotifyArrival(Vector3 worldPos, float shareML)
    {
        visualML += shareML;
        punch = Mathf.Min(punch + punchScale, punchScale * 3f);
        AddRipple(worldPos);
    }

    // Llamado desde GameManager cuando muere un coco.
    // Devuelve true si el jarron se quedo con el efecto de particulas (el jarron lo destruye
    // solo cuando termina); false si el que llama debe encargarse de destruirlo.
    public bool HandleCoconutWater(Vector3 worldPos, float waterML, GameObject deathFx)
    {
        if (!isActiveAndEnabled) return false;

        if (deathFx != null)
        {
            ParticleSystem[] systems = deathFx.GetComponentsInChildren<ParticleSystem>();
            if (systems.Length > 0)
            {
                JarParticleAttractor attractor = deathFx.AddComponent<JarParticleAttractor>();
                attractor.Init(this, systems, waterML);
                return true;
            }
        }

        SpawnDroplets(worldPos, waterML);
        return false;
    }

    // ------------------------------------------------------- Gotas de respaldo

    public void SpawnDroplets(Vector3 worldPos, float waterML)
    {
        if (!isActiveAndEnabled) return;

        int count = Mathf.Clamp(Mathf.RoundToInt(waterML / Mathf.Max(1f, mlPerDroplet)), 1, Mathf.Max(1, maxDropletsPerKill));
        float share = waterML / count;

        for (int i = 0; i < count; i++)
        {
            inFlight++;
            StartCoroutine(DropletRoutine(worldPos, share));
        }
    }

    IEnumerator DropletRoutine(Vector3 startPos, float shareML)
    {
        if (spawnDelaySpread > 0f) yield return new WaitForSeconds(Random.Range(0f, spawnDelaySpread));

        GameObject d = CreateDroplet(startPos + Random.insideUnitSphere * spawnScatter);
        Vector3 baseDropScale = d.transform.localScale;
        Vector3 a = d.transform.position;

        Vector2 sc = Random.insideUnitCircle * landingScatter;
        Vector3 landingOffset = new Vector3(sc.x, 0f, sc.y);
        Vector3 b = TargetPosition() + landingOffset;

        float duration = Mathf.Max(0.1f, flightDuration + Random.Range(-flightDurationVariance, flightDurationVariance));
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float p = Mathf.Clamp01(t);
            float u = Mathf.Lerp(p, p * p, 0.6f);

            b = TargetPosition() + landingOffset;
            Vector3 c = (a + b) * 0.5f + Vector3.up * arcHeight;
            float inv = 1f - u;
            Vector3 pos = inv * inv * a + 2f * inv * u * c + u * u * b;

            if (d != null)
            {
                d.transform.position = pos;
                d.transform.localScale = baseDropScale * Mathf.Lerp(1f, 0.6f, u);
            }
            yield return null;
        }

        if (d != null) Destroy(d);

        NotifyArrival(b, shareML);
        UnregisterFlight();
    }

    GameObject CreateDroplet(Vector3 pos)
    {
        GameObject d;

        if (dropletPrefab != null)
        {
            d = Instantiate(dropletPrefab, pos, Quaternion.identity);
        }
        else
        {
            d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            d.transform.position = pos;
            d.transform.localScale = Vector3.one * dropletSize;

            Renderer r = d.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = GetDropletMaterial();
        }

        foreach (Collider col in d.GetComponentsInChildren<Collider>()) Destroy(col);
        foreach (Rigidbody rb in d.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;

        return d;
    }

    Material GetDropletMaterial()
    {
        if (dropletMaterial != null) return dropletMaterial;

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dropletMaterial = new Material(temp.GetComponent<Renderer>().sharedMaterial);
        dropletMaterial.color = dropletColor;
        Destroy(temp);
        return dropletMaterial;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(TargetPosition(), 0.15f);
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(TargetPosition(), Mathf.Max(0.01f, landingScatter));
    }
}