using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class StaminaSliderFX : MonoBehaviour
{
    public enum RotationAxis { Z_Giro, X_InclinarAdelanteAtras, Y_Voltear }

    [Header("Referencias")]
    [SerializeField] Slider slider;
    [Tooltip("Si se deja vacío usa slider.handleRect")]
    [SerializeField] RectTransform handle;

    [Header("Movimiento vertical del slider")]
    [SerializeField] bool enableVerticalMotion = true;
    [SerializeField] float idleAmplitude = 2f; 
    [SerializeField] float idleSpeed = 2f;
    [SerializeField] float pulseAmplitude = 8f; 
    [SerializeField] float pulseFrequency = 18f;
    [SerializeField] float pulseDecay = 4f; 

    [Header("Rotación del Handle")]
    [SerializeField] RotationAxis axis = RotationAxis.Z_Giro;
    [SerializeField] float handleIdleAngle = 15f; 
    [SerializeField] float handleIdleSpeed = 3f;
    [SerializeField] float handlePulseAngle = 35f; 
    [SerializeField] float handlePulseFrequency = 14f;
    [Tooltip("Hace que el handle gire sobre su centro (sin que se mueva de lugar)")]
    [SerializeField] bool rotateAroundCenter = true;

    RectTransform rect;
    Vector2 baseAnchoredPos;
    Quaternion handleBaseRot;
    float pulse;  
    float pulseTime;
    float lastValue;

    void Reset()
    {
        slider = GetComponent<Slider>();
    }

    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        rect = (RectTransform)transform;
        if (handle == null && slider != null) handle = slider.handleRect;

        if (handle != null && rotateAroundCenter)
        {
            Vector2 newPivot = new Vector2(0.5f, 0.5f);
            Vector2 deltaPivot = newPivot - handle.pivot;
            handle.pivot = newPivot;
            handle.anchoredPosition += new Vector2(deltaPivot.x * handle.rect.width,
                                                   deltaPivot.y * handle.rect.height);
        }
    }

    void OnEnable()
    {
        baseAnchoredPos = rect.anchoredPosition;
        if (handle != null) handleBaseRot = handle.localRotation;
        lastValue = slider.value;
        pulse = 0f;
        slider.onValueChanged.AddListener(OnValueChanged);
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnValueChanged);
        rect.anchoredPosition = baseAnchoredPos;
        if (handle != null) handle.localRotation = handleBaseRot;
    }

    void OnValueChanged(float v)
    {
        float range = Mathf.Max(0.0001f, slider.maxValue - slider.minValue);
        float delta = Mathf.Abs(v - lastValue) / range;
        lastValue = v;

        pulse = Mathf.Clamp01(pulse + Mathf.Clamp01(delta * 10f + 0.3f));
        pulseTime = 0f;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        pulse = Mathf.MoveTowards(pulse, 0f, pulseDecay * dt * 0.5f);
        pulseTime += dt;

        if (enableVerticalMotion)
        {
            float idle = Mathf.Sin(t * idleSpeed) * idleAmplitude;
            float bounce = Mathf.Sin(pulseTime * pulseFrequency) * pulseAmplitude * pulse;
            rect.anchoredPosition = baseAnchoredPos + new Vector2(0f, idle + bounce);
        }

        if (handle != null)
        {
            float sway = Mathf.Sin(t * handleIdleSpeed) * handleIdleAngle;
            float swayPulse = Mathf.Sin(pulseTime * handlePulseFrequency) * handlePulseAngle * pulse;
            float angle = sway + swayPulse;

            Vector3 euler = axis == RotationAxis.Z_Giro ? new Vector3(0f, 0f, angle)
                          : axis == RotationAxis.X_InclinarAdelanteAtras ? new Vector3(angle, 0f, 0f)
                          : new Vector3(0f, angle, 0f);

            handle.localRotation = handleBaseRot * Quaternion.Euler(euler);
        }
    }
}