using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minigame_Bolos : MinigameBase
{
    public override string Name => "¡Bolos de Coco!";
    public override string Controls => "Clic para fijar el ángulo (centro es lo mejor) y otro clic para la fuerza (zona verde). 3 tiros.";

    public override string[] Intros => new[]
    {
        "La bolera del puerto quebró, así que ahora los bolos son botellas y la bola es un coco. Nadie dijo que fuera higiénico.",
        "La tía asegura que ella una vez hizo chuza con un coco. Hoy tienes la oportunidad de superarla.",
        "Los cocos se cansaron de ser golpeados y exigen revancha: hoy ELLOS son la bola. Qué ironía.",
    };

    public override bool Is3D => true;

    const int Throws = 3;
    const float BarWidth = 700f;
    const float RollTime = 0.8f;

    enum Step { Angle, Power, Rolling, Pause }

    Step step;
    int throwNumber;
    int totalPins;
    float t;
    float angleValue, powerValue;
    float speed;
    float stepTimer;
    float inputCooldown;
    bool pendingStrike;
    int pendingPins;

    MinigameStage.BolosSetup S;

    Transform[] pins;
    Vector3[] pinStartPos, pinBase, fallDir;
    Quaternion[] pinStartRot;
    float[] pinHeight, pinRadius;
    bool[] pinDown;
    float fallK;

    Transform ball;
    float ballRadius;
    Vector3 ballStart, ballTarget;
    float pitSpeed;
    Vector3 laneForward, laneRight;

    Image angleMarker, powerMarker;
    TMP_Text feedback;

    public override string Hud => "Tiro " + Mathf.Min(throwNumber + 1, Throws) + "/" + Throws + "     Pinos: " + totalPins;
    public override float Score01 => Mathf.Clamp01(totalPins / 24f);

    protected override void Setup()
    {
        throwNumber = 0;
        totalPins = 0;
        fallK = 0f;
        speed = 0.9f + 0.45f * (diff - 1f);

        S = Stage != null ? Stage.bolos : null;
        if (S == null || !S.IsReady)
        {
            Debug.LogError("Minigame_Bolos: falta configurar MinigameStage > Bolos (prefabs y Empties). Se omite el minijuego.");
            Finished = true;
            return;
        }

        Stage.UseCamera(S.cameraPoint);
        ballStart = S.ballStart.position;
        Vector3 toPins = S.pinsRoot.position - ballStart;
        toPins.y = 0f;
        laneForward = toPins.sqrMagnitude > 0.0001f ? toPins.normalized : Vector3.forward;
        laneRight = Vector3.Cross(Vector3.up, laneForward);
        int n = Mathf.Min(10, S.pinsRoot.childCount);
        pins = new Transform[n];
        pinStartPos = new Vector3[n];
        pinStartRot = new Quaternion[n];
        pinBase = new Vector3[n];
        pinHeight = new float[n];
        pinRadius = new float[n];
        pinDown = new bool[n];
        fallDir = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Transform spot = S.pinsRoot.GetChild(i);
            var go = Spawn(S.pinPrefab, spot.position, spot.rotation);
            pins[i] = go.transform;
            pinStartPos[i] = pins[i].position;
            pinStartRot[i] = pins[i].rotation;
            Bounds b = GetBounds(go);
            pinBase[i] = new Vector3(b.center.x, b.min.y, b.center.z);
            pinHeight[i] = b.size.y;
            pinRadius[i] = Mathf.Min(b.size.x, b.size.z) * 0.5f;
        }
        var ballGO = Spawn(S.ballPrefab, ballStart, S.ballStart.rotation);
        ball = ballGO.transform;
        ballRadius = Mathf.Max(0.01f, GetBounds(ballGO).extents.y);
        MiniUI.Img("PanelControles", root, new Color(0f, 0f, 0f, 0.55f), new Vector2(0, -225), new Vector2(BarWidth + 260f, 150f));
        MiniUI.Img("BarraAngulo", root, new Color(0.25f, 0.25f, 0.25f), new Vector2(0, -190), new Vector2(BarWidth, 26));
        MiniUI.Img("ZonaAngulo", root, new Color(0.3f, 0.7f, 0.3f), new Vector2(0, -190), new Vector2(BarWidth * 0.12f, 26));
        angleMarker = MiniUI.Img("MarcaAngulo", root, Color.white, new Vector2(0, -190), new Vector2(12, 46));
        MiniUI.Img("BarraFuerza", root, new Color(0.25f, 0.25f, 0.25f), new Vector2(0, -260), new Vector2(BarWidth, 26));
        MiniUI.Img("ZonaFuerza", root, new Color(0.3f, 0.7f, 0.3f), new Vector2((0.85f - 0.5f) * BarWidth, -260), new Vector2(BarWidth * 0.12f, 26));
        powerMarker = MiniUI.Img("MarcaFuerza", root, Color.white, new Vector2(0, -260), new Vector2(12, 46));
        MiniUI.Label(root, "ÁNGULO", 22, new Vector2(-BarWidth / 2f - 70f, -190), new Vector2(120, 30));
        MiniUI.Label(root, "FUERZA", 22, new Vector2(-BarWidth / 2f - 70f, -260), new Vector2(120, 30));
        feedback = MiniUI.Label(root, "", 64, new Vector2(0, 60), new Vector2(900, 100), new Color(1f, 0.9f, 0.3f));
        feedback.fontStyle = FontStyles.Bold;

        StartThrow();
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    void StartThrow()
    {
        for (int i = 0; i < pins.Length; i++)
        {
            pinDown[i] = false;
            pins[i].gameObject.SetActive(true);
            pins[i].SetPositionAndRotation(pinStartPos[i], pinStartRot[i]);
        }
        ball.gameObject.SetActive(true);
        ball.SetPositionAndRotation(ballStart, S.ballStart.rotation);

        feedback.text = "";
        step = Step.Angle;
        t = 0f;
        inputCooldown = 0.4f;
        angleMarker.color = Color.white;
        powerMarker.color = Color.white;
    }

    bool Clicked() => inputCooldown <= 0f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));

    protected override void Tick(float dt)
    {
        inputCooldown -= dt;

        switch (step)
        {
            case Step.Angle:
                t += dt;
                angleValue = Mathf.PingPong(t * speed, 1f);
                angleMarker.rectTransform.anchoredPosition = new Vector2((angleValue - 0.5f) * BarWidth, -190f);
                if (Clicked())
                {
                    angleMarker.color = new Color(1f, 0.85f, 0.2f);
                    step = Step.Power;
                    t = 0f;
                    inputCooldown = 0.25f;
                }
                break;

            case Step.Power:
                t += dt;
                powerValue = Mathf.PingPong(t * speed * 1.15f, 1f);
                powerMarker.rectTransform.anchoredPosition = new Vector2((powerValue - 0.5f) * BarWidth, -260f);
                if (Clicked())
                {
                    powerMarker.color = new Color(1f, 0.85f, 0.2f);
                    Launch();
                }
                break;

            case Step.Rolling:
                stepTimer += dt;
                float k = Mathf.Clamp01(stepTimer / RollTime);
                Vector3 prev = ball.position;
                ball.position = Vector3.Lerp(ballStart, ballTarget, k);
                Roll(ball.position - prev);
                if (k >= 1f) Knock();
                break;

            case Step.Pause:
                AnimatePins(dt);
                if (S.pit != null && ball.gameObject.activeSelf)
                {
                    Vector3 p0 = ball.position;
                    ball.position = Vector3.MoveTowards(p0, S.pit.position, pitSpeed * dt);
                    Roll(ball.position - p0);
                    if ((ball.position - S.pit.position).sqrMagnitude < 0.0001f) ball.gameObject.SetActive(false);
                }
                stepTimer -= dt;
                if (stepTimer <= 0f)
                {
                    throwNumber++;
                    if (throwNumber >= Throws) Finished = true;
                    else StartThrow();
                }
                break;
        }
    }

    void Roll(Vector3 delta)
    {
        float dist = delta.magnitude;
        if (dist < 1e-5f) return;
        Vector3 axis = Vector3.Cross(Vector3.up, delta / dist);
        ball.Rotate(axis, dist / ballRadius * Mathf.Rad2Deg, Space.World);
    }

    void Launch()
    {
        float aq = Mathf.Clamp01(1f - Mathf.Abs(angleValue - 0.5f) * 2.4f);
        float pq = Mathf.Clamp01(1f - Mathf.Abs(powerValue - 0.85f) * 3f);
        float q = aq * pq;

        bool perfect = aq > 0.9f && pq > 0.85f;
        pendingStrike = perfect;
        pendingPins = perfect ? pins.Length
            : Mathf.Clamp(Mathf.RoundToInt(10f * Mathf.Pow(q, 0.7f) + Random.Range(-1f, 1f)), 0, pins.Length);

        ballTarget = Vector3.Lerp(S.aimLeft.position, S.aimRight.position, angleValue);
        ballTarget.y = ballStart.y;
        stepTimer = 0f;
        step = Step.Rolling;
    }

    void Knock()
    {
        var order = new System.Collections.Generic.List<int>();
        for (int i = 0; i < pins.Length; i++) order.Add(i);
        for (int i = 0; i < pendingPins; i++)
        {
            int pick = Random.Range(0, order.Count);
            int pi = order[pick];
            pinDown[pi] = true;
            fallDir[pi] = (laneForward * Random.Range(0.3f, 1.5f) + laneRight * Random.Range(-1f, 1f)).normalized;
            order.RemoveAt(pick);
        }
        fallK = 0f;

        if (S.pit != null) pitSpeed = Mathf.Max(0.1f, Vector3.Distance(ball.position, S.pit.position) / 0.5f);
        else ball.gameObject.SetActive(false);

        totalPins += pendingPins;
        feedback.text = pendingStrike ? "¡CHUZA!" : pendingPins == 0 ? "¡Canaleta!" : pendingPins + " pinos";
        stepTimer = 1.3f;
        step = Step.Pause;
    }

    void AnimatePins(float dt)
    {
        fallK = Mathf.Min(1f, fallK + dt * 2.5f);
        float e = 1f - (1f - fallK) * (1f - fallK);
        for (int i = 0; i < pins.Length; i++)
        {
            if (!pinDown[i]) continue;
            Vector3 axis = Vector3.Cross(Vector3.up, fallDir[i]);
            Quaternion q = Quaternion.AngleAxis(90f * e, axis);
            pins[i].rotation = q * pinStartRot[i];
            pins[i].position = pinBase[i] + q * (pinStartPos[i] - pinBase[i])
                               + fallDir[i] * (pinHeight[i] * 0.5f * e)
                               + Vector3.up * (pinRadius[i] * e);
        }
    }
}