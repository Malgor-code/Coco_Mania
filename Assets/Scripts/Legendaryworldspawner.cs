using System.Collections.Generic;
using UnityEngine;

public class LegendaryWorldSpawner : MonoBehaviour
{
    public static LegendaryWorldSpawner Instance { get; private set; }

    [SerializeField] Transform[] spawnPoints;
    [Tooltip("Si no hay puntos libres, aparece al azar en este radio alrededor de este objeto.")]
    [SerializeField] float fallbackRadius = 6f;
    [SerializeField] float minSeparation = 1.5f;

    readonly Dictionary<string, LegendaryItem> spawned = new Dictionary<string, LegendaryItem>();

    void Awake() { Instance = this; }

    public bool Has(string id)
    {
        return spawned.TryGetValue(id, out var item) && item != null;
    }
    public bool Spawn(RewardData reward)
    {
        if (reward == null || !reward.IsWorldObject) return false;
        if (Has(reward.id)) return false;

        var go = Instantiate(reward.worldPrefab, PickPosition(), Quaternion.identity);
        var item = go.GetComponent<LegendaryItem>();
        if (item == null) item = go.AddComponent<LegendaryItem>();
        item.Init(reward);
        spawned[reward.id] = item;
        return true;
    }
    public void ClearAll()
    {
        foreach (var kv in spawned)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        spawned.Clear();
    }

    Vector3 PickPosition()
    {
        var free = new List<Transform>();
        if (spawnPoints != null)
            foreach (var p in spawnPoints)
                if (p != null && IsFree(p.position)) free.Add(p);

        if (free.Count > 0) return free[Random.Range(0, free.Count)].position;

        for (int i = 0; i < 20; i++)
        {
            Vector2 c = Random.insideUnitCircle * fallbackRadius;
            Vector3 pos = transform.position + new Vector3(c.x, 0f, c.y);
            if (IsFree(pos)) return pos;
        }
        return transform.position;
    }

    bool IsFree(Vector3 pos)
    {
        foreach (var kv in spawned)
        {
            if (kv.Value == null) continue;
            if ((kv.Value.transform.position - pos).sqrMagnitude < minSeparation * minSeparation) return false;
        }
        return true;
    }
}