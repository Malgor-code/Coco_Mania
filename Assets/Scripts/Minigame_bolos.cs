using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minigame_Bolos : MinigameBase
{
    public override string Name => Tr.T("¡Bolos de Coco!", "Coconut Bowling!", "Boliche de Coco!", "Bowling de noix de coco !", "Kokosnuss-Bowling!", "ココナッツボウリング！");
    public override string Controls => Tr.T("Clic para fijar el ángulo (centro es lo mejor) y otro clic para la fuerza (zona verde). 3 tiros.", "Click to lock the angle (center is best), then click again for power (green zone). 3 throws.", "Clique para fixar o ângulo (o centro é o melhor) e clique de novo para a força (zona verde). 3 lançamentos.", "Clique pour fixer l'angle (le centre est le meilleur), puis clique à nouveau pour la puissance (zone verte). 3 lancers.", "Klicke, um den Winkel festzulegen (die Mitte ist am besten), und noch einmal für die Kraft (grüne Zone). 3 Würfe.", "クリックで角度を決定（中央が最適）、もう一度クリックで力を決定（緑のゾーン）。3投。");

    public override string[] Intros => new[] { IntrosES, IntrosEN, IntrosPT, IntrosFR, IntrosDE, IntrosJA }[Tr.Index];

    static readonly string[] IntrosEN =
    {
        "The harbor bowling alley went bust, so now the pins are bottles and the ball is a coconut. Nobody said it was hygienic.",
        "Your aunt swears she once bowled a strike with a coconut. Today you get the chance to beat her.",
        "The coconuts got tired of being hit and demand a rematch: today THEY are the ball. How ironic.",
    };

    static readonly string[] IntrosPT =
    {
        "O boliche do porto faliu, então agora os pinos são garrafas e a bola é um coco. Ninguém disse que era higiênico.",
        "A tia jura que uma vez fez um strike com um coco. Hoje você tem a chance de superá-la.",
        "Os cocos cansaram de apanhar e exigem revanche: hoje ELES são a bola. Que ironia.",
    };

    static readonly string[] IntrosFR =
    {
        "Le bowling du port a fait faillite, alors maintenant les quilles sont des bouteilles et la boule est une noix de coco. Personne n'a dit que c'était hygiénique.",
        "Ma tante jure qu'elle a un jour fait un strike avec une noix de coco. Aujourd'hui, tu as l'occasion de faire mieux.",
        "Les noix de coco en ont assez de se faire frapper et exigent une revanche : aujourd'hui, ELLES sont la boule. Quelle ironie.",
    };

    static readonly string[] IntrosDE =
    {
        "Die Kegelbahn im Hafen ist pleitegegangen, also sind die Kegel jetzt Flaschen und die Kugel ist eine Kokosnuss. Hygienisch sollte es nie sein.",
        "Die Tante schwört, sie habe einmal mit einer Kokosnuss einen Strike geworfen. Heute kannst du sie übertreffen.",
        "Die Kokosnüsse haben es satt, geschlagen zu werden, und verlangen Revanche: Heute sind SIE die Kugel. Welche Ironie.",
    };

    static readonly string[] IntrosJA =
    {
        "港のボウリング場が潰れたので、ピンは瓶、ボールはココナッツになった。衛生的だなんて誰も言っていない。",
        "おばさんは、ココナッツでストライクを出したことがあると言い張っている。今日はそれを超えるチャンスだ。",
        "ココナッツたちは叩かれるのにうんざりして、リベンジを要求してきた。今日は彼らがボールだ。皮肉なものだ。",
    };

    static readonly string[] IntrosES =
    {
        "La bolera del puerto quebró, así que ahora los bolos son botellas y la bola es un coco. Nadie dijo que fuera higiénico.",
        "La tía asegura que ella una vez hizo chuza con un coco. Hoy tienes la oportunidad de superarla.",
        "Los cocos se cansaron de ser golpeados y exigen revancha: hoy ELLOS son la bola. Qué ironía.",
    };

    public override bool Is3D => true;

    const int Throws = 3;
    const float BarWidth = 700f;
    const float CenterTol = 0.06f;
    const float PowerTarget = 0.85f;
    const float PowerTol = 0.06f;

    static readonly Color BarDark = new Color(0.25f, 0.25f, 0.25f);
    static readonly Color BarLit = new Color(0.4f, 0.4f, 0.4f);
    static readonly Color Locked = new Color(1f, 0.85f, 0.2f);
    static readonly Color Gold = new Color(1f, 0.9f, 0.3f);
    static readonly Color Wood = new Color(0.85f, 0.7f, 0.45f);

    enum Step { Angle, Power, Rolling, Pause }

    readonly MinigameFX fx = new MinigameFX();

    Step step;
    int throwNumber, totalPins;
    float t, angleValue, powerValue, speed, stepTimer, inputCooldown, rollTime;
    bool pendingStrike;
    int pendingPins;

    MinigameStage.BolosSetup S;

    Transform[] pins;
    Vector3[] pinStartPos, pinBase, fallDir, pinScale0, wobbleAxis;
    Quaternion[] pinStartRot;
    float[] pinHeight, pinRadius, pinT, pinDelay, pinDur, pinSpin, pinSlide, pinPop;
    bool[] pinDown, pinStarted;
    float wobble, spawnT = 9f;

    Transform ball;
    Vector3 ballScale0;
    float ballRadius;
    Vector3 ballStart, ballTarget, exitPos;
    float pitSpeed, exitFade, laneSpan;
    Vector3 laneForward, laneRight;

    Image angleMarker, powerMarker, angleBar, powerBar;
    float anglePunch = 1f, powerPunch = 1f;

    public override string Hud => Tr.T("Tiro ", "Throw ", "Lançamento ", "Lancer ", "Wurf ", "投球 ") + Mathf.Min(throwNumber + 1, Throws) + "/" + Throws + Tr.T("     Pinos: ", "     Pins: ", "     Pinos: ", "     Quilles : ", "     Kegel: ", "     ピン：") + totalPins;
    public override float Score01 => Mathf.Clamp01(totalPins / 24f);

    protected override void Setup()
    {
        throwNumber = 0;
        totalPins = 0;
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
        pinStartPos = new Vector3[n]; pinBase = new Vector3[n]; fallDir = new Vector3[n];
        pinScale0 = new Vector3[n]; wobbleAxis = new Vector3[n];
        pinStartRot = new Quaternion[n];
        pinHeight = new float[n]; pinRadius = new float[n]; pinT = new float[n]; pinDelay = new float[n];
        pinDur = new float[n]; pinSpin = new float[n]; pinSlide = new float[n]; pinPop = new float[n];
        pinDown = new bool[n]; pinStarted = new bool[n];
        laneSpan = 0f;
        for (int i = 0; i < n; i++)
        {
            Transform spot = S.pinsRoot.GetChild(i);
            var go = MinigameFX.Prep(Spawn(S.pinPrefab, spot.position, spot.rotation), spot.position, spot.rotation, "Bolos: pino");
            pins[i] = go.transform;
            pinStartPos[i] = pins[i].position;
            pinStartRot[i] = pins[i].rotation;
            pinScale0[i] = pins[i].localScale;
            Bounds b = MinigameFX.GetBounds(go);
            pinBase[i] = new Vector3(b.center.x, b.min.y, b.center.z);
            pinHeight[i] = b.size.y;
            pinRadius[i] = Mathf.Min(b.size.x, b.size.z) * 0.5f;
            laneSpan = Mathf.Max(laneSpan, Vector3.Distance(S.pinsRoot.position, spot.position));
            Vector3 ax = Random.onUnitSphere; ax.y = 0f;
            wobbleAxis[i] = ax.sqrMagnitude > 0.001f ? ax.normalized : Vector3.right;
        }
        laneSpan = Mathf.Max(laneSpan, pinHeight[0] * 1.5f);

        var ballGO = MinigameFX.Prep(Spawn(S.ballPrefab, ballStart, S.ballStart.rotation), ballStart, S.ballStart.rotation, "Bolos: bola");
        ball = ballGO.transform;
        ballScale0 = ball.localScale;
        ballRadius = Mathf.Max(0.01f, MinigameFX.GetBounds(ballGO).extents.y);

        MiniUI.Img("PanelControles", root, new Color(0f, 0f, 0f, 0.55f), new Vector2(0, -225), new Vector2(BarWidth + 260f, 150f));
        angleBar = MiniUI.Img("BarraAngulo", root, BarDark, new Vector2(0, -190), new Vector2(BarWidth, 26));
        MiniUI.Img("ZonaAngulo", root, new Color(0.3f, 0.7f, 0.3f), new Vector2(0, -190), new Vector2(BarWidth * CenterTol * 2f, 26));
        angleMarker = MiniUI.Img("MarcaAngulo", root, Color.white, new Vector2(0, -190), new Vector2(12, 46));
        powerBar = MiniUI.Img("BarraFuerza", root, BarDark, new Vector2(0, -260), new Vector2(BarWidth, 26));
        MiniUI.Img("ZonaFuerza", root, new Color(0.3f, 0.7f, 0.3f), new Vector2((PowerTarget - 0.5f) * BarWidth, -260), new Vector2(BarWidth * PowerTol * 2f, 26));
        powerMarker = MiniUI.Img("MarcaFuerza", root, Color.white, new Vector2(0, -260), new Vector2(12, 46));
        MiniUI.Label(root, Tr.T("ÁNGULO", "ANGLE", "ÂNGULO", "ANGLE", "WINKEL", "角度"), 22, new Vector2(-BarWidth / 2f - 70f, -190), new Vector2(120, 30));
        MiniUI.Label(root, Tr.T("FUERZA", "POWER", "FORÇA", "PUISSANCE", "KRAFT", "パワー"), 22, new Vector2(-BarWidth / 2f - 70f, -260), new Vector2(120, 30));

        StartThrow();
    }

    void StartThrow()
    {
        for (int i = 0; i < pins.Length; i++)
        {
            pinDown[i] = false;
            pinStarted[i] = false;
            pins[i].gameObject.SetActive(true);
            pins[i].SetPositionAndRotation(pinStartPos[i], pinStartRot[i]);
            pins[i].localScale = Vector3.zero;
        }
        ball.gameObject.SetActive(true);
        ball.SetPositionAndRotation(ballStart, S.ballStart.rotation);
        ball.localScale = Vector3.zero;
        spawnT = 0f;
        wobble = 0f;

        step = Step.Angle;
        t = 0f;
        inputCooldown = 0.4f;
        angleMarker.color = Color.white;
        powerMarker.color = Color.white;
        anglePunch = powerPunch = 1f;
    }

    bool Clicked() => inputCooldown <= 0f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space));

    protected override void Tick(float dt)
    {
        if (pins == null) return;
        fx.Tick(dt);
        dt = fx.Gate(dt);
        inputCooldown -= dt;

        UpdateSpawnAnim(dt);
        UpdateMarkers(dt);

        switch (step)
        {
            case Step.Angle:
                t += dt;
                angleValue = Mathf.PingPong(t * speed, 1f);
                angleMarker.rectTransform.anchoredPosition = new Vector2((angleValue - 0.5f) * BarWidth, -190f);
                angleBar.color = Color.Lerp(BarDark, BarLit, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
                if (Clicked())
                {
                    angleMarker.color = Locked;
                    angleBar.color = BarDark;
                    anglePunch = 0f;
                    bool centered = Mathf.Abs(angleValue - 0.5f) <= CenterTol;
                    PopOnBar(-190f, centered ? Tr.T("¡Centro!", "Centered!", "Centro!", "Centré !", "Mittig!", "中央！") : "", centered);
                    MinigameFX.PlaySfx(centered ? "bolos_lock_good" : "bolos_lock");
                    step = Step.Power;
                    t = 0f;
                    inputCooldown = 0.25f;
                }
                break;

            case Step.Power:
                t += dt;
                powerValue = Mathf.PingPong(t * speed * 1.15f, 1f);
                powerMarker.rectTransform.anchoredPosition = new Vector2((powerValue - 0.5f) * BarWidth, -260f);
                powerBar.color = Color.Lerp(BarDark, BarLit, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
                if (Clicked())
                {
                    powerMarker.color = Locked;
                    powerBar.color = BarDark;
                    powerPunch = 0f;
                    bool good = Mathf.Abs(powerValue - PowerTarget) <= PowerTol;
                    PopOnBar(-260f, good ? Tr.T("¡Fuerza!", "Power!", "Força!", "Puissance !", "Kraft!", "パワー！") : "", good);
                    Launch();
                }
                break;

            case Step.Rolling:
                stepTimer += dt;
                float k = Mathf.Clamp01(stepTimer / rollTime);
                Vector3 prev = ball.position;
                ball.position = Vector3.Lerp(ballStart, ballTarget, k);
                Roll(ball.position - prev);
                if (k >= 1f) Knock();
                break;

            case Step.Pause:
                AnimatePins(dt);
                MoveBallOut(dt);
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

    void PopOnBar(float y, string text, bool good)
    {
        if (string.IsNullOrEmpty(text)) return;
        fx.Pop(root, new Vector2(BarWidth / 2f + 120f, y), text, good ? Gold : Color.white, 32, 30f, 0.7f);
    }

    void UpdateSpawnAnim(float dt)
    {
        if (spawnT >= 1.5f) return;
        spawnT += dt;
        for (int i = 0; i < pins.Length; i++)
        {
            if (pinDown[i]) continue;
            pins[i].localScale = pinScale0[i] * MinigameFX.EaseOutBack((spawnT - i * 0.035f) / 0.22f);
        }
        if (step == Step.Angle || step == Step.Power)
            ball.localScale = ballScale0 * MinigameFX.EaseOutBack(spawnT / 0.25f);
    }

    void UpdateMarkers(float dt)
    {
        if (anglePunch < 1f) anglePunch = Mathf.Min(1f, anglePunch + dt / 0.4f);
        if (powerPunch < 1f) powerPunch = Mathf.Min(1f, powerPunch + dt / 0.4f);
        angleMarker.rectTransform.localScale = Vector3.one * (1f + 0.7f * MinigameFX.Punch(anglePunch));
        powerMarker.rectTransform.localScale = Vector3.one * (1f + 0.7f * MinigameFX.Punch(powerPunch));
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
        float pq = Mathf.Clamp01(1f - Mathf.Abs(powerValue - PowerTarget) * 3f);
        float q = aq * pq;

        bool perfect = Mathf.Abs(angleValue - 0.5f) <= CenterTol && Mathf.Abs(powerValue - PowerTarget) <= PowerTol;
        pendingPins = perfect ? pins.Length
            : Mathf.Clamp(Mathf.RoundToInt(10f * Mathf.Pow(q, 0.7f) + Random.Range(-1f, 1f)), 0, pins.Length);
        pendingStrike = pendingPins >= pins.Length;

        ballTarget = Vector3.Lerp(S.aimLeft.position, S.aimRight.position, angleValue);
        ballTarget.y = ballStart.y;
        rollTime = Mathf.Lerp(1.15f, 0.6f, Mathf.Clamp01(powerValue));
        ball.localScale = ballScale0;
        stepTimer = 0f;
        step = Step.Rolling;
        MinigameFX.PlaySfx("bolos_roll");
    }

    void Knock()
    {
        int n = pins.Length;
        float[] score = new float[n];
        var order = new List<int>();
        for (int i = 0; i < n; i++)
        {
            Vector3 d = pinStartPos[i] - ballTarget;
            score[i] = Mathf.Abs(Vector3.Dot(d, laneRight)) + Mathf.Abs(Vector3.Dot(d, laneForward)) * 0.35f
                       + Random.Range(0f, pinHeight[i] * 0.6f);
            order.Add(i);
        }
        order.Sort((a, b) => score[a].CompareTo(score[b]));

        for (int j = 0; j < pendingPins; j++)
        {
            int pi = order[j];
            Vector3 flat = pinStartPos[pi] - ballTarget;
            flat.y = 0f;
            float dist = flat.magnitude;
            Vector3 away = dist > 0.001f ? flat / dist : laneForward;
            fallDir[pi] = (away * 0.6f + laneForward * 0.8f + laneRight * Random.Range(-0.25f, 0.25f)).normalized;

            pinDown[pi] = true;
            pinStarted[pi] = false;
            pinT[pi] = 0f;
            pinDelay[pi] = Mathf.Min(0.4f, dist / (pinHeight[pi] * 12f));
            pinDur[pi] = Random.Range(0.5f, 0.75f);
            pinSpin[pi] = Random.Range(-220f, 220f);
            pinSlide[pi] = Random.Range(0.6f, 1.5f);
            pinPop[pi] = Random.Range(0.6f, 1.2f);
        }

        if (S.pit != null)
        {
            exitPos = S.pit.position;
            exitFade = 0f;
        }
        else
        {
            exitPos = ball.position + laneForward * (laneSpan * 2.2f);
            exitFade = laneSpan * 0.5f;
        }
        pitSpeed = Mathf.Max(0.1f, Vector3.Distance(ball.position, exitPos) / 0.6f);

        float strength = pendingPins / (float)n;
        float u = pinHeight[0];
        Vector3 impact = ballTarget + Vector3.up * (u * 0.4f);
        if (pendingPins > 0)
        {
            fx.Burst(impact, Wood, 6 + pendingPins * 2, u, 4f, 0.1f, 0.6f, 8f, Vector3.up, 1f);
            Stage.Shake(0.2f + 0.8f * strength, 0.3f);
            fx.HitStop(pendingStrike ? 0.1f : 0.05f);
            if (pendingPins < n) wobble = 1f;
        }

        totalPins += pendingPins;
        string txt; Color col; int size;
        if (pendingStrike)
        {
            txt = Tr.T("¡CHUZA!", "STRIKE!", "STRIKE!", "STRIKE !", "STRIKE!", "ストライク！"); col = Gold; size = 96;
            fx.Confetti(S.pinsRoot.position + Vector3.up * (u * 1.5f), u, 50);
            fx.Flash(root, new Color(1f, 0.9f, 0.3f, 0.3f), 0.3f);
            MinigameFX.PlaySfx("bolos_strike");
        }
        else if (pendingPins == 0)
        {
            txt = Tr.T("¡Canaleta!", "Gutter ball!", "Canaleta!", "Gouttière !", "Rinne!", "ガター！"); col = new Color(1f, 0.4f, 0.4f); size = 72;
            MinigameFX.PlaySfx("bolos_gutter");
        }
        else
        {
            txt = pendingPins + Tr.T(" pinos", pendingPins == 1 ? " pin" : " pins", pendingPins == 1 ? " pino" : " pinos", pendingPins == 1 ? " quille" : " quilles", " Kegel", "本"); col = pendingPins >= 7 ? new Color(0.6f, 1f, 0.6f) : Color.white; size = 72;
            MinigameFX.PlaySfx("bolos_pins");
        }
        fx.Pop(root, new Vector2(0, 60), txt, col, size, 60f, 1.2f);

        stepTimer = 1.3f;
        step = Step.Pause;
    }

    void MoveBallOut(float dt)
    {
        if (!ball.gameObject.activeSelf) return;
        Vector3 p0 = ball.position;
        ball.position = Vector3.MoveTowards(p0, exitPos, pitSpeed * dt);
        Roll(ball.position - p0);
        float remaining = Vector3.Distance(ball.position, exitPos);
        if (exitFade > 0f) ball.localScale = ballScale0 * Mathf.Clamp01(remaining / exitFade);
        if (remaining < 0.01f) ball.gameObject.SetActive(false);
    }

    void AnimatePins(float dt)
    {
        wobble = Mathf.Max(0f, wobble - dt * 1.6f);
        for (int i = 0; i < pins.Length; i++)
        {
            if (!pinDown[i])
            {
                if (wobble > 0f)
                {
                    float ang = Mathf.Sin(Time.time * 38f + i) * 7f * wobble;
                    Quaternion w = Quaternion.AngleAxis(ang, wobbleAxis[i]);
                    pins[i].rotation = w * pinStartRot[i];
                    pins[i].position = pinBase[i] + w * (pinStartPos[i] - pinBase[i]);
                }
                continue;
            }

            pinT[i] += dt;
            float tt = pinT[i] - pinDelay[i];
            if (tt < 0f) continue;

            if (!pinStarted[i])
            {
                pinStarted[i] = true;
                fx.Burst(pinBase[i] + Vector3.up * (pinHeight[i] * 0.5f), Wood, 3, pinHeight[i], 3f, 0.1f, 0.5f, 8f, Vector3.up, 1f);
            }

            float k = Mathf.Clamp01(tt / pinDur[i]);
            float e = 1f - (1f - k) * (1f - k);
            Vector3 axis = Vector3.Cross(Vector3.up, fallDir[i]);
            Quaternion q = Quaternion.AngleAxis(pinSpin[i] * e, Vector3.up) * Quaternion.AngleAxis(90f * e, axis);
            float lift = Mathf.Sin(k * Mathf.PI) * pinHeight[i] * 0.45f * pinPop[i];
            pins[i].rotation = q * pinStartRot[i];
            pins[i].position = pinBase[i] + q * (pinStartPos[i] - pinBase[i])
                               + fallDir[i] * (pinHeight[i] * pinSlide[i] * e)
                               + Vector3.up * (pinRadius[i] * e + lift);
        }
    }

    public override void Cleanup()
    {
        fx.Clear();
        base.Cleanup();
    }
}