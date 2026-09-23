using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MacheteController : MonoBehaviour
{
    public static MacheteController Instance;

    [Header("Movimiento (seguir mouse)")]
    [Tooltip("Mas bajo = respuesta mas precisa/rapida. Mas alto = mas flotante.")]
    public float smoothTime = 0.05f;
    public float maxMoveSpeed = 60f;

    [Header("Rotacion (mira hacia donde se mueve, no fijo)")]
    [Tooltip("Que tan rapido gira para encarar la direccion de movimiento.")]
    public float turnSpeed = 12f;
    [Tooltip("Velocidad minima (unidades/seg) para considerar que se esta moviendo y empezar a girar. Evita que tiemble cuando esta casi quieto.")]
    public float minSpeedToTurn = 0.15f;

    [Header("Golpe automatico")]
    public float swingInterval = 1.2f;
    public float hitRadius = 1.5f;

    [Header("Recorrido del golpe (atras -> pega -> vuelve)")]
    [Tooltip("Que fraccion de swingInterval ocupa la animacion completa. El resto queda en reposo antes de arrancar el siguiente golpe. Al bajar swingInterval (mejoras de velocidad), esto se acelera automaticamente.")]
    [Range(0.3f, 1f)] public float swingAnimDurationFraction = 0.85f;
    [Tooltip("De ese tiempo total, que fraccion es el 'ir hacia atras' antes de golpear.")]
    [Range(0.05f, 0.6f)] public float windUpRatio = 0.24f;
    [Tooltip("De ese tiempo total, que fraccion es el golpe en si (de atras a impacto). El daño se aplica justo al EMPEZAR esta fase.")]
    [Range(0.05f, 0.6f)] public float strikeRatio = 0.30f;
    public Vector3 windUpEuler = new Vector3(-25f, 0f, 0f);
    public Vector3 impactEuler = new Vector3(65f, 0f, 0f);

    [Header("Sonido de golpe (opcional)")]
    public AudioClip[] swingSounds;
    [Range(0f, 0.3f)] public float swingPitchVariation = 0.08f;
    private AudioSource audioSource;
    [HideInInspector] public int damageOverride = 3;
    [HideInInspector] public float swingStaminaCostOverride = 1f;

    [Header("Visual (hijo con la malla, para animar sin mover el ancla)")]
    public Transform visual;

    [Header("Pivote de agarre (para que gire desde el MANGO, no desde el centro de la malla)")]
    [Tooltip("Punto de agarre del machete, en el espacio LOCAL de 'visual' (con su posicion/rotacion de reposo, sin animar). Si tu mango esta, por ejemplo, mas atras y abajo del centro del modelo, proba algo como (0, -0.3, -0.5) y ajusta viendo el gizmo cian en la Scene view. (0,0,0) = comportamiento viejo, gira desde el centro de la malla.")]
    public Vector3 pivotOffset = Vector3.zero;

    private Vector3 visualRestLocalPosition;

    [Header("Camara: sigue un poco al cursor (opcional)")]
    [Tooltip("Arrastra la Main Camera (o un rig que la contenga). Dejalo vacio para desactivar este efecto.")]
    public Transform cameraToFollow;
    [Tooltip("Que tanto se desplaza la camara respecto a cuanto se aleja el machete de su posicion inicial. 0 = camara fija en X/Z.")]
    public float cameraFollowStrength = 0.15f;
    [Tooltip("Maximo desplazamiento de la camara respecto a su posicion base.")]
    public float cameraFollowMaxOffset = 2.5f;
    public float cameraFollowSmoothTime = 0.25f;

    [Header("Camara: shake en cada golpe (usa el mismo 'Camera To Follow')")]
    [Tooltip("Cuanto 'trauma' (intensidad de shake) agrega CADA golpe del machete, se conecte o no. Muy bajo a proposito: es un detalle pasivo, no un golpe de camara.")]
    [Range(0f, 1f)] public float shakeTraumaPerSwing = 0.06f;
    [Tooltip("Que tan rapido decae el shake despues de un golpe.")]
    public float shakeDecaySpeed = 4f;
    public float shakeMaxOffset = 0.04f;

    [Header("Feedback de fallo (vineta roja en las esquinas cuando el golpe no conecta)")]
    [Tooltip("Imagen UI full-screen (esquinas/borde rojo) que parpadea cuando el golpe no le pega a ningun coco. Su alpha en reposo deberia ser 0. Dejala vacia para desactivar este efecto.")]
    public Image missVignetteImage;
    public float missFlashInDuration = 0.05f;
    public float missFlashOutDuration = 0.35f;
    [Range(0f, 1f)] public float missFlashMaxAlpha = 0.55f;

    private float timer;
    private Plane groundPlane;
    private Vector3 followVelocity;

    private Vector3 macheteRestPosition;
    private Vector3 cameraBasePosition;
    private Vector3 smoothedCameraFollowPos;
    private Vector3 cameraFollowVelocity;

    private float shakeTrauma = 0f;
    private Vector3 cameraShakeOffset = Vector3.zero;

    private Coroutine missFlashCoroutine;

    public float SwingProgress01 => Mathf.Clamp01(timer / swingInterval);

    public System.Action OnSwingImpact;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (visual != null) visualRestLocalPosition = visual.localPosition;
    }

    void Start()
    {
        groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        macheteRestPosition = transform.position;

        if (cameraToFollow != null)
        {
            cameraBasePosition = cameraToFollow.position;
            smoothedCameraFollowPos = cameraBasePosition;
        }

        if (missVignetteImage != null)
        {
            Color c = missVignetteImage.color;
            c.a = 0f;
            missVignetteImage.color = c;
            if (!missVignetteImage.gameObject.activeSelf) missVignetteImage.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        bool hitting = GameManager.Instance != null && GameManager.Instance.IsHittingPhase();
        bool gameStarted = GameManager.Instance != null && GameManager.Instance.currentPhase != GameManager.Phase.MainMenu;

        if (gameStarted)
        {
            FollowMouse();
        }

        UpdateCameraFollowAndShake(gameStarted);
        if (hitting)
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

            Vector3 flatVel = followVelocity;
            flatVel.y = 0f;
            if (flatVel.sqrMagnitude > minSpeedToTurn * minSpeedToTurn)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }
    }

    void UpdateCameraFollowAndShake(bool active)
    {
        if (shakeTrauma > 0f)
        {
            shakeTrauma = Mathf.Max(0f, shakeTrauma - shakeDecaySpeed * Time.deltaTime);
        }

        if (!active) return;

        if (cameraToFollow == null) return;

        float amount = shakeTrauma * shakeTrauma;
        cameraShakeOffset = amount > 0f
            ? new Vector3(
                (Mathf.PerlinNoise(Time.time * 35f, 0.37f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0.91f, Time.time * 35f) - 0.5f) * 2f,
                0f) * shakeMaxOffset * amount
            : Vector3.zero;

        Vector3 offset = transform.position - macheteRestPosition;
        offset.y = 0f;
        if (offset.magnitude > cameraFollowMaxOffset) offset = offset.normalized * cameraFollowMaxOffset;

        Vector3 targetCamPos = cameraBasePosition + offset * cameraFollowStrength;
        smoothedCameraFollowPos = Vector3.SmoothDamp(smoothedCameraFollowPos, targetCamPos, ref cameraFollowVelocity, cameraFollowSmoothTime);

        cameraToFollow.position = smoothedCameraFollowPos + cameraShakeOffset;
    }

    void TrySwing()
    {
        timer = 0f;

        StopCoroutine(nameof(SwingAnim));
        StartCoroutine(SwingAnim());
    }

    void ApplyHit()
    {
        bool hitSomething = false;
        bool killedSomething = false;

        if (CoconutSpawner.Instance != null)
        {
            foreach (var coco in CoconutSpawner.Instance.GetActiveCoconuts())
            {
                if (coco == null) continue;

                Vector3 a = transform.position; a.y = 0f;
                Vector3 b = coco.transform.position; b.y = 0f;

                if (Vector3.Distance(a, b) <= hitRadius)
                {
                    int finalDamage = damageOverride;

                    bool isCritical = false;

                    if (GameManager.Instance != null)
                    {
                        isCritical = Random.value < GameManager.Instance.GetCritChance();

                        if (isCritical)
                        {
                            finalDamage *= 2;
                        }
                    }

                    bool killed = coco.TakeDamage(finalDamage);
                    hitSomething = true;
                    if (killed) killedSomething = true;
                }
            }
        }

        shakeTrauma = Mathf.Clamp01(shakeTrauma + shakeTraumaPerSwing);

        if (!hitSomething)
        {
            TriggerMissFlash();
        }

        OnSwingImpact?.Invoke();
        PlaySwingSound();

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.OnSwingConnect(killedSomething);
        }
    }

    void TriggerMissFlash()
    {
        if (missVignetteImage == null) return;

        if (missFlashCoroutine != null) StopCoroutine(missFlashCoroutine);
        missFlashCoroutine = StartCoroutine(FlashMissVignette());
    }

    IEnumerator FlashMissVignette()
    {
        Color c = missVignetteImage.color;

        float t = 0f;
        while (t < missFlashInDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, missFlashMaxAlpha, t / missFlashInDuration);
            missVignetteImage.color = c;
            yield return null;
        }

        t = 0f;
        while (t < missFlashOutDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(missFlashMaxAlpha, 0f, t / missFlashOutDuration);
            missVignetteImage.color = c;
            yield return null;
        }

        c.a = 0f;
        missVignetteImage.color = c;
    }

    void PlaySwingSound()
    {
        if (swingSounds == null || swingSounds.Length == 0 || audioSource == null) return;
        audioSource.pitch = 1f + Random.Range(-swingPitchVariation, swingPitchVariation);
        audioSource.PlayOneShot(swingSounds[Random.Range(0, swingSounds.Length)]);
    }

    IEnumerator SwingAnim()
    {
        float totalDuration = Mathf.Max(0.05f, swingInterval * swingAnimDurationFraction);
        float upTime = totalDuration * windUpRatio;
        float downTime = totalDuration * strikeRatio;
        float backTime = Mathf.Max(0.01f, totalDuration - upTime - downTime);

        Quaternion rest = Quaternion.identity;
        Quaternion windUp = Quaternion.Euler(windUpEuler);
        Quaternion impact = Quaternion.Euler(impactEuler);

        float t = 0f;
        while (t < upTime)
        {
            t += Time.deltaTime;
            SetVisualPivotRotation(Quaternion.Lerp(rest, windUp, t / upTime));
            yield return null;
        }

        ApplyHit();

        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            SetVisualPivotRotation(Quaternion.Lerp(windUp, impact, t / downTime));
            yield return null;
        }

        t = 0f;
        while (t < backTime)
        {
            t += Time.deltaTime;
            SetVisualPivotRotation(Quaternion.Lerp(impact, rest, t / backTime));
            yield return null;
        }

        SetVisualPivotRotation(rest);
    }

    void SetVisualPivotRotation(Quaternion rotation)
    {
        if (visual == null) return;

        visual.localRotation = rotation;

        Vector3 gripInParent = visualRestLocalPosition + pivotOffset;
        visual.localPosition = gripInParent - (rotation * pivotOffset);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);

        if (visual != null && visual.parent != null)
        {
            Vector3 restLocalPos = Application.isPlaying ? visualRestLocalPosition : visual.localPosition;
            Vector3 gripWorld = visual.parent.TransformPoint(restLocalPos + pivotOffset);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(gripWorld, 0.04f);
            Gizmos.DrawLine(visual.position, gripWorld);
        }
    }
}