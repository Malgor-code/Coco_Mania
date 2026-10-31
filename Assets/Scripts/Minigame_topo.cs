using UnityEngine;

public class Minigame_Topo : MinigameBase
{
    public override string Name => Tr.T("¡Topo-Coco!", "Coco-Mole!", "Toupeira-Coco!", "Taupe-Coco !", "Kokos-Maulwurf!", "ココナッツもぐらたたき！");
    public override float Duration => 20f;
    public override string Controls => Tr.T("Clic en los cocos que asoman. Dorado = x3. ¡No le pegues al podrido (morado)!", "Click the coconuts as they pop up. Golden = x3. Don't hit the rotten one (purple)!", "Clique nos cocos que aparecem. Dourado = x3. Não bata no podre (roxo)!", "Clique sur les noix de coco qui sortent. Doré = x3. Ne tape pas la pourrie (violette) !", "Klicke auf die Kokosnüsse, die auftauchen. Golden = x3. Schlage nicht auf die faule (lila)!", "出てくるココナッツをクリック。金色は3倍！腐ったもの（紫）は叩かないで！");

    public override string[] Intros => new[] { IntrosES, IntrosEN, IntrosPT, IntrosFR, IntrosDE, IntrosJA }[Tr.Index];

    static readonly string[] IntrosEN =
    {
        "The coconuts heard the machete guy is coming and hid in holes. You have a hammer and zero shame. Smash 'em!",
        "Uncle Roberto swears this is 'a traditional harbor game'. Nobody believes him, but there's still a prize.",
        "The coconuts threw an underground party and you're the DJ... with a hammer. Don't let a single one escape!",
    };

    static readonly string[] IntrosPT =
    {
        "Os cocos souberam que o machetero está vindo e se esconderam em buracos. Você tem um martelo e zero vergonha. Vai!",
        "O tio Roberto jura que isso é 'um jogo tradicional do porto'. Ninguém acredita, mas mesmo assim tem prêmio.",
        "Os cocos organizaram uma festa subterrânea e você é o DJ... com martelo. Não deixe nenhum escapar!",
    };

    static readonly string[] IntrosFR =
    {
        "Les noix de coco ont appris que le machettiste arrive et se sont cachées dans des trous. Toi, tu as un marteau et aucune honte. Vas-y !",
        "Oncle Roberto jure que c'est « un jeu traditionnel du port ». Personne ne le croit, mais il y a quand même un prix.",
        "Les noix de coco ont organisé une fête souterraine et tu es le DJ... avec un marteau. Qu'aucune ne s'échappe !",
    };

    static readonly string[] IntrosDE =
    {
        "Die Kokosnüsse haben gehört, dass der Machetenmann kommt, und sich in Löchern versteckt. Du hast einen Hammer und keinerlei Skrupel. Los!",
        "Onkel Roberto schwört, das sei 'ein traditionelles Hafenspiel'. Niemand glaubt ihm, aber es gibt trotzdem einen Preis.",
        "Die Kokosnüsse haben eine Untergrundparty organisiert und du bist der DJ... mit Hammer. Lass keine entkommen!",
    };

    static readonly string[] IntrosJA =
    {
        "マチェーテ使いが来ると聞きつけたココナッツたちが、穴に隠れた。きみにはハンマーと、恥じらいゼロの心がある。いけ！",
        "ロベルトおじさんは、これは「港の伝統的な遊びだ」と言い張っている。誰も信じていないが、賞品は出る。",
        "ココナッツたちが地下パーティーを開いていて、きみはDJだ……ただしハンマー付き。一個も逃がすな！",
    };

    static readonly string[] IntrosES =
    {
        "Los cocos se enteraron de que viene el machetero y se escondieron en hoyos. Tú tienes un martillo y cero vergüenza. ¡Dale!",
        "El tío Roberto jura que esto es 'un juego tradicional del puerto'. Nadie le cree, pero igual hay premio.",
        "Los cocos organizaron una fiesta subterránea y tú eres el DJ... con martillo. ¡Que no se escape ninguno!",
    };

    public override bool Is3D => true;

