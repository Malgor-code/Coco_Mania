using UnityEngine;
using UnityEngine.UI;

public class Minigame_Machetazo : MinigameBase
{
    public override string Name => Tr.T("¡Machetazo Perfecto!", "Perfect Machete Strike!", "Golpe de Machete Perfeito!", "Coup de machette parfait !", "Perfekter Machetenhieb!", "完璧なマチェーテ斬り！");
    public override string Controls => Tr.T("Clic justo cuando el anillo amarillo llegue al círculo gris. 8 cocos, ¡precisión!", "Click right when the yellow ring reaches the gray circle. 8 coconuts, precision!", "Clique exatamente quando o anel amarelo chegar ao círculo cinza. 8 cocos, precisão!", "Clique pile quand l'anneau jaune atteint le cercle gris. 8 noix de coco, de la précision !", "Klicke genau, wenn der gelbe Ring den grauen Kreis erreicht. 8 Kokosnüsse, Präzision!", "黄色いリングが灰色の円に重なった瞬間にクリック。ココナッツ8個、狙いは正確に！");

    public override string[] Intros => new[] { IntrosES, IntrosEN, IntrosPT, IntrosFR, IntrosDE, IntrosJA }[Tr.Index];

    static readonly string[] IntrosEN =
    {
        "The harbor's machete master challenges you: 'A good cut isn't made fast, it's made at the exact moment.' Then he left to eat tacos.",
        "A gourmet chef needs coconuts cut with surgical precision. Pays well. Asks few questions.",
        "They say the perfect machete strike makes the coconut sigh with relief. Today we find out.",
    };

    static readonly string[] IntrosPT =
    {
        "O mestre do machete do porto te desafia: 'Um bom corte não se faz rápido, se faz no momento exato'. Depois foi comer tacos.",
        "Um chef gourmet precisa de cocos cortados com precisão cirúrgica. Paga bem. Pergunta pouco.",
        "Dizem que o golpe perfeito faz o coco suspirar de alívio. Hoje vamos comprovar.",
    };

    static readonly string[] IntrosFR =
    {
        "Le maître machettiste du port te défie : « Une bonne coupe ne se fait pas vite, elle se fait au moment exact. » Puis il est parti manger des tacos.",
        "Un chef gastronomique a besoin de noix de coco coupées avec une précision chirurgicale. Il paie bien. Il pose peu de questions.",
        "On dit que la coupe parfaite fait soupirer la noix de coco de soulagement. Aujourd'hui, on vérifie.",
    };

    static readonly string[] IntrosDE =
    {
        "Der Machetenmeister des Hafens fordert dich heraus: 'Ein guter Schnitt ist nicht schnell, er kommt im genau richtigen Moment.' Dann ging er Tacos essen.",
        "Ein Gourmet-Koch braucht Kokosnüsse, die mit chirurgischer Präzision geschnitten sind. Zahlt gut. Fragt wenig.",
        "Man sagt, der perfekte Machetenhieb lässt die Kokosnuss erleichtert aufseufzen. Heute überprüfen wir das.",
    };

    static readonly string[] IntrosJA =
    {
        "港のマチェーテの達人が挑戦してくる。「いい斬りは速さじゃない、ぴったりのタイミングで決まるんだ」。そう言って、タコスを食べに行ってしまった。",
        "グルメなシェフが、外科手術のような精度で切ったココナッツを求めている。報酬は高い。詮索は少ない。",
        "完璧な一撃を受けたココナッツは、ほっとして息をつくと言われている。今日、それを確かめよう。",
    };

    static readonly string[] IntrosES =
    {
        "El maestro machetero del puerto te reta: 'Un buen corte no se hace rápido, se hace en el momento exacto'. Luego se fue a comer tacos.",
        "Un chef gourmet necesita cocos cortados con precisión quirúrgica. Paga bien. Pregunta poco.",
        "Dicen que el machetazo perfecto hace que el coco suspire de alivio. Hoy lo comprobamos.",
    };

    public override bool Is3D => true;

