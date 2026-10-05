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
    public float turnSpeed = 12f;
    public float minSpeedToTurn = 0.15f;

    [Header("Golpe automatico")]
    public float swingInterval = 1.2f;
    public float hitRadius = 1.5f;

    [Header("Animacion del golpe (Animator)")]
    [Tooltip("Animator del machete (normalmente en el hijo con la malla). Si lo dejas vacio se busca en los hijos.")]
    public Animator animator;
    [Tooltip("Nombre EXACTO del estado del golpe en el Animator Controller.")]
    public string swingStateName = "Swing";
    [Tooltip("Nombre del clip de animacion. Si lo dejas vacio se usa el mismo nombre del estado.")]
    public string swingClipName = "";
    [Tooltip("Duracion del clip a velocidad 1, en segundos. Si es 0 se detecta automaticamente por el nombre del clip.")]
    public float swingClipLengthOverride = 0f;
    [Tooltip("Que fraccion de swingInterval ocupa la animacion completa. Al bajar swingInterval la animacion se acelera sola.")]
    [Range(0.3f, 1f)] public float swingAnimDurationFraction = 0.85f;
    [Tooltip("Velocidad minima/maxima permitida para la animacion (evita que se vea absurda).")]
    public float minAnimSpeed = 0.5f;
    public float maxAnimSpeed = 6f;
    [Tooltip("Opcional: parametro float del Animator para la velocidad del golpe (ver Speed > Parameter en el estado). Si lo dejas vacio se usa animator.speed.")]
    public string speedParameterName = "";
    [Header("Momento del impacto")]
    [Tooltip("Si esta activo, el daño se aplica cuando la animacion llama al evento 'AnimationImpact' (pon un Animation Event en el frame del golpe y agrega MacheteAnimationRelay al objeto del Animator). Si esta apagado se usa 'Impact Normalized Time'.")]
    public bool useAnimationEvent = false;
    [Tooltip("En que punto del clip ocurre el golpe (0 = inicio, 1 = final). Ej: 0.45")]
    [Range(0f, 1f)] public float impactNormalizedTime = 0.45f;

    [Header("Sonido de golpe (opcional)")]
    public AudioClip[] swingSounds;
    [Range(0f, 0.3f)] public float swingPitchVariation = 0.08f;
    private AudioSource audioSource;
    [HideInInspector] public int damageOverride = 3;

    [Header("Camara: sigue un poco al cursor (opcional)")]
    public Transform cameraToFollow;
    public float cameraFollowStrength = 0.15f;
    public float cameraFollowMaxOffset = 2.5f;
    public float cameraFollowSmoothTime = 0.25f;

    [Header("Camara: shake en cada golpe (usa el mismo 'Camera To Follow')")]
    [Range(0f, 1f)] public float shakeTraumaPerSwing = 0.06f;
    public float shakeDecaySpeed = 4f;
    public float shakeMaxOffset = 0.04f;

    [Header("Feedback de fallo (vineta roja cuando el golpe no conecta)")]
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
    private Coroutine impactRoutine;
    private bool impactPending = false;
    private float cachedClipLength = 0f;

    public float SwingProgress01 => Mathf.Clamp01(timer / swingInterval);

    public System.Action OnSwingImpact;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        cachedClipLength = ResolveClipLength();
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

    float ResolveClipLength()
    {
        if (swingClipLengthOverride > 0f) return swingClipLengthOverride;
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;

        string clipName = string.IsNullOrEmpty(swingClipName) ? swingStateName : swingClipName;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == clipName) return clip.length;
        }

        Debug.LogWarning("[MacheteController] No encontre el clip '" + clipName +
                         "'. Escribe el nombre correcto en 'Swing Clip Name' o pon la duracion en 'Swing Clip Length Override'.");
        return 0f;
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
    void OnEnable()
    {
        timer = 0f;
        impactPending = false;
        if (impactRoutine != null) { StopCoroutine(impactRoutine); impactRoutine = null; }
        if (animator != null) SetAnimSpeed(1f);
    }

    void SetAnimSpeed(float s)
    {
        if (!string.IsNullOrEmpty(speedParameterName)) animator.SetFloat(speedParameterName, s);
        else animator.speed = s;
    }
    void TrySwing()
    {
        timer = 0f;

        if (impactRoutine != null) { StopCoroutine(impactRoutine); impactRoutine = null; }
        if (impactPending) { impactPending = false; ApplyHit(); }

        float targetDuration = Mathf.Max(0.05f, swingInterval * swingAnimDurationFraction);

        if (animator != null)
        {
            if (cachedClipLength <= 0f) cachedClipLength = ResolveClipLength();

            if (cachedClipLength > 0f)
            {
                float speed = Mathf.Clamp(cachedClipLength / targetDuration, minAnimSpeed, maxAnimSpeed);
                SetAnimSpeed(speed);
                targetDuration = cachedClipLength / speed;
            }

            animator.Play(swingStateName, 0, 0f);
            animator.Update(0f);
        }

        impactPending = true;
        float delay = useAnimationEvent ? targetDuration : targetDuration * impactNormalizedTime;
        impactRoutine = StartCoroutine(ImpactAfterDelay(delay));
    }

    IEnumerator ImpactAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        impactRoutine = null;
        if (impactPending)
        {
            if (useAnimationEvent)
                Debug.LogWarning("[MacheteController] El Animation Event 'AnimationImpact' no llego. Revisa el evento y el script Relay.");
            impactPending = false;
            ApplyHit();
        }
    }

    public void AnimationImpact()
    {
        if (!useAnimationEvent) return;
        if (!impactPending) return;
        impactPending = false;
        ApplyHit();
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
                        float dmg = finalDamage;
                        if (isCritical)
                        {
                            dmg *= GameManager.Instance.GetCritMultiplier();
                            dmg *= GameManager.Instance.GetLastBreathMultiplier();
                        }
                        dmg *= GameManager.Instance.GetStreakDamageMultiplier();
                        finalDamage = Mathf.Max(1, Mathf.RoundToInt(dmg));
                    }

                    bool killed = coco.TakeDamage(finalDamage, isCritical);
                    hitSomething = true;

                    if (killed) killedSomething = true;
                }
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSwingResult(hitSomething);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}