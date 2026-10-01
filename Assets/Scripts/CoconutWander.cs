using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CoconutWander : MonoBehaviour
{
    public float speed = 1.5f;
    public float retargetInterval = 2.5f;
    public float rotationSpeed = 8f;
    public bool debugLog = false;

    private Vector3 targetPos;
    private float timer;
    private Vector3 boundsCenter;
    private Vector2 boundsHalfExtents = new Vector2(8f, 8f);
    private bool boundsSet = false;
    private float stunTimer = 0f;
    private Rigidbody rb;

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
    }

    void Start()
    {
        if (!boundsSet)
        {
            boundsCenter = transform.position;
        }
        PickNewTarget();
        rb.WakeUp();
    }

    public void SetBounds(Vector3 center, Vector2 halfExtents)
    {
        boundsCenter = center;
        boundsHalfExtents = halfExtents;
        boundsSet = true;
        PickNewTarget();
    }

    public void Stun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
    }

    void StopMoving()
    {
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        bool hitting = GameManager.Instance == null || GameManager.Instance.IsHittingPhase();
        bool dayActive = CoconutSpawner.Instance != null && CoconutSpawner.Instance.DayActive;

        if (debugLog)
        {
            Debug.Log($"{name} hitting={hitting} dayActive={dayActive} stun={stunTimer} " +
                      $"kinematic={rb.isKinematic} timeScale={Time.timeScale}");
        }

        if (!hitting && !dayActive)
        {
            StopMoving();
            return;
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            StopMoving();
            return;
        }

        timer += Time.fixedDeltaTime;
        if (timer >= retargetInterval || Vector3.Distance(transform.position, targetPos) < 0.3f)
        {
            timer = 0f;
            PickNewTarget();
        }

        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.magnitude > 0.05f)
        {
            Vector3 velocity = dir.normalized * speed;

            if (rb.isKinematic)
                rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
            else
                rb.linearVelocity = velocity;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }
        else
        {
            StopMoving();
        }
    }

    void PickNewTarget()
    {
        float x = Random.Range(boundsCenter.x - boundsHalfExtents.x, boundsCenter.x + boundsHalfExtents.x);
        float z = Random.Range(boundsCenter.z - boundsHalfExtents.y, boundsCenter.z + boundsHalfExtents.y);
        targetPos = new Vector3(x, transform.position.y, z);
    }
}