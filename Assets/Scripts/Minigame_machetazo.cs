using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame_Machetazo : MinigameBase
{
    public override string Name => "¡Machetazo Perfecto!";
    public override string Controls => "Clic justo cuando el anillo amarillo llegue al círculo gris. 8 cocos, ¡precisión!";

    public override string[] Intros => new[]
    {
        "El maestro machetero del puerto te reta: 'Un buen corte no se hace rápido, se hace en el momento exacto'. Luego se fue a comer tacos.",
        "Un chef gourmet necesita cocos cortados con precisión quirúrgica. Paga bien. Pregunta poco.",
        "Dicen que el machetazo perfecto hace que el coco suspire de alivio. Hoy lo comprobamos.",
    };

    public override bool Is3D => true;

    const int Rounds = 8;
    const float TargetSize = 170f; 
    const float StartSize = 520f;
    const float FeedbackTime = 0.55f;
    const float MacheteSwingTime = 0.2f;
    const float MacheteStartAngle = 60f;
    const float MacheteEndAngle = -40f;

    enum Step { Shrinking, Feedback }

    Step step;
    int round;
    float totalScore;
    float shrinkTime, t;
    float feedbackTimer, feedbackElapsed;
    float inputCooldown;

    MinigameStage.MachetazoSetup S;
    Transform[] spots;
    int allowed, lastSpot = -1;

    Transform coco;
    Vector3 cocoScale0, cocoLocalCenter, cocoBasePos, cocoCenterWorld;
    float cocoHeight;
    bool lastGood;

    GameObject machete;
    Quaternion macheteBaseRot;

    Image ring, target;
    TMP_Text feedback;

    static Sprite ringSprite;

    public override string Hud => "Coco " + Mathf.Min(round + 1, Rounds) + "/" + Rounds;
    public override float Score01 => Mathf.Clamp01(totalScore / Rounds);

    protected override void Setup()
    {
        round = 0;
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
        var cGO = Spawn(S.cocoPrefab, spots[0].position, spots[0].rotation);
        coco = cGO.transform;
        cocoScale0 = coco.localScale;
        Bounds b = GetBounds(cGO);
        cocoHeight = Mathf.Max(0.01f, b.size.y);
        cocoLocalCenter = coco.InverseTransformPoint(b.center);

        if (S.machetePrefab != null)
        {
            machete = Spawn(S.machetePrefab, spots[0].position, S.machetePrefab.transform.rotation);
            macheteBaseRot = machete.transform.rotation;
            machete.SetActive(false);
        }
        target = MiniUI.Img("Objetivo", root, new Color(0.85f, 0.9f, 0.85f, 0.95f), Vector2.zero, new Vector2(TargetSize, TargetSize));
        target.sprite = RingSprite();
        ring = MiniUI.Img("Anillo", root, new Color(1f, 0.85f, 0.2f, 0.95f), Vector2.zero, new Vector2(StartSize, StartSize));
        ring.sprite = RingSprite();

        feedback = MiniUI.Label(root, "", 64, new Vector2(0, 280), new Vector2(900, 100), new Color(1f, 0.9f, 0.3f));
        feedback.fontStyle = FontStyles.Bold;

        StartRound();
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
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
        cocoBasePos = coco.position;
        cocoCenterWorld = coco.TransformPoint(cocoLocalCenter);
        if (machete != null) machete.SetActive(false);
        Vector2 local = Vector2.zero;
        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        if (cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(cocoCenterWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, new Vector2(sp.x, sp.y), null, out local);
        }
        ring.rectTransform.anchoredPosition = local;
        target.rectTransform.anchoredPosition = local;
        ring.rectTransform.sizeDelta = new Vector2(StartSize, StartSize);
        ring.gameObject.SetActive(true);

        feedback.text = "";
        t = 0f;
        inputCooldown = 0.25f;
        step = Step.Shrinking;
    }

    protected override void Tick(float dt)
    {
        inputCooldown -= dt;

        if (step == Step.Shrinking)
        {
            t += dt;
            float k = Mathf.Clamp01(t / shrinkTime);
            float size = Mathf.Lerp(StartSize, 90f, k);
            ring.rectTransform.sizeDelta = new Vector2(size, size);

            if (inputCooldown <= 0f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                Judge(size);
            }
            else if (k >= 1f)
            {
                Judge(-1f);
            }
        }
        else
        {
            feedbackElapsed += dt;
            AnimateFeedback();

            feedbackTimer -= dt;
            if (feedbackTimer <= 0f)
            {
                round++;
                if (round >= Rounds) Finished = true;
                else StartRound();
            }
        }
    }

    void Judge(float size)
    {
        float s;
        if (size < 0f) s = 0f;
        else s = Mathf.Clamp01(1f - Mathf.Abs(size - TargetSize) / 130f);

        totalScore += s;
        ring.gameObject.SetActive(false);
        feedback.text = size < 0f ? "¡Muy lento!" : s > 0.9f ? "¡PERFECTO!" : s > 0.6f ? "¡Bien!" : s > 0.25f ? "¡Casi!" : "¡Fallaste!";

        lastGood = size >= 0f && s > 0.25f;
        feedbackElapsed = 0f;
        feedbackTimer = FeedbackTime;
        if (machete != null && size >= 0f) machete.SetActive(true);
        step = Step.Feedback;
    }
    void AnimateFeedback()
    {
        Camera cam = Stage != null ? Stage.ActiveCamera : null;
        float k = Mathf.Clamp01(feedbackElapsed / FeedbackTime);

        if (lastGood)
        {
            float e = 1f - (1f - k) * (1f - k);
            coco.position = cocoBasePos + Vector3.up * (cocoHeight * 2f * e);
            coco.localScale = cocoScale0 * (1f - k * k);
        }
        else
        {
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            coco.position = cocoBasePos + right * (Mathf.Sin(k * 40f) * cocoHeight * 0.08f * (1f - k));
        }

        if (machete != null && machete.activeSelf && cam != null)
        {
            float m = Mathf.Clamp01(feedbackElapsed / MacheteSwingTime);
            float angle = Mathf.Lerp(MacheteStartAngle, MacheteEndAngle, m);
            machete.transform.position = cocoCenterWorld
                                         + cam.transform.right * (cocoHeight * 0.8f)
                                         + cam.transform.up * (cocoHeight * 0.5f);
            machete.transform.rotation = Quaternion.AngleAxis(angle, cam.transform.forward) * macheteBaseRot;
        }
    }
}