    const float PopTime = 0.16f;
    const float DieTime = 0.18f;
    const float SwingTime = 0.22f;
    const float SwingAngle = -60f;
    const float StrikeFrac = 0.35f;
    const float MinHitRadiusPx = 48f;
    const float HitForgiveness = 1.25f;

    static readonly Color Gold = new Color(1f, 0.85f, 0.2f);
    static readonly Color Rotten = new Color(0.45f, 0.2f, 0.55f);
    static readonly Color Dirt = new Color(0.42f, 0.28f, 0.16f);
    static readonly Color Juice = new Color(0.92f, 0.97f, 1f);
    static readonly Color Bad = new Color(1f, 0.3f, 0.3f);

    class Hole
    {
        public Transform spot;
        public GameObject[] variants = new GameObject[3];
        public Vector3[] scale0 = new Vector3[3];
        public float[] shownOff = new float[3];
        public float[] depth = new float[3];
        public float life, maxLife, dying, sparkle, seed;
        public int type;
        public bool active;
    }

    readonly MinigameFX fx = new MinigameFX();

    MinigameStage.TopoSetup S;
    Hole[] holes;
    float spawnTimer, spawnInterval, cocoLife;
    int points;
    float unit;

    GameObject hammer;
    Quaternion hammerBaseRot;
    Plane groundPlane;
    float hammerLift;
    float swingT = 1f;
    Vector3 hammerPos, hammerTarget;
    bool cursorHidden, prevCursor;