    const int Rounds = 8;
    const float TargetSize = 170f;
    const float StartSize = 520f;
    const float EndSize = 60f;
    const float Tolerance = 120f;
    const float PerfectScore = 0.88f;
    const float FeedbackTime = 0.85f;
    const float MacheteSwingTime = 0.16f;
    const float ReactTime = FeedbackTime - MacheteSwingTime;
    const float PopInTime = 0.3f;
    const float MacheteStartAngle = 60f;
    const float MacheteEndAngle = -40f;

    static readonly Color Yellow = new Color(1f, 0.85f, 0.2f, 0.95f);
    static readonly Color Green = new Color(0.45f, 1f, 0.45f, 0.95f);
    static readonly Color Juice = new Color(0.92f, 0.97f, 1f);
    static readonly Color Gold = new Color(1f, 0.9f, 0.3f);

    enum Step { Shrinking, Feedback }

    readonly MinigameFX fx = new MinigameFX();

    Step step;
    int round, perfectStreak;
    float totalScore;
    float shrinkTime, t;
    float feedbackElapsed, inputCooldown;

    MinigameStage.MachetazoSetup S;
    Transform[] spots;
    int allowed, lastSpot = -1;

    Transform coco;
    Vector3 cocoScale0, cocoLocalCenter, cocoBasePos, cocoCenterWorld;
    Quaternion cocoBaseRot;
    float cocoHeight;
    bool swung, lastGood, impactDone;
    float lastS;
    float flashT;
    float uiFxT = 1f;

    GameObject machete;
    Quaternion macheteBaseRot;

    Image ring, target, slash, pulse;
    Vector2 cocoUI;

    static Sprite ringSprite;

    public override string Hud => Tr.T("Coco ", "Coconut ", "Coco ", "Noix de coco ", "Kokosnuss ", "ココナッツ ") + Mathf.Min(round + 1, Rounds) + "/" + Rounds;
    public override float Score01 => Mathf.Clamp01(totalScore / Rounds);

    protected override void Setup()
    {
        round = 0;
        perfectStreak = 0;
        totalScore = 0f;
        lastSpot = -1;
        shrinkTime = Mathf.Max(0.7f, 1.5f / Scale(0.25f));

        S = Stage != null ? Stage.machetazo : null;
        if (S == null || !S.IsReady)
        {
            Debug.LogError("Minigame_Machetazo: falta configurar MinigameStage > Machetazo (prefab de coco, camera point y spots root). Se omite el minijuego.");
            Finished = true;
            return;
        }

        Stage.UseCamera(S.cameraPoint);
        int n = S.spotsRoot.childCount;
        spots = new Transform[n];
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            spots[i] = S.spotsRoot.GetChild(i);
            centroid += spots[i].position;
        }
        centroid /= n;
        System.Array.Sort(spots, (a, b) =>
            (a.position - centroid).sqrMagnitude.CompareTo((b.position - centroid).sqrMagnitude));
        allowed = Mathf.Clamp(Mathf.CeilToInt(n * Mathf.Lerp(0.4f, 1f, Mathf.InverseLerp(1f, 3.5f, diff))), 1, n);

        var cGO = MinigameFX.Prep(Spawn(S.cocoPrefab, spots[0].position, spots[0].rotation), spots[0].position, spots[0].rotation, "Machetazo: coco");
        coco = cGO.transform;
        cocoScale0 = coco.localScale;
        Bounds b = MinigameFX.GetBounds(cGO);
        cocoHeight = Mathf.Max(0.01f, b.size.y);
        cocoLocalCenter = coco.InverseTransformPoint(b.center);

        if (S.machetePrefab != null)
        {
            machete = MinigameFX.Prep(Spawn(S.machetePrefab, spots[0].position, S.machetePrefab.transform.rotation), spots[0].position, S.machetePrefab.transform.rotation, "Machetazo: machete");
            macheteBaseRot = machete.transform.rotation;
            machete.SetActive(false);
        }

