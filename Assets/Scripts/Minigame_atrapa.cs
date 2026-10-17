using System.Collections.Generic;
using UnityEngine;

public class Minigame_Atrapa : MinigameBase
{
    public override string Name => "¡Lluvia de Cocos!";
    public override float Duration => 20f;
    public override string Controls => "Mueve el mouse para mover la canasta. Atrapa los cocos, dorado = x3, ¡esquiva las piedras!";

    public override string[] Intros => new[]
    {
        "Una palmera gigante perdió la paciencia y empezó a soltar cocos como si no hubiera mañana. Agarra una canasta.",
        "El pronóstico dijo 'lluvia ligera'. El pronóstico mintió: llueven cocos. Y alguna que otra piedra.",
        "La tía te prestó su canasta del mandado. Te pidió que no la rompas. Spoiler: la vas a romper.",
    };

    public override bool Is3D => true;
    const float FallTime = 1.87f;
    static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
    static readonly Color StoneColor = new Color(0.45f, 0.45f, 0.5f);

    class Item
    {
        public Transform tr;
        public int type;
        public float speed;
        public Vector3 spin;
        public float radius;
    }

    MinigameStage.AtrapaSetup S;
    readonly List<Item> items = new List<Item>();

    Transform basket;
    Vector3 trackA, trackB, trackDir;
    float trackLen;
    Plane trackPlane;
    float basketT = 0.5f, targetT = 0.5f;
    float topOffset, botOffset, basketHalfW, dropDistance;

    float spawnTimer, spawnInterval, fallSpeed;
    int points;

    public override string Hud => "Puntos: " + points;

    public override float Score01
    {
        get
        {
            float target = Duration / spawnInterval * 0.55f;
            return Mathf.Clamp01(points / target);
        }
    }

    protected override void Setup()
    {
        points = 0;
        items.Clear();
        spawnInterval = Mathf.Max(0.25f, 0.55f / Scale(0.3f));
        spawnTimer = 0.3f;
        basketT = targetT = 0.5f;

        S = Stage != null ? Stage.atrapa : null;
        if (S == null || !S.IsReady)
        {
            Debug.LogError("Minigame_Atrapa: falta configurar MinigameStage > Atrapa (prefabs y Empties). Se omite el minijuego.");
            Finished = true;
            return;
        }

        Stage.UseCamera(S.cameraPoint);
        trackA = S.basketLeft.position;
        trackB = S.basketRight.position;
        Vector3 d = trackB - trackA;
        trackLen = Mathf.Max(0.01f, d.magnitude);
        trackDir = d / trackLen;
        Vector3 n = Vector3.Cross(trackDir, Vector3.up);
        if (n.sqrMagnitude < 1e-6f) n = Vector3.forward;
        trackPlane = new Plane(n.normalized, trackA);
        Vector3 spawnMid = (S.spawnLeft.position + S.spawnRight.position) * 0.5f;
        dropDistance = Mathf.Max(0.1f, spawnMid.y - (trackA.y + trackB.y) * 0.5f);
        fallSpeed = dropDistance / FallTime * Scale(0.25f);
        var bGO = Spawn(S.basketPrefab, Vector3.Lerp(trackA, trackB, 0.5f), S.basketLeft.rotation);
        basket = bGO.transform;
        Bounds b = GetBounds(bGO);
        topOffset = b.max.y - basket.position.y;
        botOffset = b.min.y - basket.position.y;
        basketHalfW = Mathf.Max(0.05f, Mathf.Abs(trackDir.x) * b.extents.x + Mathf.Abs(trackDir.z) * b.extents.z);
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static void Tint(GameObject go, Color c)
    {
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color", c);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb);
    }

    protected override void Tick(float dt)
    {
        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (trackPlane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                targetT = Mathf.Clamp01(Vector3.Dot(hit - trackA, trackDir) / trackLen);
            }
        }
        basketT = Mathf.Lerp(basketT, targetT, 1f - Mathf.Exp(-20f * dt));
        basket.position = Vector3.Lerp(trackA, trackB, basketT);
        Vector3 bp = basket.position;

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnItem();
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item it = items[i];
            it.tr.position += Vector3.down * (it.speed * dt);
            it.tr.Rotate(it.spin * dt, Space.Self);

            Vector3 p = it.tr.position;
            float dx = Mathf.Abs(Vector3.Dot(p - bp, trackDir));
            bool caught = p.y <= bp.y + topOffset + it.radius
                          && p.y >= bp.y + botOffset
                          && dx < basketHalfW + it.radius * 0.3f;
            if (caught)
            {
                points += it.type == 0 ? 1 : it.type == 1 ? 3 : -2;
                Remove(i);
            }
            else if (p.y < bp.y + botOffset - dropDistance * 0.15f)
            {
                Remove(i);
            }
        }
    }

    void SpawnItem()
    {
        float stoneChance = Mathf.Clamp(0.15f + (diff - 1f) * 0.08f, 0.15f, 0.35f);
        float r = Random.value;
        int type = r < stoneChance ? 2 : r < stoneChance + 0.1f ? 1 : 0;

        GameObject prefab = S.cocoPrefab;
        bool tint = type != 0;
        Color c = type == 1 ? Gold : StoneColor;
        if (type == 1 && S.cocoGoldPrefab != null) { prefab = S.cocoGoldPrefab; tint = false; }
        if (type == 2 && S.stonePrefab != null) { prefab = S.stonePrefab; tint = false; }

        Vector3 pos = Vector3.Lerp(S.spawnLeft.position, S.spawnRight.position, Random.value);
        var go = Spawn(prefab, pos, prefab.transform.rotation);
        if (tint) Tint(go, c);

        Bounds b = GetBounds(go);
        items.Add(new Item
        {
            tr = go.transform,
            type = type,
            speed = fallSpeed * Random.Range(0.85f, 1.2f),
            spin = Random.insideUnitSphere * 180f,
            radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 0.8f,
        });
    }

    void Remove(int i)
    {
        Object.Destroy(items[i].tr.gameObject);
        items.RemoveAt(i);
    }

    public override void Cleanup()
    {
        items.Clear();
        base.Cleanup();
    }
}