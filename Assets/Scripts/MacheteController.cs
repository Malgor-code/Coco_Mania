using UnityEngine;
using System.Collections;

public class MacheteController : MonoBehaviour
{
    [Header("Movimiento (seguir mouse)")]
    [Tooltip("Mas bajo = respuesta mas precisa/rapida. Mas alto = mas flotante.")]
    public float smoothTime = 0.05f;
    public float maxMoveSpeed = 60f;

    [Header("Golpe automatico")]
    public float swingInterval = 1.2f;
    public float hitRadius = 1.5f;

    [Header("Visual (hijo con la malla, para animar sin mover el ancla)")]
    public Transform visual;

    private float timer;
    private Plane groundPlane;
    private Vector3 followVelocity;

    public float SwingProgress01 => Mathf.Clamp01(timer / swingInterval);

    public System.Action OnSwingImpact;

    void Start()
    {
        groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
    }

    void Update()
    {
        FollowMouse();

        if (GameManager.Instance != null && GameManager.Instance.IsHittingPhase())
        {
            timer += Time.deltaTime;
            if (timer >= swingInterval)
            {
                timer = 0f;
                Swing();
            }
        }
    }

    void FollowMouse()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            transform.position = Vector3.SmoothDamp(transform.position, targetPoint, ref followVelocity, smoothTime, maxMoveSpeed);
        }
    }

    void Swing()
    {
        StopCoroutine(nameof(SwingAnim));
        StartCoroutine(SwingAnim());

        if (CoconutSpawner.Instance == null || GameManager.Instance == null) return;

        foreach (var coco in CoconutSpawner.Instance.GetActiveCoconuts())
        {
            if (coco == null) continue;

            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = coco.transform.position; b.y = 0f;

            if (Vector3.Distance(a, b) <= hitRadius)
            {
                coco.TakeDamage(GameManager.Instance.damage);
            }
        }

        GameManager.Instance.UseStaminaForSwing();
        OnSwingImpact?.Invoke();
    }

    IEnumerator SwingAnim()
    {
        if (visual == null) yield break;

        Quaternion rest = Quaternion.identity;
        Quaternion windUp = Quaternion.Euler(-25f, 0f, 0f);
        Quaternion impact = Quaternion.Euler(65f, 0f, 0f);

        float upTime = 0.08f;
        float downTime = 0.10f;
        float backTime = 0.15f;
        float t;

        t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            visual.localRotation = Quaternion.Lerp(rest, windUp, t / upTime);
            yield return null;
        }

        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            visual.localRotation = Quaternion.Lerp(windUp, impact, t / downTime);
            yield return null;
        }

        t = 0f;
        while (t < backTime)
        {
            t += Time.deltaTime;
            visual.localRotation = Quaternion.Lerp(impact, rest, t / backTime);
            yield return null;
        }

        visual.localRotation = rest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}