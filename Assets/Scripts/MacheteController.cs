using UnityEngine;
using System.Collections;

public class MacheteController : MonoBehaviour
{
    public static MacheteController Instance;

    [Header("Movimiento (seguir mouse)")]
    [Tooltip("Mas bajo = respuesta mas precisa/rapida. Mas alto = mas flotante.")]
    public float smoothTime = 0.05f;
    public float maxMoveSpeed = 60f;

    [Header("Golpe automatico")]
    public float swingInterval = 1.2f;
    public float hitRadius = 1.5f;

    [Header("Sonido de golpe (opcional)")]
    public AudioClip[] swingSounds;
    [Range(0f, 0.3f)] public float swingPitchVariation = 0.08f;
    private AudioSource audioSource;
    [HideInInspector] public int damageOverride = 3;
    [HideInInspector] public float swingStaminaCostOverride = 1f;

    [Header("Visual (hijo con la malla, para animar sin mover el ancla)")]
    public Transform visual;

    private float timer;
    private Plane groundPlane;
    private Vector3 followVelocity;

    public float SwingProgress01 => Mathf.Clamp01(timer / swingInterval);

    public System.Action OnSwingImpact;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

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
                TrySwing();
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

    void TrySwing()
    {
        if (CoconutSpawner.Instance == null || GameManager.Instance == null) return;

        bool hitSomething = false;
        bool killedSomething = false;

        foreach (var coco in CoconutSpawner.Instance.GetActiveCoconuts())
        {
            if (coco == null) continue;

            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = coco.transform.position; b.y = 0f;

            if (Vector3.Distance(a, b) <= hitRadius)
            {
                bool killed = coco.TakeDamage(damageOverride);
                hitSomething = true;
                if (killed) killedSomething = true;
            }
        }

        if (!hitSomething) return;

        timer = 0f;
        StopCoroutine(nameof(SwingAnim));
        StartCoroutine(SwingAnim());

        GameManager.Instance.UseStaminaForSwing(swingStaminaCostOverride);
        OnSwingImpact?.Invoke();

        PlaySwingSound();

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.OnSwingConnect(killedSomething);
        }
    }

    void PlaySwingSound()
    {
        if (swingSounds == null || swingSounds.Length == 0 || audioSource == null) return;
        audioSource.pitch = 1f + Random.Range(-swingPitchVariation, swingPitchVariation);
        audioSource.PlayOneShot(swingSounds[Random.Range(0, swingSounds.Length)]);
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