    public override string Hud => Tr.T("Puntos: ", "Points: ", "Pontos: ", "Points : ", "Punkte: ", "ポイント：") + points;

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
        swingT = 1f;

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
            var h = new Hole { spot = S.holesRoot.GetChild(i), seed = Random.value * 10f };
            for (int k = 0; k < 3; k++)
            {
                GameObject prefab = S.cocoPrefab;
                bool tint = k > 0;
                if (k == 1 && S.cocoGoldPrefab != null) { prefab = S.cocoGoldPrefab; tint = false; }
                if (k == 2 && S.cocoRottenPrefab != null) { prefab = S.cocoRottenPrefab; tint = false; }

                var go = MinigameFX.Prep(Spawn(prefab, h.spot.position, h.spot.rotation), h.spot.position, h.spot.rotation, "Topo: coco");
                if (tint) MinigameFX.Tint(go, k == 1 ? Gold : Rotten);
                go.transform.SetPositionAndRotation(h.spot.position, h.spot.rotation);
                Bounds b = MinigameFX.GetBounds(go);
                MinigameFX.ProjectExtents(b, go.transform.position, h.spot.up, out float mn, out float mx);
                float depth = Mathf.Max(0.01f, mx - mn);
                h.depth[k] = depth;
                bool sane = Mathf.Abs(mn) <= depth * 2f;
                h.shownOff[k] = (S.alignBaseToHole && sane) ? -mn : 0f;
                if (S.alignBaseToHole && !sane)
                    Debug.LogWarning("Minigame_Topo: la base del coco esta muy lejos de su pivote (hoyo " + i + "). Se usa el pivote del prefab.");

                h.variants[k] = go;
                h.scale0[k] = go.transform.localScale;
                go.SetActive(false);
            }
            holes[i] = h;
        }

        float spacing = holes.Length > 1 ? Vector3.Distance(holes[0].spot.position, holes[1].spot.position) : 1f;
        unit = Mathf.Max(0.05f, spacing * 0.3f);
        groundPlane = new Plane(Vector3.up, holes[0].spot.position);
        hammerLift = spacing * 0.35f;
        if (S.hammerPrefab != null)
        {
            hammerPos = hammerTarget = holes[0].spot.position + Vector3.up * hammerLift;
            hammer = MinigameFX.Prep(Spawn(S.hammerPrefab, hammerPos, S.hammerPrefab.transform.rotation), hammerPos, S.hammerPrefab.transform.rotation, "Topo: martillo");
            hammerBaseRot = hammer.transform.rotation;
            prevCursor = Cursor.visible;
            Cursor.visible = false;
            cursorHidden = true;
        }
    }

    Vector3 HolePos(Hole h, int type, float v)
    {
        float off = h.shownOff[type] - h.depth[type] * (1f - v);
        return h.spot.position + h.spot.up * off;
    }

    void Apply(Hole h, int type, float v, float squash, float wobbleDeg)
    {
        Transform tr = h.variants[type].transform;
        Vector3 s = h.scale0[type];
        if (S.riseFromHole) tr.position = HolePos(h, type, v);
        else { tr.position = HolePos(h, type, 1f); s *= Mathf.Max(0f, v); }
        if (squash > 0f) s = new Vector3(s.x * (1f + 0.3f * squash), s.y * (1f - 0.5f * squash), s.z * (1f + 0.3f * squash));
        tr.localScale = s;
        tr.rotation = h.spot.rotation * Quaternion.AngleAxis(wobbleDeg, Vector3.forward);
    }

    protected override void Tick(float dt)
    {
        if (holes == null) return;
        fx.Tick(dt);
        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        if (Input.GetMouseButtonDown(0))
        {
            swingT = 0f;
            MinigameFX.PlaySfx("topo_swing");
            bool hit = cam != null && TryHit(cam);
            if (!hit) Miss(cam);
        }
        UpdateHammer(dt, cam);

        spawnTimer -= dt;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnCoco();
        }

        foreach (var h in holes)
        {
            if (h.dying > 0f)
            {
                h.dying -= dt;
                if (h.dying <= 0f) { h.variants[h.type].SetActive(false); continue; }
                float k = 1f - Mathf.Clamp01(h.dying / DieTime);
                Apply(h, h.type, 1f - MinigameFX.EaseInCubic(k), Mathf.Clamp01(k * 5f), 0f);
                continue;
            }
            if (!h.active) continue;

            h.life -= dt;
            if (h.life <= 0f) { HideHole(h); continue; }

            float age = h.maxLife - h.life;
            float appear = Mathf.Clamp01(age / PopTime);
            float leave = Mathf.Clamp01(h.life / PopTime);
            float v = MinigameFX.EaseOutBack(appear) * Mathf.SmoothStep(0f, 1f, leave);
            bool urgent = appear >= 1f && h.life < h.maxLife * 0.3f;
            float wob = Mathf.Sin((Time.time + h.seed) * (urgent ? 38f : 9f)) * (urgent ? 7f : 3f) * (h.type == 2 ? 1.6f : 1f);
            Apply(h, h.type, v, 0f, wob);
            if (h.type == 1 && appear >= 1f)
            {
                h.sparkle -= dt;
                if (h.sparkle <= 0f)
                {
                    h.sparkle = 0.1f;
                    Vector3 p = h.variants[1].transform.position + h.spot.up * (h.depth[1] * Random.Range(0.2f, 1f))
                                + Random.insideUnitSphere * (h.depth[1] * 0.35f);
                    fx.Burst(p, Gold, 1, h.depth[1], 0.8f, 0.07f, 0.5f, -0.5f);
                }
            }
        }
    }

    void UpdateHammer(float dt, Camera cam)
    {
        if (swingT < 1f) swingT = Mathf.Min(1f, swingT + dt / SwingTime);
        if (hammer == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(ray, out float enter))
            hammerTarget = ray.GetPoint(enter) + Vector3.up * hammerLift;
        hammerPos = Vector3.Lerp(hammerPos, hammerTarget, 1f - Mathf.Exp(-40f * dt));
        float pose = 0f;
        if (swingT < 1f)
        {
            pose = swingT < StrikeFrac
                ? MinigameFX.EaseOutCubic(swingT / StrikeFrac)
                : Mathf.Pow(1f - (swingT - StrikeFrac) / (1f - StrikeFrac), 2f);
        }
        hammer.transform.position = hammerPos + Vector3.down * (hammerLift * 0.5f * pose);
        hammer.transform.rotation = Quaternion.AngleAxis(SwingAngle * pose, cam.transform.right) * hammerBaseRot;
    }

    bool TryHit(Camera cam)
    {
        Vector2 mouse = Input.mousePosition;
        int best = -1;
        float bestD = float.MaxValue;
        for (int i = 0; i < holes.Length; i++)
        {
            Hole h = holes[i];
            if (!h.active) continue;

            Bounds b = MinigameFX.GetBounds(h.variants[h.type]);
            Vector3 sp = cam.WorldToScreenPoint(b.center);
            if (sp.z <= 0f) continue;
            float worldR = Mathf.Max(b.extents.x, b.extents.z, b.extents.y * 0.5f);
            Vector3 spEdge = cam.WorldToScreenPoint(b.center + cam.transform.right * worldR);
            float r = Mathf.Max(Mathf.Abs(spEdge.x - sp.x) * HitForgiveness, MinHitRadiusPx);
            float d = Vector2.Distance(mouse, new Vector2(sp.x, sp.y));
            if (d <= r && d < bestD) { best = i; bestD = d; }
        }
        if (best < 0) return false;
        Hit(holes[best], cam);
        return true;
    }

    void Miss(Camera cam)
    {
        fx.Burst(hammerTarget - Vector3.up * hammerLift, Dirt, 5, unit, 2.5f, 0.1f, 0.4f, 8f, Vector3.up, 0.8f);
        MinigameFX.PlaySfx("topo_miss");
    }

    void Hit(Hole h, Camera cam)
    {
        int type = h.type;
        float u = h.depth[type];
        Vector3 c = MinigameFX.GetBounds(h.variants[type]).center;

        points += type == 0 ? 1 : type == 1 ? 3 : -2;

        string txt; Color tcol, pcol; float shake; int tsize;
        if (type == 0) { txt = "+1"; tcol = Color.white; pcol = Juice; shake = 0.12f; tsize = 52; MinigameFX.PlaySfx("topo_hit"); }
        else if (type == 1)
        {
            txt = "+3"; tcol = Gold; pcol = Gold; shake = 0.3f; tsize = 72;
            fx.Flash(root, new Color(1f, 0.85f, 0.2f, 0.18f), 0.2f);
            fx.Burst(c, Color.white, 8, u, 5f, 0.08f, 0.6f, 6f);
            MinigameFX.PlaySfx("topo_gold");
        }
        else
        {
            txt = "-2"; tcol = Bad; pcol = Rotten; shake = 0.6f; tsize = 64;
            fx.Flash(root, new Color(1f, 0.15f, 0.15f, 0.28f), 0.25f);
            MinigameFX.PlaySfx("topo_rotten");
        }

        fx.Burst(c, pcol, type == 1 ? 14 : 10, u, 4f, 0.14f, 0.65f, 9f, h.spot.up * 0.8f);
        fx.Burst(h.spot.position, Dirt, 6, u, 3f, 0.1f, 0.5f, 10f, h.spot.up);
        Stage.Shake(shake, 0.2f);

        if (MinigameFX.WorldToUI(root, cam, c + h.spot.up * (u * 0.5f), out Vector2 ui))
            fx.Pop(root, ui + Vector2.up * 20f, txt, tcol, tsize);

        h.active = false;
        h.dying = DieTime;
    }

    void HideHole(Hole h)
    {
        h.active = false;
        h.dying = 0f;
        h.variants[h.type].SetActive(false);
    }

    void SpawnCoco()
    {
        int start = Random.Range(0, holes.Length);
        for (int k = 0; k < holes.Length; k++)
        {
            Hole h = holes[(start + k) % holes.Length];
            if (h.active || h.dying > 0f) continue;

            float rottenChance = Mathf.Clamp((diff - 1f) * 0.1f, 0f, 0.3f);
            float r = Random.value;
            h.type = r < rottenChance ? 2 : r < rottenChance + 0.1f ? 1 : 0;
            h.life = h.maxLife = cocoLife;
            h.sparkle = 0f;
            h.active = true;

            Apply(h, h.type, 0f, 0f, 0f);
            h.variants[h.type].SetActive(true);
            fx.Burst(h.spot.position, Dirt, 4, unit, 2.5f, 0.1f, 0.4f, 9f, h.spot.up, 0.7f);
            return;
        }
    }

    public override void Cleanup()
    {
        fx.Clear();
        if (cursorHidden) { Cursor.visible = prevCursor; cursorHidden = false; }
        base.Cleanup();
    }
}