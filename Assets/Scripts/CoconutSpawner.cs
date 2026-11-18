using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class CoconutSpawner : MonoBehaviour
{
    public static CoconutSpawner Instance;

    [Header("Prefab de respaldo (fallback) y area de spawn")]
    [Tooltip("Se usa SOLO si el tipo elegido no tiene su propio prefab asignado todavia.")]
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
    [Tooltip("Maximo de cocos del tipo MAS NUEVO vivos al mismo tiempo. 0 = sin limite.")]
    public int maxNewestTierAlive = 2;

    [Header("Recompensa por tipo")]
    [Tooltip("1 = el agua crece igual que la vida. Mayor a 1 = los cocos grandes pagan mas agua por golpe que los chicos.")]
    public float lootExponent = 1.15f;
    [Tooltip("Agua del Coco Verde (el primero). El resto se calcula con lootExponent.")]
    public float lootBase = 1.2f;
    [HideInInspector] public float tierWeightBias = 0f;
    [HideInInspector] public float hpGrowthPerCycle = 1f;

    [Header("Tipos de coco (en orden de desbloqueo)")]
    [Tooltip("Se llena con CocoMania > Cocos > 2. Asignar los tipos al CoconutSpawner.")]
    public List<CoconutTypeSO> coconutTypes = new List<CoconutTypeSO>();

    public bool DayActive { get; private set; }
    public const float BaseSpawnInterval = 10f;

    float spawnTimer;
    Transform poolRoot;
    readonly List<CoconutTarget> activeCoconuts = new List<CoconutTarget>();
    readonly Dictionary<CoconutTarget, int> tierOf = new Dictionary<CoconutTarget, int>();
    readonly Dictionary<GameObject, ObjectPool<CoconutTarget>> pools = new Dictionary<GameObject, ObjectPool<CoconutTarget>>();
    readonly Dictionary<CoconutTarget, ObjectPool<CoconutTarget>> poolOf = new Dictionary<CoconutTarget, ObjectPool<CoconutTarget>>();
    readonly HashSet<CoconutTarget> dying = new HashSet<CoconutTarget>();

    void Awake()
    {
        Instance = this;
        spawnInterval = BaseSpawnInterval;
        poolRoot = new GameObject("CoconutPool").transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (poolRoot != null) Destroy(poolRoot.gameObject);
    }

    ObjectPool<CoconutTarget> GetPool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<CoconutTarget> pool)) return pool;

        pool = new ObjectPool<CoconutTarget>(
            createFunc: () => CreateInstance(prefab),
            actionOnGet: c => c.gameObject.SetActive(true),
            actionOnRelease: c => c.gameObject.SetActive(false),
            actionOnDestroy: c => { if (c != null) Destroy(c.gameObject); },
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize: 64);
        pools[prefab] = pool;
        return pool;
    }

    CoconutTarget CreateInstance(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, poolRoot);
        return EnsureCoconutComponents(go);
    }

    CoconutTarget EnsureCoconutComponents(GameObject go)
    {
        if (go.GetComponent<CoconutWander>() == null) go.AddComponent<CoconutWander>();

        CoconutTarget target = go.GetComponent<CoconutTarget>();
        if (target == null) target = go.AddComponent<CoconutTarget>();
        return target;
    }

    void ReleaseNow(CoconutTarget c)
    {
        if (c == null) return;
        if (poolOf.TryGetValue(c, out ObjectPool<CoconutTarget> pool))
        {
            poolOf.Remove(c);
            pool.Release(c);
        }
        else
        {
            Destroy(c.gameObject);
        }
    }

    public void ReleaseDelayed(CoconutTarget c, float delay)
    {
        if (c == null) return;
        dying.Add(c);
        StartCoroutine(ReleaseRoutine(c, delay));
    }

    IEnumerator ReleaseRoutine(CoconutTarget c, float delay)
    {
        yield return new WaitForGameSeconds(delay);
        if (dying.Remove(c)) ReleaseNow(c);
    }

    void ReleaseDying()
    {
        foreach (var c in dying) ReleaseNow(c);
        dying.Clear();
    }
    public void StartNewDay()
    {
        ReleaseDying();
        for (int i = activeCoconuts.Count - 1; i >= 0; i--) ReleaseNow(activeCoconuts[i]);
        activeCoconuts.Clear();
        tierOf.Clear();
        spawnTimer = 0f;

        StopAllCoroutines();
        DayActive = true;
        StartCoroutine(SpawnStartingCoconutsSequenced());
    }

    public void ClearAllCoconuts()
    {
        DayActive = false;
        for (int i = activeCoconuts.Count - 1; i >= 0; i--) ReleaseNow(activeCoconuts[i]);
        activeCoconuts.Clear();
        tierOf.Clear();
    }

    public void SpawnExtraCoconut() { StartCoroutine(SpawnExtraDelayed()); }

    IEnumerator SpawnExtraDelayed()
    {
        yield return new WaitForGameSeconds(0.3f);
        if (!DayActive) yield break;
        if (!RunManager.Instance.IsHitting) yield break;
        if (activeCoconuts.Count >= maxCoconuts) yield break;
        SpawnOne();
    }

    IEnumerator SpawnStartingCoconutsSequenced()
    {
        for (int i = 0; i < startingCoconuts; i++)
        {
            SpawnOne();
            if (i < startingCoconuts - 1 && startSpawnStagger > 0f)
                yield return new WaitForGameSeconds(startSpawnStagger);
        }

        OnInitialSpawnComplete?.Invoke();
    }

    void Update()
    {
        if (GameManager.IsPaused) return;
        if (!RunManager.Instance.IsHitting) return;

        activeCoconuts.RemoveAll(c => c == null);
        if (tierOf.Count > activeCoconuts.Count + 16) CleanTierMap();

        if (activeCoconuts.Count == 0)
        {
            SpawnOne();
            spawnTimer = 0f;
            return;
        }
        if (!SkillTreeManager.Instance.TimedSpawnUnlocked) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= Mathf.Max(0.3f, spawnInterval) && activeCoconuts.Count < maxCoconuts)
        {
            spawnTimer = 0f;
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        CoconutTypeSO chosenType = PickTypeToSpawn(out int tierIndex);

        GameObject prefabToUse = (chosenType != null && chosenType.prefab != null) ? chosenType.prefab : coconutPrefab;
        if (prefabToUse == null) return;

        Vector3 center = mapCenter != null ? mapCenter.position : Vector3.zero;
        Vector3 pos = PickSpawnPosition(center);
        pos.y = prefabToUse.transform.position.y;

        ObjectPool<CoconutTarget> pool = GetPool(prefabToUse);
        CoconutTarget target = pool.Get();
        poolOf[target] = pool;
        target.transform.SetPositionAndRotation(pos, Quaternion.identity);

        float hpMult = chosenType != null ? chosenType.HpMultiplier(hpBoost) : hpBoost;
        float loot = chosenType != null ? chosenType.LootMultiplier(lootBase, lootExponent) : lootBase;
        target.ApplyType(chosenType, hpMult, loot, LegacyManager.Instance.CurrentHpScale);

        activeCoconuts.Add(target);
        tierOf[target] = tierIndex;

        CoconutWander wander = target.GetComponent<CoconutWander>();
        if (wander != null) wander.SetBounds(center, mapHalfExtents);
    }

    CoconutTypeSO PickTypeToSpawn(out int chosenIndex)
    {
        int cycle = DebtManager.Instance.BillCycle;
        List<int> unlocked = new List<int>();

        for (int i = 0; i < coconutTypes.Count; i++)
        {
            CoconutTypeSO t = coconutTypes[i];
            if (t == null) continue;
            int required = i == 0 ? 1 : t.unlockCycle;
            if (cycle >= required) unlocked.Add(i);
        }

        if (unlocked.Count == 0)
        {
            chosenIndex = coconutTypes.Count > 0 ? 0 : -1;
            return coconutTypes.Count > 0 ? coconutTypes[0] : null;
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