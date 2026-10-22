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
    const float SpawnInTime = 0.18f;

    static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
    static readonly Color StoneColor = new Color(0.45f, 0.45f, 0.5f);
    static readonly Color Juice = new Color(0.92f, 0.97f, 1f);
    static readonly Color Dust = new Color(0.7f, 0.62f, 0.5f);
    static readonly Color Bad = new Color(1f, 0.3f, 0.3f);

    class Item
    {
        public Transform tr;
        public int type;
        public float speed, accel;
        public Vector3 spin;
        public Vector3 scale0;
        public float radius;
        public float age;
        public float trail;
    }

    readonly MinigameFX fx = new MinigameFX();

    MinigameStage.AtrapaSetup S;
    readonly List<Item> items = new List<Item>();

    Transform basket;
    Vector3 basketScale0;
    Quaternion basketBaseRot;
    Vector3 trackA, trackB, trackDir;
    float trackLen;
    Plane trackPlane;
    float basketT = 0.5f, targetT = 0.5f;
    float topOffset, botOffset, basketHalfW, dropDistance, unit;

    float tilt;
    float punchT = 1f;
    float tintT;
    bool basketTinted;

    float spawnTimer, spawnInterval, fallSpeed;
    float lastU = 0.5f;
    int points, streak;

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
        streak = 0;
        items.Clear();
        spawnInterval = Mathf.Max(0.25f, 0.55f / Scale(0.3f));
        spawnTimer = 0.3f;
        basketT = targetT = 0.5f;
        punchT = 1f;
        tilt = 0f;

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

        var bGO = MinigameFX.Prep(Spawn(S.basketPrefab, Vector3.Lerp(trackA, trackB, 0.5f), S.basketLeft.rotation), Vector3.Lerp(trackA, trackB, 0.5f), S.basketLeft.rotation, "Atrapa: canasta");
        basket = bGO.transform;
        basketScale0 = basket.localScale;
        basketBaseRot = basket.rotation;
        Bounds b = MinigameFX.GetBounds(bGO);
        topOffset = b.max.y - basket.position.y;
        botOffset = b.min.y - basket.position.y;
        basketHalfW = Mathf.Max(0.05f, Mathf.Abs(trackDir.x) * b.extents.x + Mathf.Abs(trackDir.z) * b.extents.z);
        unit = Mathf.Max(0.05f, b.size.y);
    }

    protected override void Tick(float dt)
    {
        if (basket == null) return;
        fx.Tick(dt);

        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        float prevT = basketT;
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

        UpdateBasketFeel(dt, cam, (basketT - prevT) / Mathf.Max(dt, 0.0001f));

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnItem();
        }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            Item it = items[i];
            it.age += dt;
            float prevY = it.tr.position.y;
            it.speed += it.accel * dt;
            it.tr.position += Vector3.down * (it.speed * dt);
            it.tr.Rotate(it.spin * dt, Space.Self);
            it.tr.localScale = it.scale0 * MinigameFX.EaseOutBack(it.age / SpawnInTime);

            Vector3 p = it.tr.position;
            if (it.type == 1)
            {
                it.trail -= dt;
                if (it.trail <= 0f)
                {
                    it.trail = 0.06f;
                    fx.Burst(p, Gold, 1, it.radius * 2f, 0.6f, 0.08f, 0.45f, 2f);
                }
            }

            float dx = Mathf.Abs(Vector3.Dot(p - bp, trackDir));
            bool inColumn = dx < basketHalfW + it.radius * 0.3f;
            bool caught = inColumn
                          && p.y <= bp.y + topOffset + it.radius
                          && prevY >= bp.y + botOffset;
            if (caught)
            {
                OnCaught(it, cam, bp);
                Remove(i);
            }
            else if (p.y < bp.y + botOffset - dropDistance * 0.12f)
            {
                fx.Burst(p, it.type == 2 ? StoneColor : Dust, 5, it.radius * 2f, 2.5f, 0.12f, 0.4f, 8f, Vector3.up, 0.8f);
                if (it.type != 2) streak = 0;
                Remove(i);
            }
        }
    }

    void UpdateBasketFeel(float dt, Camera cam, float velT)
    {
        float targetTilt = 0f;
        if (cam != null)
        {
            float screenDir = Mathf.Sign(Vector3.Dot(trackDir, cam.transform.right));
            targetTilt = Mathf.Clamp(-velT * screenDir * 12f, -15f, 15f);
        }
        tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-14f * dt));
        Vector3 axis = cam != null ? cam.transform.forward : Vector3.forward;
        basket.rotation = Quaternion.AngleAxis(tilt, axis) * basketBaseRot;
        float sq = 0f;
        if (punchT < 1f)
        {
            punchT = Mathf.Min(1f, punchT + dt / 0.35f);
            sq = MinigameFX.Punch(punchT);
        }
        basket.localScale = new Vector3(basketScale0.x * (1f + 0.18f * sq), basketScale0.y * (1f - 0.22f * sq), basketScale0.z * (1f + 0.18f * sq));

        if (basketTinted)
        {
            tintT -= dt;
            if (tintT <= 0f) { MinigameFX.ClearTint(basket.gameObject); basketTinted = false; }
        }
    }

    void OnCaught(Item it, Camera cam, Vector3 bp)
    {
        Vector3 p = it.tr.position;
        punchT = 0f;
        string txt; Color tcol; int size;

        if (it.type == 0)
        {
            points += 1; streak++;
            txt = "+1"; tcol = Color.white; size = 50;
            fx.Burst(p, Juice, 8, it.radius * 2f, 3.5f, 0.14f, 0.55f, 9f, Vector3.up, 0.9f);
            Stage.Shake(0.08f, 0.12f);
            MinigameFX.PlaySfx("atrapa_catch");
        }
        else if (it.type == 1)
        {
            points += 3; streak++;
            txt = "+3"; tcol = Gold; size = 72;
            fx.Burst(p, Gold, 16, it.radius * 2f, 4.5f, 0.12f, 0.7f, 7f, Vector3.up, 1f);
            fx.Flash(root, new Color(1f, 0.85f, 0.2f, 0.16f), 0.2f);
            MinigameFX.Tint(basket.gameObject, new Color(1.8f, 1.6f, 0.8f));
            basketTinted = true; tintT = 0.15f;
            Stage.Shake(0.25f, 0.2f);
            MinigameFX.PlaySfx("atrapa_gold");
        }
        else
        {
            points -= 2; streak = 0;
            txt = "-2"; tcol = Bad; size = 64;
            fx.Burst(p, StoneColor, 10, it.radius * 2f, 4f, 0.14f, 0.5f, 10f, Vector3.up, 1f);
            fx.Flash(root, new Color(1f, 0.15f, 0.15f, 0.28f), 0.25f);
            MinigameFX.Tint(basket.gameObject, new Color(1.6f, 0.45f, 0.45f));
            basketTinted = true; tintT = 0.25f;
            Stage.Shake(0.7f, 0.3f);
            MinigameFX.PlaySfx("atrapa_stone");
        }

        if (MinigameFX.WorldToUI(root, cam, bp + Vector3.up * topOffset, out Vector2 ui))
        {
            fx.Pop(root, ui + Vector2.up * 40f, txt, tcol, size);
            if (streak > 0 && streak % 5 == 0)
                fx.Pop(root, ui + Vector2.up * 150f, "¡Racha x" + streak + "!", Gold, 44, 70f, 1f);
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
        float u = Random.value;
        if (Random.value < 0.3f) u = Mathf.Clamp01(basketT + Random.Range(-0.12f, 0.12f));
        if (Mathf.Abs(u - lastU) < 0.1f) u = Mathf.Repeat(u + 0.3f, 1f);
        lastU = u;

        Vector3 pos = Vector3.Lerp(S.spawnLeft.position, S.spawnRight.position, u);
        var go = MinigameFX.Prep(Spawn(prefab, pos, prefab.transform.rotation), pos, prefab.transform.rotation, "Atrapa: objeto");
        if (tint) MinigameFX.Tint(go, c);

        Bounds b = MinigameFX.GetBounds(go);
        float spd = fallSpeed * Random.Range(0.85f, 1.2f);
        var it = new Item
        {
            tr = go.transform,
            type = type,
            speed = spd * 0.7f,
            accel = spd * 0.5f,
            spin = Random.insideUnitSphere * 180f,
            scale0 = go.transform.localScale,
            radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 0.8f,
        };
        go.transform.localScale = Vector3.zero;
        items.Add(it);
    }

    void Remove(int i)
    {
        Object.Destroy(items[i].tr.gameObject);
        items.RemoveAt(i);
    }

    public override void Cleanup()
    {
        items.Clear();
        fx.Clear();
        base.Cleanup();
    }
}