        target = MiniUI.Img("Objetivo", root, new Color(0.85f, 0.9f, 0.85f, 0.95f), Vector2.zero, new Vector2(TargetSize, TargetSize));
        target.sprite = RingSprite();
        target.raycastTarget = false;
        ring = MiniUI.Img("Anillo", root, Yellow, Vector2.zero, new Vector2(StartSize, StartSize));
        ring.sprite = RingSprite();
        ring.raycastTarget = false;

        pulse = MiniUI.Img("Pulso", root, Color.white, Vector2.zero, new Vector2(TargetSize, TargetSize));
        pulse.sprite = RingSprite();
        pulse.raycastTarget = false;
        pulse.gameObject.SetActive(false);
        slash = MiniUI.Img("Tajo", root, new Color(1f, 1f, 1f, 0.9f), Vector2.zero, new Vector2(420f, 12f));
        slash.raycastTarget = false;
        slash.gameObject.SetActive(false);

        StartRound();
    }

    static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = size * 0.5f;
        float inner = r * 0.88f;
        var center = new Vector2(r - 0.5f, r - 0.5f);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01(r - d) * Mathf.Clamp01(d - inner);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return ringSprite;
    }

    void StartRound()
    {
        int pick = 0;
        if (allowed > 1)
        {
            do { pick = Random.Range(0, allowed); } while (pick == lastSpot);
        }
        lastSpot = pick;

        Transform spot = spots[pick];
        coco.SetPositionAndRotation(spot.position, spot.rotation);
        coco.localScale = cocoScale0;
        coco.gameObject.SetActive(true);
        MinigameFX.ClearTint(coco.gameObject);
        cocoBasePos = coco.position;
        cocoBaseRot = coco.rotation;
        cocoCenterWorld = coco.TransformPoint(cocoLocalCenter);
        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        Vector2 local = Vector2.zero;
        MinigameFX.WorldToUI(root, cam, cocoCenterWorld, out local);
        cocoUI = local;
        ring.rectTransform.anchoredPosition = local;
        target.rectTransform.anchoredPosition = local;
        ring.rectTransform.sizeDelta = new Vector2(StartSize, StartSize);
        ring.color = new Color(Yellow.r, Yellow.g, Yellow.b, 0f);
        ring.gameObject.SetActive(true);

        slash.gameObject.SetActive(false);
        pulse.gameObject.SetActive(false);
        uiFxT = 1f;
        flashT = 0f;
        swung = lastGood = impactDone = false;

        ApplyPopIn(0f);
        if (machete != null && cam != null) { machete.SetActive(true); PlaceMachete(cam, 0f, 0f); }

        t = 0f;
        inputCooldown = 0.25f;
        step = Step.Shrinking;
    }
    void ApplyPopIn(float k)
    {
        coco.position = cocoBasePos + Vector3.up * (cocoHeight * 1.6f * (1f - MinigameFX.EaseOutCubic(k)));
        coco.localScale = cocoScale0 * MinigameFX.EaseOutBack(k);
    }

    protected override void Tick(float dt)
    {
        if (coco == null) return;
        fx.Tick(dt);
        dt = fx.Gate(dt);
        inputCooldown -= dt;
        Camera cam = Stage != null ? Stage.ActiveCamera : null;

        if (step == Step.Shrinking)
        {
            t += dt;
            float k = Mathf.Clamp01(t / shrinkTime);
            float size = Mathf.Lerp(StartSize, EndSize, k);
            ring.rectTransform.sizeDelta = new Vector2(size, size);
            float closeness = 1f - Mathf.Clamp01(Mathf.Abs(size - TargetSize) / 50f);
            Color c = Color.Lerp(Yellow, Green, closeness);
            c.a = Mathf.Min(0.95f, t / 0.15f);
            ring.color = c;
            target.rectTransform.sizeDelta = Vector2.one * (TargetSize * (1f + 0.05f * closeness * Mathf.Sin(Time.time * 18f)));

            ApplyPopIn(t / PopInTime);
            if (machete != null && cam != null)
                PlaceMachete(cam, 0f, Mathf.Sin(Time.time * 6f) * cocoHeight * 0.04f);

            if (inputCooldown <= 0f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                Judge(size);
            else if (k >= 1f)
                Judge(-1f);
        }
        else
        {
            feedbackElapsed += dt;
            AnimateFeedback(dt, cam);

            if (feedbackElapsed >= FeedbackTime)
            {
                round++;
                if (round >= Rounds) Finished = true;
                else StartRound();
            }
        }
    }

    void Judge(float size)
    {
        float s = size < 0f ? 0f : Mathf.Clamp01(1f - Mathf.Abs(size - TargetSize) / Tolerance);
        totalScore += s;
        lastS = s;
        swung = size >= 0f;
        lastGood = swung && s > 0.25f;
        impactDone = false;
        feedbackElapsed = 0f;
        flashT = 0f;
        ring.gameObject.SetActive(false);
        target.rectTransform.sizeDelta = Vector2.one * TargetSize;
        coco.position = cocoBasePos;
        coco.localScale = cocoScale0;
        step = Step.Feedback;

        if (swung) MinigameFX.PlaySfx("machete_swing");
        else Impact();
    }

    void Impact()
    {
        impactDone = true;
        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        float u = cocoHeight;
        Vector3 bias = cam != null ? (cam.transform.up * 0.7f - cam.transform.right * 0.5f) : Vector3.up;

        string txt; Color col; int size = 64;
        if (!swung)
        {
            txt = Tr.T("¡Muy lento!", "Too slow!", "Muito lento!", "Trop lent !", "Zu langsam!", "遅すぎ！"); col = new Color(0.8f, 0.8f, 0.8f);
            perfectStreak = 0;
            MinigameFX.PlaySfx("machete_late");
        }
        else if (lastS > PerfectScore)
        {
            perfectStreak++;
            txt = perfectStreak > 1 ? Tr.T("¡PERFECTO! x", "PERFECT! x", "PERFEITO! x", "PARFAIT ! x", "PERFEKT! x", "パーフェクト！ x") + perfectStreak : Tr.T("¡PERFECTO!", "PERFECT!", "PERFEITO!", "PARFAIT !", "PERFEKT!", "パーフェクト！");
            col = Gold; size = 84;
            fx.Burst(cocoCenterWorld, Juice, 26, u, 5f, 0.12f, 0.8f, 8f, bias, 1.1f);
            fx.Confetti(cocoCenterWorld, u, 30);
            fx.Flash(root, new Color(1f, 0.95f, 0.6f, 0.3f), 0.25f);
            fx.HitStop(0.08f);
            Stage.Shake(0.9f, 0.3f);
            MinigameFX.PlaySfx("machete_perfect");
        }
        else if (lastS > 0.6f)
        {
            perfectStreak = 0;
            txt = Tr.T("¡Bien!", "Nice!", "Bom!", "Bien !", "Gut!", "いいね！"); col = new Color(0.5f, 1f, 0.5f);
            fx.Burst(cocoCenterWorld, Juice, 16, u, 4.5f, 0.12f, 0.7f, 8f, bias, 1.1f);
            fx.HitStop(0.05f);
            Stage.Shake(0.45f, 0.22f);
            MinigameFX.PlaySfx("machete_good");
        }
        else if (lastS > 0.25f)
        {
            perfectStreak = 0;
            txt = Tr.T("¡Casi!", "Almost!", "Quase!", "Presque !", "Fast!", "おしい！"); col = new Color(1f, 0.75f, 0.3f);
            fx.Burst(cocoCenterWorld, Juice, 8, u, 3.5f, 0.12f, 0.6f, 8f, bias, 1.1f);
            fx.HitStop(0.03f);
            Stage.Shake(0.25f, 0.18f);
            MinigameFX.PlaySfx("machete_ok");
        }
        else
        {
            perfectStreak = 0;
            txt = Tr.T("¡Fallaste!", "Missed!", "Errou!", "Raté !", "Verfehlt!", "ミス！"); col = new Color(1f, 0.35f, 0.35f);
            fx.Burst(cocoCenterWorld, new Color(0.8f, 0.8f, 0.8f), 5, u, 3f, 0.1f, 0.4f, 6f, bias, 1.2f);
            Stage.Shake(0.12f, 0.15f);
            MinigameFX.PlaySfx("machete_miss");
        }

        if (lastGood)
        {
            MinigameFX.Tint(coco.gameObject, new Color(2.5f, 2.5f, 2.5f, 1f));
            flashT = 0.08f;
        }

        fx.Pop(root, cocoUI + Vector2.up * 190f, txt, col, size, 80f, 1f);
        slash.rectTransform.anchoredPosition = cocoUI;
        slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        slash.gameObject.SetActive(swung);
        pulse.rectTransform.anchoredPosition = cocoUI;
        pulse.color = new Color(col.r, col.g, col.b, 0.9f);
        pulse.gameObject.SetActive(true);
        uiFxT = 0f;
    }

    void AnimateFeedback(float dt, Camera cam)
    {
        if (swung && !impactDone && feedbackElapsed >= MacheteSwingTime) Impact();

        if (flashT > 0f)
        {
            flashT -= dt;
            if (flashT <= 0f) MinigameFX.ClearTint(coco.gameObject);
        }

        if (uiFxT < 1f)
        {
            uiFxT = Mathf.Min(1f, uiFxT + dt / 0.3f);
            float e = MinigameFX.EaseOutCubic(uiFxT);
            Color pc = pulse.color; pc.a = 0.9f * (1f - uiFxT); pulse.color = pc;
            pulse.rectTransform.sizeDelta = Vector2.one * (TargetSize * (1f + 1.4f * e));
            slash.rectTransform.sizeDelta = new Vector2(420f * (0.4f + 0.6f * e), 12f * (1f - uiFxT));
            Color sc = slash.color; sc.a = 0.9f * (1f - uiFxT); slash.color = sc;
            if (uiFxT >= 1f) { pulse.gameObject.SetActive(false); slash.gameObject.SetActive(false); }
        }
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
        if (!impactDone)
        {
            coco.position = cocoBasePos;
        }
        else
        {
            float k = Mathf.Clamp01((feedbackElapsed - (swung ? MacheteSwingTime : 0f)) / ReactTime);
            if (lastGood)
            {
                float e = MinigameFX.EaseOutCubic(k);
                coco.position = cocoBasePos + Vector3.up * (cocoHeight * 2.2f * e) + right * (cocoHeight * 0.6f * k);
                coco.rotation = Quaternion.AngleAxis(540f * k, fwd) * cocoBaseRot;
                coco.localScale = cocoScale0 * (1f - k * k);
            }
            else
            {
                float hop = Mathf.Sin(Mathf.Min(1f, k * 1.6f) * Mathf.PI) * cocoHeight * 0.3f;
                coco.position = cocoBasePos + Vector3.up * hop + right * (Mathf.Sin(k * 40f) * cocoHeight * 0.08f * (1f - k));
            }
        }

        if (machete != null && machete.activeSelf && cam != null)
            PlaceMachete(cam, swung ? Mathf.Clamp01(feedbackElapsed / MacheteSwingTime) : 0f, 0f);
    }

    void PlaceMachete(Camera cam, float m, float bob)
    {
        Vector3 c = cocoCenterWorld;
        Vector3 r = cam.transform.right, u = cam.transform.up;
        float H = cocoHeight;
        Vector3 a = c + r * (H * 1.0f) + u * (H * 1.0f);
        Vector3 b = c - r * (H * 0.3f) - u * (H * 0.1f);
        float e = MinigameFX.EaseInCubic(m);
        machete.transform.position = Vector3.Lerp(a, b, e) + u * bob;
        float ang = Mathf.Lerp(MacheteStartAngle, MacheteEndAngle, e);
        machete.transform.rotation = Quaternion.AngleAxis(ang, cam.transform.forward) * macheteBaseRot;
    }

    public override void Cleanup()
    {
        fx.Clear();
        base.Cleanup();
    }
}