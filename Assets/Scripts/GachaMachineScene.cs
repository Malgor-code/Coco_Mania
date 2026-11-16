using System;
using System.Collections;
using UnityEngine;

public class GachaMachineScene : MonoBehaviour
{
    public static GachaMachineScene Instance;

    [Header("Referencias")]
    public RewardMachineManager machine;
    [Tooltip("Camara de la escena del gacha. Debe empezar DESACTIVADA (el GameObject).")]
    public Camera gachaCamera;
    [Tooltip("(Opcional) Si queda vacio se usa Camera.main al entrar.")]
    public Camera mainCameraOverride;
    [Tooltip("Animator de la palmera.")]
    public Animator animator;

    [Header("Animator")]
    [Tooltip("Parametro Bool: true = animacion Shake, false = Idle.")]
    public string shakingBool = "Shaking";
    [Tooltip("Parametro Trigger que lanza la animacion Pull (cae el coco y se rompe).")]
    public string pullTrigger = "Pull";
    [Tooltip("Segundos desde el Pam hasta que se muestra el resultado (lo que dura Pull).")]
    public float pullAnimDuration = 1.4f;

    [Header("Agitar el mouse")]
    [Tooltip("Movimiento minimo del mouse por frame (fraccion del ancho de pantalla) para contar.")]
    public float minMouseDelta = 0.004f;
    [Tooltip("Cuanto sube la carga (0-1) cada vez que cambias de direccion.")]
    public float chargePerShake = 0.12f;
    [Tooltip("Cuanto baja la carga por segundo si dejas de agitar.")]
    public float chargeDecay = 0.4f;
    [Tooltip("Velocidad del Shake con la carga casi en 0 y casi en 1.")]
    public float minShakeSpeed = 1f;
    public float maxShakeSpeed = 3.5f;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    [Tooltip("Suena en loop-corto mientras agitas; el pitch sube con la carga.")]
    public AudioClip shakeClip;
    public AudioClip pamClip;

    public bool IsActive { get; private set; }
    public bool IsPulling => pulling;
    public bool IsArmed => armed;
    public float Charge => charge;
    public PullTier CurrentTier => tier;
    public event Action<RewardData, bool> OnPullFinished;
    public event Action OnArmedChanged;

    Camera mainCam;
    bool mainListenerWasOn;
    bool armed, pulling;
    PullTier tier = PullTier.Normal;
    float charge;
    float lastMouseX;
    int lastSign;
    float nextShakeSound;

    void Awake()
    {
        Instance = this;
        if (gachaCamera != null) gachaCamera.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnDisable()
    {
        if (IsActive) Exit();
    }

    public void Enter()
    {
        if (IsActive || gachaCamera == null) return;

        mainCam = mainCameraOverride != null ? mainCameraOverride : Camera.main;
        if (mainCam != null)
        {
            var l = mainCam.GetComponent<AudioListener>();
            mainListenerWasOn = l != null && l.enabled;
            if (l != null) l.enabled = false;
            mainCam.gameObject.SetActive(false);
        }

        gachaCamera.gameObject.SetActive(true);
        var gl = gachaCamera.GetComponent<AudioListener>();
        if (gl != null) gl.enabled = true;

        IsActive = true;
        ResetShake();
        Cursor.visible = true;
        lastMouseX = Input.mousePosition.x;
    }

    public void Exit()
    {
        if (!IsActive) return;
        IsActive = false;
        StopAllCoroutines();
        pulling = false;
        SetArmed(false);
        ResetShake();

        if (gachaCamera != null) gachaCamera.gameObject.SetActive(false);
        if (mainCam != null)
        {
            mainCam.gameObject.SetActive(true);
            var l = mainCam.GetComponent<AudioListener>();
            if (l != null) l.enabled = mainListenerWasOn;
        }
    }

    public bool Arm(PullTier newTier)
    {
        if (!IsActive || pulling || machine == null || !machine.CanPull(newTier)) return false;
        tier = newTier;
        ResetShake();
        SetArmed(true);
        return true;
    }

    public void Disarm()
    {
        if (pulling) return;
        SetArmed(false);
        ResetShake();
    }

    void SetArmed(bool value)
    {
        if (armed == value) return;
        armed = value;
        OnArmedChanged?.Invoke();
    }

    void ResetShake()
    {
        charge = 0f;
        lastSign = 0;
        if (animator != null)
        {
            animator.speed = 1f;
            if (!string.IsNullOrEmpty(shakingBool)) animator.SetBool(shakingBool, false);
        }
    }

    void Update()
    {
        float mx = Input.mousePosition.x;
        float dx = (mx - lastMouseX) / Mathf.Max(1f, Screen.width);
        lastMouseX = mx;

        if (!IsActive || !armed || pulling) return;

        if (Mathf.Abs(dx) > minMouseDelta)
        {
            int sign = dx > 0f ? 1 : -1;
            if (lastSign != 0 && sign != lastSign) charge += chargePerShake;
            lastSign = sign;
        }

        charge = Mathf.Max(0f, charge - chargeDecay * Time.unscaledDeltaTime);

        bool shaking = charge > 0.02f;
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(shakingBool)) animator.SetBool(shakingBool, shaking);
            animator.speed = shaking ? Mathf.Lerp(minShakeSpeed, maxShakeSpeed, Mathf.Clamp01(charge)) : 1f;
        }

        if (shaking && audioSource != null && shakeClip != null && Time.unscaledTime >= nextShakeSound)
        {
            audioSource.pitch = Mathf.Lerp(0.9f, 1.6f, Mathf.Clamp01(charge));
            audioSource.PlayOneShot(shakeClip, 0.6f);
            nextShakeSound = Time.unscaledTime + Mathf.Lerp(0.35f, 0.1f, Mathf.Clamp01(charge));
        }

        if (charge >= 1f) StartCoroutine(PullRoutine());
    }

    IEnumerator PullRoutine()
    {
        pulling = true;
        SetArmed(false);

        // ¡PAM!
        if (animator != null)
        {
            animator.speed = 1f;
            if (!string.IsNullOrEmpty(shakingBool)) animator.SetBool(shakingBool, false);
            if (!string.IsNullOrEmpty(pullTrigger)) animator.SetTrigger(pullTrigger);
        }
        if (audioSource != null && pamClip != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(pamClip);
        }

        RewardData result = null;
        bool duplicate = false;
        void Handler(RewardData d, bool dup) { result = d; duplicate = dup; }

        machine.OnPullResult += Handler;
        machine.Pull(tier);
        machine.OnPullResult -= Handler;

        charge = 0f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, pullAnimDuration));

        pulling = false;
        OnPullFinished?.Invoke(result, duplicate);
    }
}