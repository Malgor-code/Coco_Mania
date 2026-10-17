using UnityEngine;

public class Minigame_Topo : MinigameBase
{
    public override string Name => "¡Topo-Coco!";
    public override float Duration => 20f;
    public override string Controls => "Clic en los cocos que asoman. Dorado = x3. ¡No le pegues al podrido (morado)!";

    public override string[] Intros => new[]
    {
        "Los cocos se enteraron de que viene el machetero y se escondieron en hoyos. Tú tienes un martillo y cero vergüenza. ¡Dale!",
        "El tío Roberto jura que esto es 'un juego tradicional del puerto'. Nadie le cree, pero igual hay premio.",
        "Los cocos organizaron una fiesta subterránea y tú eres el DJ... con martillo. ¡Que no se escape ninguno!",
    };

    public override bool Is3D => true;
    const float PopTime = 0.12f;  
    const float SwingTime = 0.15f; 
    const float SwingAngle = -55f;  
    const float MinHitRadiusPx = 40f;

    static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
    static readonly Color Rotten = new Color(0.45f, 0.2f, 0.55f);

    class Hole
    {
        public Transform spot;
        public GameObject[] variants = new GameObject[3];
        public Vector3[] scale0 = new Vector3[3];
        public float height;
        public float life, maxLife;
        public int type;
        public bool active;
    }

    MinigameStage.TopoSetup S;
    Hole[] holes;
    float spawnTimer;
    float spawnInterval;
    float cocoLife;
    int points;

    GameObject hammer;
    Quaternion hammerBaseRot;
    Plane groundPlane;
    float hammerLift;
    float hammerSwing;

    public override string Hud => "Puntos: " + points;

    public override float Score01
    {
        get
        {
            float expected = Duration / spawnInterval;
            return Mathf.Clamp01(points / (expected * 0.8f));
        }
    }

    protected override void Setup()
    {
        points = 0;
        spawnInterval = Mathf.Max(0.3f, 0.9f / Scale(0.35f));
        cocoLife = Mathf.Max(0.5f, 1.3f / Scale(0.25f));
        spawnTimer = 0.5f;
        hammerSwing = 0f;

        S = Stage != null ? Stage.topo : null;
        if (S == null || !S.IsReady)
        {
            Debug.LogError("Minigame_Topo: falta configurar MinigameStage > Topo (prefab de coco, camera point y holes root). Se omite el minijuego.");
            Finished = true;
            return;
        }

        Stage.UseCamera(S.cameraPoint);

        int n = S.holesRoot.childCount;
        holes = new Hole[n];
        for (int i = 0; i < n; i++)
        {
            var h = new Hole { spot = S.holesRoot.GetChild(i) };
            for (int k = 0; k < 3; k++)
            {
                GameObject prefab = S.cocoPrefab;
                bool tint = k > 0;
                if (k == 1 && S.cocoGoldPrefab != null) { prefab = S.cocoGoldPrefab; tint = false; }
                if (k == 2 && S.cocoRottenPrefab != null) { prefab = S.cocoRottenPrefab; tint = false; }

                var go = Spawn(prefab, h.spot.position, h.spot.rotation);
                if (k == 0) h.height = Mathf.Max(0.01f, GetBounds(go).size.y); 
                if (tint) Tint(go, k == 1 ? Gold : Rotten);
                h.variants[k] = go;
                h.scale0[k] = go.transform.localScale;
                go.SetActive(false);
            }
            holes[i] = h;
        }

        groundPlane = new Plane(Vector3.up, holes[0].spot.position);
        float spacing = holes.Length > 1 ? Vector3.Distance(holes[0].spot.position, holes[1].spot.position) : 1f;
        hammerLift = spacing * 0.35f;
        if (S.hammerPrefab != null)
        {
            hammer = Spawn(S.hammerPrefab, holes[0].spot.position + Vector3.up * hammerLift, S.hammerPrefab.transform.rotation);
            hammerBaseRot = hammer.transform.rotation;
        }
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

        // Martillo
        if (Input.GetMouseButtonDown(0))
        {
            hammerSwing = SwingTime;
            if (cam != null) TryHit(cam);
        }
        hammerSwing = Mathf.Max(0f, hammerSwing - dt);
        if (hammer != null && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.Raycast(ray, out float enter))
                hammer.transform.position = ray.GetPoint(enter) + Vector3.up * hammerLift;
            float ang = hammerSwing > 0f ? SwingAngle : 0f;
            hammer.transform.rotation = Quaternion.AngleAxis(ang, cam.transform.right) * hammerBaseRot;
        }

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnCoco();
        }

        foreach (var h in holes)
        {
            if (!h.active) continue;
            h.life -= dt;
            if (h.life <= 0f) { HideHole(h); continue; }

            float age = h.maxLife - h.life;
            float up = Mathf.Min(Mathf.Clamp01(age / PopTime), Mathf.Clamp01(h.life / PopTime));
            var tr = h.variants[h.type].transform;
            if (S.riseFromHole)
            {
                tr.position = h.spot.position - h.spot.up * (h.height * (1f - up));
            }
            else
            {
                tr.position = h.spot.position;
                tr.localScale = h.scale0[h.type] * up;
            }
        }
    }
    void TryHit(Camera cam)
    {
        Vector2 mouse = Input.mousePosition;
        int best = -1;
        float bestD = float.MaxValue;
        for (int i = 0; i < holes.Length; i++)
        {
            Hole h = holes[i];
            if (!h.active) continue;

            Bounds b = GetBounds(h.variants[h.type]);
            Vector3 sp = cam.WorldToScreenPoint(b.center);
            if (sp.z <= 0f) continue;
            float worldR = Mathf.Max(b.extents.x, b.extents.z, b.extents.y * 0.5f);
            Vector3 spEdge = cam.WorldToScreenPoint(b.center + cam.transform.right * worldR);
            float r = Mathf.Max(Mathf.Abs(spEdge.x - sp.x), MinHitRadiusPx);
            float d = Vector2.Distance(mouse, new Vector2(sp.x, sp.y));
            if (d <= r && d < bestD) { best = i; bestD = d; }
        }
        if (best >= 0) Hit(best);
    }

    void Hit(int i)
    {
        Hole h = holes[i];
        if (!h.active) return;
        points += h.type == 0 ? 1 : h.type == 1 ? 3 : -2;
        HideHole(h);
    }

    void HideHole(Hole h)
    {
        h.active = false;
        h.variants[h.type].SetActive(false);
    }

    void SpawnCoco()
    {
        int start = Random.Range(0, holes.Length);
        for (int k = 0; k < holes.Length; k++)
        {
            Hole h = holes[(start + k) % holes.Length];
            if (h.active) continue;

            float rottenChance = Mathf.Clamp((diff - 1f) * 0.1f, 0f, 0.3f);
            float r = Random.value;
            h.type = r < rottenChance ? 2 : r < rottenChance + 0.1f ? 1 : 0;
            h.life = h.maxLife = cocoLife;
            h.active = true;

            var go = h.variants[h.type];
            go.transform.localScale = h.scale0[h.type];
            go.transform.SetPositionAndRotation(
                h.spot.position - (S.riseFromHole ? h.spot.up * h.height : Vector3.zero), h.spot.rotation);
            go.SetActive(true);
            return;
        }
    }
}