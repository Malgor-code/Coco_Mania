using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CoconutSpawner : MonoBehaviour
{
    public static CoconutSpawner Instance;

    [Header("Prefab de respaldo (fallback) y area de spawn")]
    [Tooltip("Se usa SOLO si el tipo elegido (en 'Tipos de coco' abajo) no tiene su propio prefab asignado todavia. Cada tier deberia tener el suyo.")]
    public GameObject coconutPrefab;
    public Transform mapCenter;
    public Vector2 mapHalfExtents = new Vector2(8f, 8f);
    [Header("Reglas de spawn")]
    public int startingCoconuts = 4;
    public float spawnInterval = 5f;
    public int maxCoconuts = 15;
    public float baseScale = 0.6f;
    public float scaleGrowthPerTier = 0.05f;
    public float maxScale = 1.4f;
    public float hpBoost = 1.5f;
    public float startSpawnStagger = 0.3f;
    public event System.Action OnInitialSpawnComplete;
    public float spawnSpeedMultiplier = 2f;
    public float tierWeightBias = 0f;
    public void SpawnExtraCoconut() { StartCoroutine(SpawnExtraDelayed()); }
    [Header("Tipos de coco (progresion por muertes)")]
    public List<CoconutTypeData> coconutTypes = new List<CoconutTypeData>();

    public bool DayActive { get; private set; }

    private float spawnTimer;
    private readonly List<CoconutTarget> activeCoconuts = new List<CoconutTarget>();

    private static readonly CoconutTypeData[] CanonicalTypes = new CoconutTypeData[]
    {
    new CoconutTypeData { typeName = "Coco Verde",       hpMultiplier = 1.00f, lootMultiplier = 1.00f },
    new CoconutTypeData { typeName = "Coco Maduro",      hpMultiplier = 3f,    lootMultiplier = 1.4f },
    new CoconutTypeData { typeName = "Coco Correoso",    hpMultiplier = 6f,   lootMultiplier = 1.8f },
    new CoconutTypeData { typeName = "Coco Fibroso",     hpMultiplier = 10f,  lootMultiplier = 2.2f },
    new CoconutTypeData { typeName = "Coco Petreo",      hpMultiplier = 18f,   lootMultiplier = 2.6f },
    new CoconutTypeData { typeName = "Coco Curtido",     hpMultiplier = 23f,   lootMultiplier = 3f },
    new CoconutTypeData { typeName = "Coco Blindado",    hpMultiplier = 30f,   lootMultiplier = 3.66f },
    new CoconutTypeData { typeName = "Coco de Hierro",   hpMultiplier = 42f,  lootMultiplier = 4.46f },
    new CoconutTypeData { typeName = "Coco de Acero",    hpMultiplier = 52f,  lootMultiplier = 5.23f },
    new CoconutTypeData { typeName = "Coco de Titanio",  hpMultiplier = 63f,   lootMultiplier = 5.98f },
    new CoconutTypeData { typeName = "Coco de Diamante", hpMultiplier = 71f,   lootMultiplier = 6.84f },
    new CoconutTypeData { typeName = "Coco Legendario",  hpMultiplier = 90f,   lootMultiplier = 8.26f },
    new CoconutTypeData { typeName = "Coco Mitico",      hpMultiplier = 110f,  lootMultiplier = 9.99f },
    new CoconutTypeData { typeName = "Coco Ancestral",   hpMultiplier = 140f,  lootMultiplier = 11.39f },
    new CoconutTypeData { typeName = "Coco Supremo",     hpMultiplier = 160f, lootMultiplier = 12.77f },
};

    public const float BaseSpawnInterval = 10f;

    void Awake()
    {
        Instance = this;
        spawnInterval = BaseSpawnInterval;
        StampCanonicalData();
    }

    void StampCanonicalData()
    {
        if (coconutTypes == null) coconutTypes = new List<CoconutTypeData>();

        while (coconutTypes.Count < CanonicalTypes.Length)
            coconutTypes.Add(new CoconutTypeData());

        for (int i = 0; i < CanonicalTypes.Length; i++)
        {
            GameObject keepPrefab = coconutTypes[i].prefab;
            coconutTypes[i].typeName = CanonicalTypes[i].typeName;
            coconutTypes[i].hpMultiplier = CanonicalTypes[i].hpMultiplier * hpBoost;
            coconutTypes[i].lootMultiplier = CanonicalTypes[i].lootMultiplier;
            coconutTypes[i].prefab = keepPrefab;
        }
    }

    public void StartNewDay()
    {
        for (int i = activeCoconuts.Count - 1; i >= 0; i--)
        {
            if (activeCoconuts[i] != null) Destroy(activeCoconuts[i].gameObject);
        }
        activeCoconuts.Clear();
        spawnTimer = 0f;

        StopAllCoroutines();
        DayActive = true;
        StartCoroutine(SpawnStartingCoconutsSequenced());
    }
    IEnumerator SpawnExtraDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        if (!DayActive) yield break;
        if (GameManager.Instance == null || !GameManager.Instance.IsHittingPhase()) yield break;
        if (activeCoconuts.Count >= maxCoconuts) yield break;
        SpawnOne();
    }
    IEnumerator SpawnStartingCoconutsSequenced()
    {
        for (int i = 0; i < startingCoconuts; i++)
        {
            SpawnOne();
            if (i < startingCoconuts - 1 && startSpawnStagger > 0f)
                yield return new WaitForSeconds(startSpawnStagger);
        }

        OnInitialSpawnComplete?.Invoke();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsHittingPhase()) return;

        activeCoconuts.RemoveAll(c => c == null);
        if (activeCoconuts.Count == 0)
        {
            SpawnOne();
            spawnTimer = 0f;
            return;
        }
        if (!GameManager.Instance.TimedSpawnUnlocked) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= Mathf.Max(0.3f, spawnInterval) && activeCoconuts.Count < maxCoconuts)
        {
            spawnTimer = 0f;
            SpawnOne();
        }
    }

    public void ClearAllCoconuts()
    {
        DayActive = false;
        foreach (var coco in GetActiveCoconuts())
        {
            if (coco != null) Destroy(coco.gameObject);
        }
        activeCoconuts.Clear();
    }

    void SpawnOne()
    {
        CoconutTypeData chosenType = PickTypeToSpawn();

        GameObject prefabToUse = (chosenType != null && chosenType.prefab != null) ? chosenType.prefab : coconutPrefab;
        if (prefabToUse == null) return;

        Vector3 center = mapCenter != null ? mapCenter.position : Vector3.zero;
        Vector3 pos = center + new Vector3(
            Random.Range(-mapHalfExtents.x, mapHalfExtents.x),
            0f,
            Random.Range(-mapHalfExtents.y, mapHalfExtents.y)
        );
        pos.y = prefabToUse.transform.position.y;
        GameObject go = Instantiate(prefabToUse, pos, Quaternion.identity);
        CoconutTarget target = EnsureCoconutComponents(go);
        target.ApplyType(chosenType);
        activeCoconuts.Add(target);

        CoconutWander wander = go.GetComponent<CoconutWander>();
        if (wander != null) wander.SetBounds(center, mapHalfExtents);
    }

    CoconutTarget EnsureCoconutComponents(GameObject go)
    {
        if (go.GetComponent<CoconutWander>() == null)
        {
            go.AddComponent<CoconutWander>();
        }

        CoconutTarget target = go.GetComponent<CoconutTarget>();
        if (target == null)
        {
            target = go.AddComponent<CoconutTarget>();
        }

        return target;
    }

    CoconutTypeData PickTypeToSpawn()
    {
        int kills = GameManager.Instance != null ? GameManager.Instance.killsAtDayStart : 0;
        List<CoconutTypeData> unlocked = new List<CoconutTypeData>();

        for (int i = 0; i < coconutTypes.Count; i++)
        {
            bool hasThreshold = GameManager.Instance != null && i < GameManager.Instance.coconutUnlocks.Count;
            int required = hasThreshold ? GameManager.Instance.coconutUnlocks[i].killsRequired : (i == 0 ? 0 : int.MaxValue);

            if (kills >= required) unlocked.Add(coconutTypes[i]);
        }

        if (unlocked.Count == 0)
            return coconutTypes.Count > 0 ? coconutTypes[0] : new CoconutTypeData();

        if (Mathf.Approximately(tierWeightBias, 0f) || unlocked.Count == 1)
            return unlocked[Random.Range(0, unlocked.Count)];

        float[] w = new float[unlocked.Count];
        float total = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            float t = i / (float)(w.Length - 1);
            w[i] = Mathf.Max(0.05f, 1f + tierWeightBias * t);
            total += w[i];
        }
        float r = Random.value * total;
        for (int i = 0; i < w.Length; i++)
        {
            r -= w[i];
            if (r <= 0f) return unlocked[i];
        }
        return unlocked[unlocked.Count - 1];
    }


    public void RemoveCoconut(CoconutTarget coco)
    {
        activeCoconuts.Remove(coco);
    }

    public List<CoconutTarget> GetActiveCoconuts()
    {
        return new List<CoconutTarget>(activeCoconuts);
    }
}