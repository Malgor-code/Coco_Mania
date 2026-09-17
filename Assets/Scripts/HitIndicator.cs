using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HitIndicator : MonoBehaviour
{
    [Header("Referencias")]
    public MacheteController machete;

    [Header("Forma")]
    public int segments = 48;
    public float lineWidth = 0.06f;

    [Header("Colores")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.25f);
    public Color chargingColor = new Color(1f, 0.85f, 0.2f, 0.55f);
    public Color impactColor = new Color(1f, 0.3f, 0.2f, 0.95f);
    public float impactFlashDuration = 0.12f;

    [Header("Pulso al comprar rango")]
    public Color radiusGrowColor = new Color(0.3f, 1f, 0.5f, 0.9f);
    public float radiusGrowPulseDuration = 0.35f;
    [Tooltip("Que tanto se pasa del tamano real durante el pulso (0.3 = 30% mas grande)")]
    public float radiusGrowOvershoot = 0.3f;

    private LineRenderer lr;
    private float flashTimer;
    private float radiusPulseTimer;
    private float lastKnownRadius = -1f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = segments;
        lr.widthMultiplier = lineWidth;

        DrawCircle(machete != null ? machete.hitRadius : 1.5f);
    }

    void OnEnable()
    {
        if (machete != null) machete.OnSwingImpact += Flash;
    }

    void OnDisable()
    {
        if (machete != null) machete.OnSwingImpact -= Flash;
    }

    void Update()
    {
        if (machete == null) return;

        float realRadius = machete.hitRadius;

        if (lastKnownRadius >= 0f && !Mathf.Approximately(realRadius, lastKnownRadius))
        {
            radiusPulseTimer = radiusGrowPulseDuration;
        }
        lastKnownRadius = realRadius;

        float drawRadius = realRadius;
        if (radiusPulseTimer > 0f)
        {
            radiusPulseTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(radiusPulseTimer / radiusGrowPulseDuration);
            drawRadius = realRadius * (1f + radiusGrowOvershoot * t);
        }

        DrawCircle(drawRadius);

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            lr.startColor = impactColor;
            lr.endColor = impactColor;
            return;
        }

        if (radiusPulseTimer > 0f)
        {
            lr.startColor = radiusGrowColor;
            lr.endColor = radiusGrowColor;
            return;
        }

        Color c = Color.Lerp(idleColor, chargingColor, machete.SwingProgress01);
        lr.startColor = c;
        lr.endColor = c;
    }

    void DrawCircle(float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    void Flash()
    {
        flashTimer = impactFlashDuration;
    }
}