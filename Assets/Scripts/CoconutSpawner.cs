using UnityEngine;
using System.Collections.Generic;

public class CoconutSpawner : MonoBehaviour
{
    public static CoconutSpawner Instance;

    [Header("Prefab y area de spawn")]
    public GameObject coconutPrefab;
    public Transform mapCenter;              
    public Vector2 mapHalfExtents = new Vector2(8f, 8f); 

    [Header("Reglas de spawn")]
    public int startingCoconuts = 4;
    public float spawnInterval = 5f;
    public int maxCoconuts = 15;

    [Header("Tipos de coco (progresion por muertes)")]
    public List<CoconutTypeData> coconutTypes = new List<CoconutTypeData>();

    private float spawnTimer;
    private readonly List<CoconutTarget> activeCoconuts = new List<CoconutTarget>();

    void Awake()
    {
        Instance = this;

        if (coconutTypes == null || coconutTypes.Count == 0)
        {
            coconutTypes = new List<CoconutTypeData>
            {
                new CoconutTypeData { typeName = "Coco Comun", killsToUnlock = 0, hpMultiplier = 1f, lootMultiplier = 1f },
                new CoconutTypeData { typeName = "Coco Duro", killsToUnlock = 15, hpMultiplier = 2f, lootMultiplier = 2f },
                new CoconutTypeData { typeName = "Coco Blindado", killsToUnlock = 40, hpMultiplier = 3.2f, lootMultiplier = 3.5f },
            };
        }
    }

    public void StartNewDay()
    {
        for (int i = activeCoconuts.Count - 1; i >= 0; i--)
        {
            if (activeCoconuts[i] != null) Destroy(activeCoconuts[i].gameObject);
        }
        activeCoconuts.Clear();

        for (int i = 0; i < startingCoconuts; i++)
        {
            SpawnOne();
        }
        spawnTimer = 0f;
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

    void SpawnOne()
    {
        if (coconutPrefab == null) return;

        CoconutTypeData chosenType = PickTypeToSpawn();

        Vector3 center = mapCenter != null ? mapCenter.position : Vector3.zero;
        Vector3 pos = center + new Vector3(
            Random.Range(-mapHalfExtents.x, mapHalfExtents.x),
            0f,
            Random.Range(-mapHalfExtents.y, mapHalfExtents.y)
        );
        pos.y = coconutPrefab.transform.position.y;

        GameObject go = Instantiate(coconutPrefab, pos, Quaternion.identity);

        CoconutTarget target = go.GetComponent<CoconutTarget>();
        if (target != null)
        {
            target.ApplyType(chosenType);
            activeCoconuts.Add(target);
        }

        CoconutWander wander = go.GetComponent<CoconutWander>();
        if (wander != null) wander.SetBounds(center, mapHalfExtents);
    }

    CoconutTypeData PickTypeToSpawn()
    {
        int kills = GameManager.Instance != null ? GameManager.Instance.totalCoconutsKilled : 0;

        List<CoconutTypeData> unlocked = new List<CoconutTypeData>();
        foreach (var t in coconutTypes)
        {
            if (kills >= t.killsToUnlock) unlocked.Add(t);
        }

        if (unlocked.Count == 0)
        {
            return coconutTypes.Count > 0 ? coconutTypes[0] : new CoconutTypeData();
        }

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