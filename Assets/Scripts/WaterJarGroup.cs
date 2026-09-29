using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaterJarGroup : MonoBehaviour
{
    public static WaterJarGroup Instance;

    [Header("Jarrones (en orden: se llenan de izquierda a derecha)")]
    public List<WaterJar3D> jars = new List<WaterJar3D>();
    [Tooltip("Cuanta agua (mL) llena UN jarron por completo antes de pasar al siguiente.")]
    public float capacityPerJarML = 200f;

    [Header("Particulas de los cocos (Death Particles) -> jarron activo")]
    [Tooltip("Tiempo que las particulas hacen su caida/animacion normal en el piso antes de salir volando hacia el jarron.")]
    public float particleAttractDelay = 3f;
    public float particleMinSpeed = 2f;
    public float particleMaxSpeed = 14f;
    [Tooltip("Tiempo que tardan en pasar de la velocidad minima a la maxima al salir volando.")]
    public float particleAccelTime = 0.6f;
    [Tooltip("Que tan rapido giran hacia el jarron (mas alto = trayectoria mas directa).")]
    public float particleSteering = 10f;
    public float particleArriveDistance = 0.25f;
    [Tooltip("Seguro: si algo sale mal, el efecto se elimina solo pasado este tiempo.")]
    public float particleMaxLifetime = 10f;

    [Header("Destino / dispersion")]
    [Tooltip("Radio alrededor de la boca del jarron activo donde caen las gotas/particulas, para que no lleguen todas al mismo punto.")]
    public float landingScatter = 0.25f;

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

    private float visualML = 0f;
    private int inFlight = 0;
    private bool subscribed = false;
    private Material dropletMaterial;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnDayStarted -= ResetDay;
    }

    void Update()
    {
        if (!subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnDayStarted += ResetDay;
            subscribed = true;
        }

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            float current = gm.dayMoneyEarned;
            if (visualML > current) visualML = current;
            if (inFlight <= 0 && visualML < current)
            {
                float catchup = Mathf.Max(capacityPerJarML, 1f) * 2f;
                visualML = Mathf.MoveTowards(visualML, current, catchup * Time.deltaTime);
            }
        }

        ApplyFillsToJars();
    }

    void ApplyFillsToJars()
    {
        float cap = Mathf.Max(1f, capacityPerJarML);
        for (int i = 0; i < jars.Count; i++)
        {
            if (jars[i] == null) continue;
            float filled01 = Mathf.Clamp01((visualML - i * cap) / cap);
            jars[i].SetFillTarget(filled01);
        }
    }

    int ActiveJarIndex()
    {
        if (jars.Count == 0) return -1;
        float cap = Mathf.Max(1f, capacityPerJarML);
        int idx = Mathf.FloorToInt(visualML / cap);
        return Mathf.Clamp(idx, 0, jars.Count - 1);
    }
    void ResetDay()
    {
        visualML = 0f;
        foreach (var j in jars)
        {
            if (j != null) j.SetFillImmediate(0f);
        }
    }

    public Vector3 TargetPosition()
    {
        int idx = ActiveJarIndex();
        if (idx >= 0 && jars[idx] != null) return jars[idx].TargetPosition();
        return transform.position + Vector3.up;
    }

    public Vector3 GetLandingPoint(uint seed)
    {
        float a = ((seed & 0xFFFF) / 65535f) * Mathf.PI * 2f;
        float r = Mathf.Sqrt(((seed >> 16) & 0xFFFF) / 65535f) * landingScatter;
        return TargetPosition() + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }

    public void RegisterFlight() { inFlight++; }
    public void UnregisterFlight() { inFlight = Mathf.Max(0, inFlight - 1); }

    public void NotifyArrival(Vector3 worldPos, float shareML)
    {
        int idx = ActiveJarIndex();
        visualML += shareML;
        if (idx >= 0 && jars[idx] != null) jars[idx].Arrive(worldPos);
    }

    public bool HandleCoconutWater(Vector3 worldPos, float waterML, GameObject deathFx)
    {
        if (!isActiveAndEnabled || jars.Count == 0) return false;

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

    public void SpawnDroplets(Vector3 worldPos, float waterML)
    {
        if (!isActiveAndEnabled || jars.Count == 0) return;

        int count = Mathf.Clamp(Mathf.RoundToInt(waterML / Mathf.Max(1f, mlPerDroplet)), 1, Mathf.Max(1, maxDropletsPerKill));
        float share = waterML / count;

        for (int i = 0; i < count; i++)
        {
            RegisterFlight();
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

        float duration = Mathf.Max(0.1f, flightDuration + Random.Range(-flightDurationVariance, flightDurationVariance));
        float t = 0f;
        Vector3 b = TargetPosition() + landingOffset;

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