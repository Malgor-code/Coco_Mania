using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CoconutWander : MonoBehaviour
{
    public float speed = 1.5f;
    public float retargetInterval = 2.5f;
    public float rotationSpeed = 8f; 

    private Vector3 targetPos;
    private float timer;
    private Vector3 boundsCenter;
    private Vector2 boundsHalfExtents = new Vector2(8f, 8f);
    private float stunTimer = 0f;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        boundsCenter = transform.position;
        PickNewTarget();
    }

    public void SetBounds(Vector3 center, Vector2 halfExtents)
    {
        boundsCenter = center;
        boundsHalfExtents = halfExtents;
        PickNewTarget();
    }

    public void Stun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsHittingPhase())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector3.zero;
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

        rb.linearVelocity = dir.magnitude > 0.05f ? dir.normalized * speed : Vector3.zero;

        if (dir.magnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void PickNewTarget()
    {
        float x = Random.Range(boundsCenter.x - boundsHalfExtents.x, boundsCenter.x + boundsHalfExtents.x);
        float z = Random.Range(boundsCenter.z - boundsHalfExtents.y, boundsCenter.z + boundsHalfExtents.y);
        targetPos = new Vector3(x, transform.position.y, z);
    }
}