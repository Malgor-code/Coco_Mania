using UnityEngine;

[DisallowMultipleComponent]
public class CoconutWander : MonoBehaviour
{
    public enum MoveStyle
    {
        Despreocupado,  // Coco Verde
        Perezoso,       // Maduro
        Terco,          // Correoso
        Nervioso,       // Fibroso
        Rodante,        // Petreo
        Persistente,    // Curtido
        Vigilante,      // Blindado
        Saltarin,       // Hierro
        Robotico,       // Acero
        Implacable,     // Titanio
        Asustadizo,     // Diamante
        Huidizo,        // Legendario
        Sigiloso,       // Mitico
        Embrujado,      // Ancestral
        Jefe            // Supremo
    }

    [Header("Ajustes generales")]
    [Tooltip("Multiplica la velocidad de TODOS los cocos. Bajalo si se sienten muy rapidos.")]
    public float speedMultiplier = 1f;
    [Tooltip("Distancia minima al borde del mapa.")]
    public float edgeMargin = 0.6f;
    [Tooltip("Altura del salto del estilo Saltarin (Coco de Hierro).")]
    public float hopHeight = 0.45f;
    public float rotationSpeed = 8f;
    public bool debugLog = false;

    [Header("Personalidad (se sortea por coco al aparecer)")]
    [Range(0f, 0.6f)] public float speedVariation = 0.15f;
    [Range(0f, 1f)] public float personalityVariation = 0.5f;
    [Tooltip("Mientras esta quieto, gira la cabeza/cuerpo de vez en cuando.")]
    public bool lookAroundWhenIdle = true;

    [Header("Visual (opcional)")]
    [Tooltip("Hijo con el modelo. Si lo asignas, el salto se aplica a el en vez de al objeto raiz.")]
    public Transform visual;

    [Header("Reaccion al golpe")]
    [Tooltip("Un empujoncito suave lejos del machete al recibir un golpe. Los cocos pesados no se mueven.")]
    public bool useHitNudge = true;
    public float hitNudgeDistance = 0.25f;
    public float nudgeDuration = 0.15f;
    [Tooltip("Probabilidad de entrar en panico y huir tras un golpe (no aplica a los pesados).")]
    [Range(0f, 1f)] public float panicChanceAfterHit = 0.35f;
    public float panicDuration = 1.2f;

    [Header("Animator (opcional)")]
    [Tooltip("Parametro float: 0 = parado, 0.5 = caminar/brincar, 1 = correr. Vacio = no se usa.")]
    public string animSpeedParameter = "";

    [Header("Solo lectura (para depurar)")]
    public MoveStyle style = MoveStyle.Despreocupado;

    public bool IsStunned => Time.time < stunUntil;

    enum State { Idle, Moving }

    private CoconutTarget tgt;
    private Rigidbody rb;
    private Animator anim;
    private bool animHasParam;
    private float animValue;
    private Vector3 center;
    private Vector2 half = new Vector2(8f, 8f);
    private bool boundsSet;
    private bool initialized;
    private Vector3 home;
    private float baseY;
    private Vector3 velocity;
    private Vector3 nudgeVel;
    private float nudgeDecay;
    private float stunUntil = -1f;
    private float fleeUntil = -1f;
    private State state = State.Idle;
    private float timer;
    private Vector3 target;
    private float legLength;
    private float curSpeed;
    private bool hopping;
    private float hopOffset;
    private Vector3 visualBaseLocal;
    private float speedMult = 1f;
    private float lazy = 1f;
    private float targetYaw;
    private float lookTimer;
    private float baseSpeed = 1.6f, accel = 14f, pauseMin = 0.4f, pauseMax = 1.4f;
    private float fleeSpeed = 4f, fleeRadius = 3f;
    private float heading;
    private int zigSign = 1, zigLegs, bossLegs, patrolIndex;
    private bool axisX;
    private Vector3[] patrol;

    void Awake()
    {
        tgt = GetComponent<CoconutTarget>();
        baseY = transform.position.y;
        targetYaw = transform.eulerAngles.y;
        rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;
        }

        // personalidad por coco
        speedMult = 1f + Random.Range(-speedVariation, speedVariation);
        lazy = Mathf.Lerp(1f, Random.Range(0.4f, 1.9f), personalityVariation);

        anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(animSpeedParameter))
        {
            foreach (var p in anim.parameters)
                if (p.name == animSpeedParameter) { animHasParam = true; break; }
        }

        if (visual != null) visualBaseLocal = visual.localPosition;
    }

    void OnDisable()
    {
        velocity = Vector3.zero;
        nudgeVel = Vector3.zero;
        hopOffset = 0f;
        ResetVisual();
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }

    public void SetBounds(Vector3 c, Vector2 h)
    {
        center = c;
        half = h;
        boundsSet = true;
        Init();
    }

    public void Stun(float seconds)
    {
        if (seconds > 0.01f) stunUntil = Mathf.Max(stunUntil, Time.time + seconds);
    }
    public void OnHit()
    {
        if (!initialized) return;

        bool heavy = IsHeavy();

        if (style == MoveStyle.Huidizo) fleeUntil = Time.time + 2f;
        else if (!heavy && Random.value < panicChanceAfterHit)  
            fleeUntil = Mathf.Max(fleeUntil, Time.time + panicDuration);

        if (useHitNudge && !heavy && MacheteDir(CurrentPos(), out Vector3 toM, out float d))
        {
            Vector3 away = -toM;
            nudgeVel = away * (2f * hitNudgeDistance / Mathf.Max(0.05f, nudgeDuration));
            nudgeDecay = nudgeVel.magnitude / Mathf.Max(0.05f, nudgeDuration);
        }
    }
    void Init()
    {
        initialized = true;
        Vector3 p = CurrentPos();
        if (!boundsSet) center = p;
        home = p;
        baseY = p.y;
        velocity = Vector3.zero;
        nudgeVel = Vector3.zero;

        heading = Random.value * 360f;
        zigSign = Random.value < 0.5f ? 1 : -1;
        axisX = Random.value < 0.5f;
        zigLegs = 0; bossLegs = 0; patrolIndex = 0;
        if (tgt == null) tgt = GetComponent<CoconutTarget>();
        GameManager.CoconutAbility ability = tgt != null ? tgt.ability : GameManager.CoconutAbility.Ninguna;
        style = StyleFromAbility(ability);
        ConfigureStyle();

        if (style == MoveStyle.Vigilante)
        {
            patrol = new Vector3[3];
            for (int i = 0; i < patrol.Length; i++) patrol[i] = RandomNear(home, 3.5f);
        }

        state = State.Idle;
        timer = Random.Range(0f, 0.6f);
    }

    static MoveStyle StyleFromAbility(GameManager.CoconutAbility a)
    {
        switch (a)
        {
            case GameManager.CoconutAbility.Generoso: return MoveStyle.Perezoso;
            case GameManager.CoconutAbility.Resistente: return MoveStyle.Terco;
            case GameManager.CoconutAbility.FibraDura: return MoveStyle.Nervioso;
            case GameManager.CoconutAbility.Pesado: return MoveStyle.Rodante;
            case GameManager.CoconutAbility.Tenaz: return MoveStyle.Persistente;
            case GameManager.CoconutAbility.Armadura: return MoveStyle.Vigilante;
            case GameManager.CoconutAbility.Rebote: return MoveStyle.Saltarin;
            case GameManager.CoconutAbility.Fortificado: return MoveStyle.Robotico;
            case GameManager.CoconutAbility.Implacable: return MoveStyle.Implacable;
            case GameManager.CoconutAbility.FragilValioso: return MoveStyle.Asustadizo;
            case GameManager.CoconutAbility.Regeneracion: return MoveStyle.Huidizo;
            case GameManager.CoconutAbility.Camuflaje: return MoveStyle.Sigiloso;
            case GameManager.CoconutAbility.Maldicion: return MoveStyle.Embrujado;
            case GameManager.CoconutAbility.Supremo: return MoveStyle.Jefe;
            default: return MoveStyle.Despreocupado;
        }
    }
    void ConfigureStyle()
    {
        fleeSpeed = 4f;
        fleeRadius = 3f;

        switch (style)
        {
            case MoveStyle.Despreocupado: baseSpeed = 1.6f; accel = 14f; pauseMin = 0.4f; pauseMax = 1.4f; break;
            case MoveStyle.Perezoso: baseSpeed = 0.9f; accel = 8f; pauseMin = 1.5f; pauseMax = 3.0f; break;
            case MoveStyle.Terco: baseSpeed = 2.0f; accel = 12f; pauseMin = 0.2f; pauseMax = 0.6f; break;
            case MoveStyle.Nervioso: baseSpeed = 3.0f; accel = 25f; pauseMin = 0f; pauseMax = 0.1f; break;
            case MoveStyle.Rodante: baseSpeed = 1.1f; accel = 2f; pauseMin = 0f; pauseMax = 0f; break;
            case MoveStyle.Persistente: baseSpeed = 1.3f; accel = 14f; pauseMin = 0.5f; pauseMax = 1.2f; break;
            case MoveStyle.Vigilante: baseSpeed = 1.8f; accel = 14f; pauseMin = 0.8f; pauseMax = 1.4f; break;
            case MoveStyle.Saltarin: baseSpeed = 4.4f; accel = 40f; pauseMin = 0.25f; pauseMax = 0.5f; break;
            case MoveStyle.Robotico: baseSpeed = 2.4f; accel = 40f; pauseMin = 0.3f; pauseMax = 0.6f; break;
            case MoveStyle.Implacable: baseSpeed = 3.0f; accel = 10f; pauseMin = 0f; pauseMax = 0f; break;
            case MoveStyle.Asustadizo: baseSpeed = 1.2f; accel = 18f; pauseMin = 0.6f; pauseMax = 1.6f; fleeSpeed = 4.5f; fleeRadius = 3f; break;
            case MoveStyle.Huidizo: baseSpeed = 1.6f; accel = 14f; pauseMin = 0.4f; pauseMax = 1.2f; fleeSpeed = 3.6f; break;
            case MoveStyle.Sigiloso: baseSpeed = 6.0f; accel = 40f; pauseMin = 1.4f; pauseMax = 2.6f; break;
            case MoveStyle.Embrujado: baseSpeed = 1.6f; accel = 6f; pauseMin = 0f; pauseMax = 0f; break;
            case MoveStyle.Jefe: baseSpeed = 1.4f; accel = 10f; pauseMin = 0.6f; pauseMax = 1.2f; break;
        }
    }

    bool IsHeavy()
    {
        return style == MoveStyle.Rodante || style == MoveStyle.Vigilante || style == MoveStyle.Robotico
            || style == MoveStyle.Implacable || style == MoveStyle.Jefe;
    }

    Vector3 CurrentPos()
    {
        return (rb != null) ? rb.position : transform.position;
    }
    void FixedUpdate()
    {
        if (!initialized) Init();

        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;
        bool hitting = GameManager.Instance == null || GameManager.Instance.IsHittingPhase();
        bool dayActive = CoconutSpawner.Instance != null && CoconutSpawner.Instance.DayActive;
        bool active = hitting || dayActive;

        if (debugLog)
        {
            Debug.Log($"{name} style={style} state={state} hitting={hitting} dayActive={dayActive} " +
                      $"stunned={IsStunned} rb={(rb != null)} timeScale={Time.timeScale}");
        }

        Vector3 pos = CurrentPos();
        Vector3 desired = Vector3.zero;

        if (active && !IsStunned)
        {
            desired = Think(pos, dt);
        }
        else
        {
            hopping = false;
            hopOffset = 0f;
        }

        velocity = Vector3.MoveTowards(velocity, desired, accel * dt);
        if (nudgeVel.sqrMagnitude > 0.0001f)
            nudgeVel = Vector3.MoveTowards(nudgeVel, Vector3.zero, nudgeDecay * dt);

        Vector3 move = velocity + nudgeVel;
        move.y = 0f;
        Vector3 next = ClampToBounds(pos + move * dt);
        if (rb != null)
        {
            if (rb.isKinematic)
                rb.MovePosition(new Vector3(next.x, pos.y, next.z));
            else
                rb.linearVelocity = new Vector3((next.x - pos.x) / dt, 0f, (next.z - pos.z) / dt);
        }
        else
        {
            next.y = baseY + (visual == null ? hopOffset : 0f);
            transform.position = next;
        }
        if (visual != null) visual.localPosition = visualBaseLocal + Vector3.up * hopOffset;

        UpdateRotation(dt);
        UpdateAnimator(dt);
    }

    void UpdateRotation(float dt)
    {
        Vector3 hv = velocity;
        hv.y = 0f;
        if (hv.sqrMagnitude > 0.04f)
            targetYaw = Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg;

        Quaternion want = Quaternion.Euler(0f, targetYaw, 0f);
        Quaternion cur = (rb != null) ? rb.rotation : transform.rotation;
        Quaternion nxt = Quaternion.Slerp(cur, want, rotationSpeed * dt);

        if (rb != null) rb.MoveRotation(nxt);
        else transform.rotation = nxt;
    }

    void UpdateAnimator(float dt)
    {
        if (!animHasParam) return;
        float speed = velocity.magnitude;
        float target = speed < 0.1f ? 0f : (speed < 2.5f ? 0.5f : 1f);
        animValue = Mathf.MoveTowards(animValue, target, 4f * dt);
        anim.SetFloat(animSpeedParameter, animValue);
    }

    void ResetVisual()
    {
        if (visual != null) visual.localPosition = visualBaseLocal;
    }

    Vector3 Think(Vector3 pos, float dt)
    {
        float fr = 0f;
        if (style == MoveStyle.Asustadizo) fr = fleeRadius;
        if (Time.time < fleeUntil) fr = 999f;

        if (fr > 0f)
        {
            Vector3 away = FleeDirection(pos, fr);
            if (away != Vector3.zero)
            {
                state = State.Idle;
                timer = 0.2f;
                hopping = false;
                hopOffset = 0f;
                return away * (fleeSpeed * speedMult * speedMultiplier);
            }
        }
        if (state == State.Idle)
        {
            timer -= dt;
            if (lookAroundWhenIdle) LookAroundStep(dt);
            if (timer > 0f) return Vector3.zero;
            BeginLeg(pos);
        }

        float spd = curSpeed * speedMult * speedMultiplier;
        Vector3 to = target - pos;
        to.y = 0f;
        float dist = to.magnitude;
        float arrive = Mathf.Max(0.15f, spd * dt * 1.5f);
        if (dist <= arrive)
        {
            EndLeg(pos);
            return Vector3.zero;
        }

        if (hopping && legLength > 0.01f)
        {
            float p = 1f - Mathf.Clamp01(dist / legLength);
            hopOffset = Mathf.Sin(p * Mathf.PI) * hopHeight;
        }

        return to / dist * spd;
    }

    void LookAroundStep(float dt)
    {
        if (velocity.sqrMagnitude > 0.04f) return;
        lookTimer -= dt;
        if (lookTimer <= 0f)
        {
            lookTimer = Random.Range(0.6f, 1.4f);
            targetYaw += Random.Range(50f, 140f) * (Random.value < 0.5f ? -1f : 1f);
        }
    }

    void EndLeg(Vector3 pos)
    {
        hopping = false;
        hopOffset = 0f;
        state = State.Idle;
        timer = Random.Range(pauseMin, pauseMax) * lazy;
        if (timer <= 0f) BeginLeg(pos);
    }

    void BeginLeg(Vector3 pos)
    {
        curSpeed = baseSpeed;
        hopping = false;
        Vector3 t;

        switch (style)
        {
            case MoveStyle.Perezoso:
                t = RandomNear(pos, 2.5f);
                break;

            case MoveStyle.Terco:
                t = BounceStep(pos, 6f);
                break;

            case MoveStyle.Rodante:
                t = BounceStep(pos, 7f);
                break;

            case MoveStyle.Nervioso:
                zigSign = -zigSign;
                zigLegs++;
                if (zigLegs % 4 == 0 || OutOfBounds(pos + HeadingDir(heading) * 3f))
                    heading = TowardCenterHeading(pos) + Random.Range(-60f, 60f);
                t = ClampToBounds(pos + HeadingDir(heading + zigSign * 55f) * 1.7f);
                break;

            case MoveStyle.Persistente:
                t = RandomNear(home, 1.2f);
                break;

            case MoveStyle.Vigilante:
                patrolIndex = (patrolIndex + 1) % patrol.Length;
                t = patrol[patrolIndex];
                break;

            case MoveStyle.Saltarin:
                {
                    Vector3 dir = HeadingDir(Random.value * 360f);
                    t = pos + dir * 2f;
                    if (OutOfBounds(t))
                        t = pos + HeadingDir(TowardCenterHeading(pos) + Random.Range(-40f, 40f)) * 2f;
                    t = ClampToBounds(t);
                    hopping = true;
                    break;
                }

            case MoveStyle.Robotico:
                {
                    axisX = !axisX;
                    float d = Random.Range(2f, 4f) * (Random.value < 0.5f ? -1f : 1f);
                    t = pos + (axisX ? new Vector3(d, 0f, 0f) : new Vector3(0f, 0f, d));
                    if (OutOfBounds(t)) t = pos - (t - pos);
                    t = ClampToBounds(t);
                    break;
                }

            case MoveStyle.Implacable:
                t = RandomInBounds();
                break;

            case MoveStyle.Sigiloso:
                {
                    Vector3 dir = HeadingDir(Random.value * 360f);
                    t = pos + dir * Random.Range(3f, 4.5f);
                    if (OutOfBounds(t))
                        t = pos + HeadingDir(TowardCenterHeading(pos) + Random.Range(-40f, 40f)) * Random.Range(3f, 4.5f);
                    t = ClampToBounds(t);
                    break;
                }

            case MoveStyle.Embrujado:
                {
                    Vector3 fromC = pos - center;
                    fromC.y = 0f;
                    float rMax = Mathf.Max(2.6f, Mathf.Min(half.x, half.y) - 1.5f);
                    float r = Mathf.Clamp(fromC.magnitude, 2.5f, rMax);
                    float ang = Mathf.Atan2(fromC.z, fromC.x) * Mathf.Rad2Deg + 35f;
                    t = new Vector3(center.x + Mathf.Cos(ang * Mathf.Deg2Rad) * r, pos.y,
                                    center.z + Mathf.Sin(ang * Mathf.Deg2Rad) * r);
                    t = ClampToBounds(t);
                    break;
                }

            case MoveStyle.Jefe:
                {
                    bossLegs++;
                    float phase = PhaseFactor();
                    if (bossLegs % 3 == 0 && MacheteDir(pos, out Vector3 toM, out float dM))
                    {
                        curSpeed = 6f * phase; 
                        t = ClampToBounds(pos + toM * Mathf.Min(dM, 4f));
                    }
                    else
                    {
                        curSpeed = baseSpeed * phase;
                        t = ClampToBounds(center + new Vector3(Random.Range(-half.x, half.x) * 0.4f, 0f,
                                                               Random.Range(-half.y, half.y) * 0.4f));
                    }
                    break;
                }

            default:
                t = RandomNear(pos, 4f);
                break;
        }

        target = t;
        Vector3 flat = target - pos;
        flat.y = 0f;
        legLength = flat.magnitude;
        state = State.Moving;
    }

    float PhaseFactor()
    {
        if (tgt == null || tgt.hpMax <= 0) return 1f;
        return 1f + (1f - (float)tgt.hp / tgt.hpMax) * 0.8f;
    }

    bool MacheteDir(Vector3 pos, out Vector3 dir, out float dist)
    {
        dir = Vector3.zero;
        dist = 0f;
        MacheteController m = MacheteController.Instance;
        if (m == null || !m.gameObject.activeInHierarchy) return false;
        Vector3 d = m.transform.position - pos;
        d.y = 0f;
        dist = d.magnitude;
        if (dist < 0.01f) return false;
        dir = d / dist;
        return true;
    }

    Vector3 FleeDirection(Vector3 pos, float radius)
    {
        if (!MacheteDir(pos, out Vector3 toM, out float d) || d > radius) return Vector3.zero;

        Vector3 dir = -toM;
        if (OutOfBounds(pos + dir * 0.6f))
        {
            Vector3 toC = center - pos;
            toC.y = 0f;
            Vector3 perp = Vector3.Cross(Vector3.up, dir);
            if (Vector3.Dot(perp, toC) < 0f) perp = -perp;
            dir = (perp + toC.normalized * 0.3f).normalized;
        }
        return dir;
    }

    Vector3 BounceStep(Vector3 pos, float dist)
    {
        Vector3 t = pos + HeadingDir(heading) * dist;
        if (OutOfBounds(t))
        {
            heading = TowardCenterHeading(pos) + Random.Range(-45f, 45f);
            t = pos + HeadingDir(heading) * dist;
        }
        return ClampToBounds(t);
    }

    static Vector3 HeadingDir(float degrees)
    {
        return Quaternion.Euler(0f, degrees, 0f) * Vector3.forward;
    }

    float TowardCenterHeading(Vector3 pos)
    {
        Vector3 toC = center - pos;
        return Mathf.Atan2(toC.x, toC.z) * Mathf.Rad2Deg;
    }

    Vector3 RandomNear(Vector3 pos, float radius)
    {
        Vector2 c = Random.insideUnitCircle * radius;
        return ClampToBounds(pos + new Vector3(c.x, 0f, c.y));
    }

    Vector3 RandomInBounds()
    {
        return new Vector3(
            center.x + Random.Range(-half.x + edgeMargin, half.x - edgeMargin),
            baseY,
            center.z + Random.Range(-half.y + edgeMargin, half.y - edgeMargin));
    }

    bool OutOfBounds(Vector3 p)
    {
        return Mathf.Abs(p.x - center.x) > half.x - edgeMargin || Mathf.Abs(p.z - center.z) > half.y - edgeMargin;
    }

    Vector3 ClampToBounds(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, center.x - half.x + edgeMargin, center.x + half.x - edgeMargin);
        p.z = Mathf.Clamp(p.z, center.z - half.y + edgeMargin, center.z + half.y - edgeMargin);
        return p;
    }
}