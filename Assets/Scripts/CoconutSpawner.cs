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
    [Header("Animacion al golpear (objeto de la escena)")]
    [Tooltip("Arrastra aqui el objeto con Animator de la escena. Se le pasa a cada coco al nacer.")]
    public Animator hitAnimator;
    [Header("Reglas de spawn")]
    public int startingCoconuts = 4;
    public float spawnInterval = 5f;
    public int maxCoconuts = 15;
    public float startSpawnStagger = 0.3f;
    public event System.Action OnInitialSpawnComplete;
    public void SpawnExtraCoconut() { StartCoroutine(SpawnExtraDelayed()); }
    [Header("Tipos de coco (progresion por muertes)")]
    public List<CoconutTypeData> coconutTypes = new List<CoconutTypeData>();

    public bool DayActive { get; private set; }

    private float spawnTimer;
    private readonly List<CoconutTarget> activeCoconuts = new List<CoconutTarget>();

    private static readonly CoconutTypeData[] CanonicalTypes = new CoconutTypeData[]
    {
        new CoconutTypeData { typeName = "Coco Verde",       killsToUnlock = 0,   hpMultiplier = 1.00f,  lootMultiplier = 1.00f },
        new CoconutTypeData { typeName = "Coco Maduro",      killsToUnlock = 12,  hpMultiplier = 1.33f,  lootMultiplier = 1.15f },
        new CoconutTypeData { typeName = "Coco Correoso",    killsToUnlock = 25,  hpMultiplier = 1.55f,  lootMultiplier = 1.32f },
        new CoconutTypeData { typeName = "Coco Fibroso",     killsToUnlock = 45,  hpMultiplier = 1.73f,  lootMultiplier = 1.52f },
        new CoconutTypeData { typeName = "Coco Petreo",      killsToUnlock = 70,  hpMultiplier = 2.07f,  lootMultiplier = 1.75f },
        new CoconutTypeData { typeName = "Coco Curtido",     killsToUnlock = 100, hpMultiplier = 2.49f,  lootMultiplier = 2.01f },
        new CoconutTypeData { typeName = "Coco Blindado",    killsToUnlock = 140, hpMultiplier = 2.99f,  lootMultiplier = 2.31f },
        new CoconutTypeData { typeName = "Coco de Hierro",   killsToUnlock = 185, hpMultiplier = 3.58f,  lootMultiplier = 2.66f },
        new CoconutTypeData { typeName = "Coco de Acero",    killsToUnlock = 235, hpMultiplier = 4.30f,  lootMultiplier = 3.06f },
        new CoconutTypeData { typeName = "Coco de Titanio",  killsToUnlock = 290, hpMultiplier = 5.16f,  lootMultiplier = 3.52f },
        new CoconutTypeData { typeName = "Coco de Diamante", killsToUnlock = 350, hpMultiplier = 6.19f,  lootMultiplier = 4.05f },
        new CoconutTypeData { typeName = "Coco Legendario",  killsToUnlock = 420, hpMultiplier = 7.43f,  lootMultiplier = 4.65f },
        new CoconutTypeData { typeName = "Coco Mitico",      killsToUnlock = 500, hpMultiplier = 8.92f,  lootMultiplier = 5.35f },
        new CoconutTypeData { typeName = "Coco Ancestral",   killsToUnlock = 600, hpMultiplier = 10.70f, lootMultiplier = 6.15f },
        new CoconutTypeData { typeName = "Coco Supremo",     killsToUnlock = 720, hpMultiplier = 12.84f, lootMultiplier = 7.08f },
    };

    void Awake()
    {
        Instance = this;
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
            coconutTypes[i].killsToUnlock = CanonicalTypes[i].killsToUnlock;
            coconutTypes[i].hpMultiplier = CanonicalTypes[i].hpMultiplier;
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

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && activeCoconuts.Count < maxCoconuts)
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
        if (hitAnimator != null) target.hitAnimator = hitAnimator;
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
        int kills = GameManager.Instance != null ? GameManager.Instance.totalCoconutsKilled : 0;
        List<CoconutTypeData> unlocked = new List<CoconutTypeData>();

        for (int i = 0; i < coconutTypes.Count; i++)
        {
            int required = coconutTypes[i].killsToUnlock;
            if (GameManager.Instance != null && i < GameManager.Instance.coconutUnlocks.Count)
                required = GameManager.Instance.coconutUnlocks[i].killsRequired;

            if (kills >= required) unlocked.Add(coconutTypes[i]);
        }

        if (unlocked.Count == 0)
            return coconutTypes.Count > 0 ? coconutTypes[0] : new CoconutTypeData();

        return unlocked[Random.Range(0, unlocked.Count)];
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