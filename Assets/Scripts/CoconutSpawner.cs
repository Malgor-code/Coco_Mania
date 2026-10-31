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
    public int maxCoconuts = 40;
    public float baseScale = 1f;
    public float scaleGrowthPerTier = 0.05f;
    public float maxScale = 1.4f;
    public float hpBoost = 1.5f;
    public float startSpawnStagger = 0.3f;
    public event System.Action OnInitialSpawnComplete;
    public float spawnSpeedMultiplier = 2f;
    public float globalSizeMultiplier = 0.7f;
    public float minSpawnDistance = 1.5f;

    [Header("Probabilidad por tipo (favorece los cocos anteriores)")]
    [Tooltip("Cada tipo desbloqueado pesa 'decay' veces lo que el anterior. 0.75 = el tipo mas nuevo sale bastante menos que el primero. 1 = todos igual.")]
    [Range(0.3f, 1f)] public float tierWeightDecay = 0.75f;
    [Tooltip("Peso minimo de cualquier tipo, para que los tipos altos nunca desaparezcan del todo.")]
    [Range(0.01f, 0.5f)] public float minTierWeight = 0.08f;
    [Tooltip("Maximo de cocos del tipo MAS NUEVO vivos al mismo tiempo. Evita que el tablero se llene de cocos que aun no puedes matar. 0 = sin limite.")]
    public int maxNewestTierAlive = 2;

    [Header("Recompensa por tipo")]
    [Tooltip("1 = el agua crece igual que la vida. Mayor a 1 = los cocos grandes pagan mas agua por golpe que los chicos.")]
    public float lootExponent = 1.15f;
    [Tooltip("Agua del Coco Verde (el primero). El resto se calcula con lootExponent.")]
    public float lootBase = 1.2f;
    [HideInInspector] public float tierWeightBias = 0f;
    [HideInInspector] public float hpGrowthPerCycle = 1f;

    float GetCycleHpMultiplier()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetLegacyHpScale() : 1f;
    }
    public void SpawnExtraCoconut() { StartCoroutine(SpawnExtraDelayed()); }
    [Header("Tipos de coco (progresion por ciclo)")]
    public List<CoconutTypeData> coconutTypes = new List<CoconutTypeData>();

    public bool DayActive { get; private set; }

    private float spawnTimer;
    private readonly List<CoconutTarget> activeCoconuts = new List<CoconutTarget>();
    private readonly Dictionary<CoconutTarget, int> tierOf = new Dictionary<CoconutTarget, int>();
    private static readonly CoconutTypeData[] CanonicalTypes = new CoconutTypeData[]
{
    new CoconutTypeData { typeName = "Coco Verde",       hpMultiplier = 1f },
    new CoconutTypeData { typeName = "Coco Maduro",      hpMultiplier = 2.2f },
    new CoconutTypeData { typeName = "Coco Correoso",    hpMultiplier = 6f },
    new CoconutTypeData { typeName = "Coco Fibroso",     hpMultiplier = 12f },
    new CoconutTypeData { typeName = "Coco Petreo",      hpMultiplier = 20f },
    new CoconutTypeData { typeName = "Coco Curtido",     hpMultiplier = 26f },
    new CoconutTypeData { typeName = "Coco Blindado",    hpMultiplier = 34f },
    new CoconutTypeData { typeName = "Coco de Hierro",   hpMultiplier = 46f },
    new CoconutTypeData { typeName = "Coco de Acero",    hpMultiplier = 58f },
    new CoconutTypeData { typeName = "Coco de Titanio",  hpMultiplier = 70f },
    new CoconutTypeData { typeName = "Coco de Diamante", hpMultiplier = 80f },
    new CoconutTypeData { typeName = "Coco Legendario",  hpMultiplier = 98f },
    new CoconutTypeData { typeName = "Coco Mitico",      hpMultiplier = 118f },
    new CoconutTypeData { typeName = "Coco Ancestral",   hpMultiplier = 145f },
    new CoconutTypeData { typeName = "Coco Supremo",     hpMultiplier = 165f },
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
            float baseHp = CanonicalTypes[i].hpMultiplier;
            coconutTypes[i].typeName = CanonicalTypes[i].typeName;
            coconutTypes[i].hpMultiplier = baseHp * hpBoost;
            // Agua = lootBase * vida ^ lootExponent  ->  los cocos grandes pagan mas agua por golpe.
            coconutTypes[i].lootMultiplier = lootBase * Mathf.Pow(baseHp, lootExponent);
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
        tierOf.Clear();
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
        if (tierOf.Count > activeCoconuts.Count + 16) CleanTierMap();

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
        tierOf.Clear();
    }

    void SpawnOne()
    {
        int tierIndex;
        CoconutTypeData chosenType = PickTypeToSpawn(out tierIndex);

        GameObject prefabToUse = (chosenType != null && chosenType.prefab != null) ? chosenType.prefab : coconutPrefab;
        if (prefabToUse == null) return;

        Vector3 center = mapCenter != null ? mapCenter.position : Vector3.zero;
        Vector3 pos = PickSpawnPosition(center);
        pos.y = prefabToUse.transform.position.y;
        GameObject go = Instantiate(prefabToUse, pos, Quaternion.identity);
        CoconutTarget target = EnsureCoconutComponents(go);
        target.ApplyType(chosenType, GetCycleHpMultiplier());
        activeCoconuts.Add(target);
        tierOf[target] = tierIndex;

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
    CoconutTypeData PickTypeToSpawn(out int chosenIndex)
    {
        int cycle = GameManager.Instance != null ? GameManager.Instance.billCycle : 1;
        List<int> unlocked = new List<int>();

        for (int i = 0; i < coconutTypes.Count; i++)
        {
            bool hasThreshold = GameManager.Instance != null && i < GameManager.Instance.coconutUnlocks.Count;
            int required = hasThreshold ? GameManager.Instance.coconutUnlocks[i].cycleRequired : (i == 0 ? 1 : int.MaxValue);
            if (cycle >= required) unlocked.Add(i);
        }

        if (unlocked.Count == 0)
        {
            chosenIndex = coconutTypes.Count > 0 ? 0 : -1;
            return coconutTypes.Count > 0 ? coconutTypes[0] : new CoconutTypeData();
        }

        if (unlocked.Count == 1)
        {
            chosenIndex = unlocked[0];
            return coconutTypes[chosenIndex];
        }

        int newest = unlocked[unlocked.Count - 1];
        bool newestFull = maxNewestTierAlive > 0 && CountAlive(newest) >= maxNewestTierAlive;
        int usable = newestFull ? unlocked.Count - 1 : unlocked.Count;

        float[] w = new float[usable];
        float total = 0f;
        for (int k = 0; k < usable; k++)
        {
            w[k] = Mathf.Max(minTierWeight, Mathf.Pow(tierWeightDecay, k));
            total += w[k];
        }

        float r = Random.value * total;
        for (int k = 0; k < usable; k++)
        {
            r -= w[k];
            if (r <= 0f)
            {
                chosenIndex = unlocked[k];
                return coconutTypes[chosenIndex];
            }
        }

        chosenIndex = unlocked[usable - 1];
        return coconutTypes[chosenIndex];
    }

    int CountAlive(int tierIndex)
    {
        int count = 0;
        for (int i = 0; i < activeCoconuts.Count; i++)
        {
            CoconutTarget c = activeCoconuts[i];
            if (c == null) continue;
            if (tierOf.TryGetValue(c, out int t) && t == tierIndex) count++;
        }
        return count;
    }

    void CleanTierMap()
    {
        var dead = new List<CoconutTarget>();
        foreach (var kv in tierOf)
            if (kv.Key == null) dead.Add(kv.Key);
        for (int i = 0; i < dead.Count; i++) tierOf.Remove(dead[i]);
    }

    public void RemoveCoconut(CoconutTarget coco)
    {
        activeCoconuts.Remove(coco);
        if (coco != null) tierOf.Remove(coco);
    }

    public List<CoconutTarget> GetActiveCoconuts()
    {
        return new List<CoconutTarget>(activeCoconuts);
    }
    Vector3 PickSpawnPosition(Vector3 center)
    {
        Vector3 best = center;
        float bestDist = -1f;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 p = center + new Vector3(
                Random.Range(-mapHalfExtents.x, mapHalfExtents.x), 0f,
                Random.Range(-mapHalfExtents.y, mapHalfExtents.y));
            float nearest = float.MaxValue;
            foreach (var c in activeCoconuts)
            {
                if (c == null) continue;
                Vector3 cp = c.transform.position;
                nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(p.x, p.z), new Vector2(cp.x, cp.z)));
            }
            if (nearest >= minSpawnDistance) return p;
            if (nearest > bestDist) { bestDist = nearest; best = p; }
        }
        return best;
    }
}