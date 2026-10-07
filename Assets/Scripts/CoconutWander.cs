using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CoconutWander : MonoBehaviour
{
    public enum State { Idle, Walk, Run, LookAround, Hop }

    [Header("Movimiento base")]
    public float speed = 1.5f;
    [Tooltip("Correr = speed x esto.")]
    public float runSpeedMultiplier = 2.4f;
    [Tooltip("Brincar = speed x esto.")]
    public float hopSpeedMultiplier = 0.7f;
    [Tooltip("Tiempo maximo para llegar a un destino (si no llega, cambia de comportamiento).")]
    public float retargetInterval = 2.5f;
    public float rotationSpeed = 8f;
    public bool debugLog = false;

    [Header("Personalidad (se sortea por coco al aparecer)")]
    [Range(0f, 0.6f)] public float speedVariation = 0.3f;
    [Range(0f, 1f)] public float personalityVariation = 0.7f;

    [Header("Pesos de cada comportamiento (mayor = mas probable)")]
    public float idleWeight = 25f;
    public float walkWeight = 40f;
    public float runWeight = 15f;
    public float lookAroundWeight = 12f;
    public float hopWeight = 8f;

    [Header("Duraciones de los estados quietos (min, max)")]
    public Vector2 idleTime = new Vector2(0.8f, 3f);
    public Vector2 lookAroundTime = new Vector2(1.2f, 2.5f);

    [Header("Distancia de cada trayecto (min, max)")]
    public Vector2 walkDistance = new Vector2(1.5f, 4.5f);
    public Vector2 runDistance = new Vector2(3f, 7f);
    public Vector2 hopDistance = new Vector2(1f, 3f);

    [Header("Correr en zigzag")]
    [Range(0f, 1f)] public float runZigzagChance = 0.35f;
    public float zigzagAngle = 35f;
    public float zigzagFrequency = 3f;
    public Transform visual;
    public float hopHeight = 0.25f;
    public float hopsPerSecond = 2.2f;

    [Header("Reaccion al golpe")]
    [Range(0f, 1f)] public float panicChanceAfterHit = 0.35f;

    [Header("Animator (opcional)")]
    [Tooltip("Parametro float: 0 = parado, 0.5 = caminar/brincar, 1 = correr. Vacio = no se usa.")]
    public string animSpeedParameter = "";

    private Vector3 targetPos;
    private Vector3 boundsCenter;
    private Vector2 boundsHalfExtents = new Vector2(8f, 8f);
    private bool boundsSet = false;
    private float stunTimer = 0f;
    private Rigidbody rb;

    private State state = State.Idle;
    private State lastState = State.Idle;
    private float stateTimer;
    private float lookTimer;
    private float lookYaw;
    private bool zigzagging;

    private float speedMult = 1f;
    private float lazy = 1f;
    private float restless = 1f;
    private float seed;

    private float hopPhase;
    private float hopAir = 1f;
    private Vector3 visualBaseLocal;

    private Animator anim;
    private bool animHasParam;
    private float animValue, animTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;

        seed = Random.value * 100f;
        speedMult = 1f + Random.Range(-speedVariation, speedVariation);
        lazy = Mathf.Lerp(1f, Random.Range(0.4f, 1.9f), personalityVariation);
        restless = Mathf.Lerp(1f, Random.Range(0.4f, 1.9f), personalityVariation);

        anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(animSpeedParameter))
        {
            foreach (var p in anim.parameters)
                if (p.name == animSpeedParameter) { animHasParam = true; break; }
        }

        if (visual != null) visualBaseLocal = visual.localPosition;
    }

    void Start()
    {
        if (!boundsSet) boundsCenter = transform.position;
        PickNextState();
        stateTimer *= Random.Range(0.2f, 1f);
        rb.WakeUp();
    }

    void OnDisable()
    {
        ResetVisual();
    }

    public void SetBounds(Vector3 center, Vector2 halfExtents)
    {
        boundsCenter = center;
        boundsHalfExtents = halfExtents;
        boundsSet = true;
        if (state == State.Walk || state == State.Run || state == State.Hop) PickTarget(walkDistance);
    }

    public void Stun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
        ResetVisual();
    }

    void StopMoving()
    {
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }
    void Update()
    {
        if (state == State.Hop && stunTimer <= 0f)
        {
            hopPhase += Time.deltaTime * hopsPerSecond;
            float h = Mathf.Abs(Mathf.Sin(hopPhase * Mathf.PI)) * hopHeight;
            if (visual != null) visual.localPosition = visualBaseLocal + Vector3.up * h;
            hopAir = h > hopHeight * 0.15f ? 1f : 0.2f;
        }

        if (animHasParam)
        {
            animValue = Mathf.MoveTowards(animValue, animTarget, 4f * Time.deltaTime);
            anim.SetFloat(animSpeedParameter, animValue);
        }
    }

    void ResetVisual()
    {
        if (visual != null) visual.localPosition = visualBaseLocal;
    }
    void FixedUpdate()
    {
        bool hitting = GameManager.Instance == null || GameManager.Instance.IsHittingPhase();
        bool dayActive = CoconutSpawner.Instance != null && CoconutSpawner.Instance.DayActive;

        if (debugLog)
        {
            Debug.Log($"{name} state={state} hitting={hitting} dayActive={dayActive} stun={stunTimer} " +
                      $"kinematic={rb.isKinematic} timeScale={Time.timeScale}");
        }

        if (!hitting && !dayActive)
        {
            StopMoving();
            animTarget = 0f;
            return;
        }

        float dt = Time.fixedDeltaTime;

        if (stunTimer > 0f)
        {
            stunTimer -= dt;
            StopMoving();
            animTarget = 0f;
            if (stunTimer <= 0f) AfterStun();
            return;
        }

        stateTimer -= dt;
        bool arrived = false;

        switch (state)
        {
            case State.Idle:
                StopMoving();
                animTarget = 0f;
                break;

            case State.LookAround:
                StopMoving();
                animTarget = 0f;
                lookTimer -= dt;
                if (lookTimer <= 0f)
                {
                    lookTimer = Random.Range(0.4f, 0.9f);
                    lookYaw = rb.rotation.eulerAngles.y + Random.Range(50f, 140f) * (Random.value < 0.5f ? -1f : 1f);
                }
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.Euler(0f, lookYaw, 0f), rotationSpeed * 0.15f * dt * 10f * 0.1f));
                break;

            case State.Walk:
                arrived = MoveToTarget(speed * speedMult, dt, 0f);
                animTarget = 0.5f;
                break;

            case State.Run:
                {
                    float zig = zigzagging
                        ? Mathf.Sign(Mathf.Sin(Time.time * zigzagFrequency + seed)) * zigzagAngle
                        : 0f;
                    arrived = MoveToTarget(speed * runSpeedMultiplier * speedMult, dt, zig);
                    animTarget = 1f;
                }
                break;

            case State.Hop:
                arrived = MoveToTarget(speed * hopSpeedMultiplier * speedMult * hopAir, dt, 0f);
                animTarget = 0.5f;
                break;
        }

        if (stateTimer <= 0f || arrived) PickNextState();
    }
    bool MoveToTarget(float moveSpeed, float dt, float angleOffset)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.magnitude < 0.3f)
        {
            StopMoving();
            return true;
        }

        dir.Normalize();
        if (Mathf.Abs(angleOffset) > 0.01f) dir = Quaternion.Euler(0f, angleOffset, 0f) * dir;

        Vector3 velocity = dir * moveSpeed;

        if (rb.isKinematic) rb.MovePosition(rb.position + velocity * dt);
        else rb.linearVelocity = velocity;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * dt));
        return false;
    }
    void PickNextState()
    {
        ResetVisual();

        State next = RollState();
        if (next == lastState && (next == State.Idle || next == State.LookAround)) next = RollState();
        EnterState(next);
    }

    State RollState()
    {
        float wIdle = idleWeight * lazy;
        float wWalk = walkWeight;
        float wRun = runWeight * restless;
        float wLook = lookAroundWeight * lazy;
        float wHop = (visual != null ? hopWeight : 0f) * restless;

        float r = Random.value * (wIdle + wWalk + wRun + wLook + wHop);

        if ((r -= wIdle) < 0f) return State.Idle;
        if ((r -= wWalk) < 0f) return State.Walk;
        if ((r -= wRun) < 0f) return State.Run;
        if ((r -= wLook) < 0f) return State.LookAround;
        return State.Hop;
    }

    void EnterState(State s)
    {
        lastState = state;
        state = s;
        hopAir = 1f;

        switch (s)
        {
            case State.Idle:
                stateTimer = Random.Range(idleTime.x, idleTime.y);
                break;

            case State.LookAround:
                stateTimer = Random.Range(lookAroundTime.x, lookAroundTime.y);
                lookTimer = 0f;
                break;

            case State.Walk:
                PickTarget(walkDistance);
                stateTimer = retargetInterval * Random.Range(0.8f, 1.4f);
                break;

            case State.Run:
                PickTarget(runDistance);
                zigzagging = Random.value < runZigzagChance;
                stateTimer = retargetInterval * Random.Range(0.5f, 0.9f);
                break;

            case State.Hop:
                PickTarget(hopDistance);
                hopPhase = 0f;
                stateTimer = retargetInterval * Random.Range(0.6f, 1.1f);
                break;
        }
    }

    void AfterStun()
    {
        if (Random.value < panicChanceAfterHit && MacheteController.Instance != null)
        {
            Vector3 away = transform.position - MacheteController.Instance.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f)
            {
                EnterState(State.Run);
                Vector3 dest = transform.position + away.normalized * Random.Range(runDistance.x, runDistance.y);
                targetPos = ClampToBounds(dest);
                zigzagging = true;
                return;
            }
        }
        PickNextState();
    }
    void PickTarget(Vector2 distanceRange)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(distanceRange.x, distanceRange.y);
        Vector3 dest = transform.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * dist;
        targetPos = ClampToBounds(dest);
    }

    Vector3 ClampToBounds(Vector3 p)
    {
        p.x = Mathf.Clamp(p.x, boundsCenter.x - boundsHalfExtents.x, boundsCenter.x + boundsHalfExtents.x);
        p.z = Mathf.Clamp(p.z, boundsCenter.z - boundsHalfExtents.y, boundsCenter.z + boundsHalfExtents.y);
        p.y = transform.position.y;
        return p;
    }
}