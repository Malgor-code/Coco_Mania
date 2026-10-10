using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HitIndicator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si lo dejas vacio se usa MacheteController.Instance")]
    public MacheteController machete;

    [Header("Forma")]
    public int segments = 64;
    public float lineWidth = 0.15f;
    public float heightOffset = 0.1f;

    [Header("Material")]
    [Tooltip("Crea un material Sprites/Default (evita el color morado/magenta).")]
    public bool forceDefaultMaterial = true;

    [Header("Colores (sincronizados con la animacion)")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.35f);
    public Color chargingColor = new Color(1f, 0.85f, 0.2f, 0.8f);
    public Color impactColor = new Color(1f, 0.3f, 0.2f, 1f);
    public float impactFlashDuration = 0.12f;
    [Tooltip("Tiempo que tarda en volver de rojo a blanco despues del flash.")]
    public float impactFadeDuration = 0.3f;
    [Tooltip("Cuanto 'salta' el circulo al impactar (0.1 = 10% mas grande).")]
    public float impactPunch = 0.1f;

    [Header("Pulso al comprar rango")]
    public Color radiusGrowColor = new Color(0.3f, 1f, 0.5f, 1f);
    public float radiusGrowPulseDuration = 0.35f;
    [Tooltip("Que tanto se pasa del tamano real durante el pulso (0.3 = 30% mas grande)")]
    public float radiusGrowOvershoot = 0.3f;

    private LineRenderer lr;
    private bool subscribed;
    private float timeSinceImpact = 999f;
    private float radiusPulseTimer;
    private float lastKnownRadius = -1f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.alignment = LineAlignment.TransformZ;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCornerVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sortingOrder = 10;

        if (forceDefaultMaterial || lr.sharedMaterial == null)
            lr.material = new Material(Shader.Find("Sprites/Default"));

        Bind();
        DrawCircle(machete != null ? machete.hitRadius : 1.5f);
    }

    void OnEnable()
    {
        Bind();
    }

    void OnDisable()
    {
        if (machete != null && subscribed)
        {
            machete.OnSwingImpact -= OnImpact;
            subscribed = false;
        }
    }

    void Bind()
    {
        if (machete == null) machete = MacheteController.Instance;
        if (machete != null && !subscribed)
        {
            machete.OnSwingImpact += OnImpact;
            subscribed = true;
        }
    }

    void Update()
    {
        if (machete == null)
        {
            Bind();
            if (machete == null) return;
        }

        float dt = Time.deltaTime;
        float realRadius = machete.hitRadius;
        if (lastKnownRadius >= 0f && !Mathf.Approximately(realRadius, lastKnownRadius))
            radiusPulseTimer = radiusGrowPulseDuration;
        lastKnownRadius = realRadius;
        float drawRadius = realRadius;
        if (radiusPulseTimer > 0f)
        {
            radiusPulseTimer -= dt;
            float t = Mathf.Clamp01(radiusPulseTimer / radiusGrowPulseDuration);
            drawRadius *= 1f + radiusGrowOvershoot * t;
        }

        timeSinceImpact += dt;
        if (timeSinceImpact < impactFlashDuration)
        {
            float f = 1f - timeSinceImpact / impactFlashDuration;
            drawRadius *= 1f + impactPunch * f;
        }

        DrawCircle(drawRadius);

        // Color
        Color c;
        float totalFade = impactFlashDuration + impactFadeDuration;

        if (radiusPulseTimer > 0f)
        {
            c = radiusGrowColor;
        }
        else if (timeSinceImpact < totalFade)
        {
            float t = Mathf.Clamp01((timeSinceImpact - impactFlashDuration) / Mathf.Max(0.01f, impactFadeDuration));
            c = Color.Lerp(impactColor, idleColor, t);
        }
        else
        {
            float elapsed = machete.SwingProgress01 * machete.swingInterval;
            float impactTime = Mathf.Max(0.01f,
                machete.swingInterval * machete.swingAnimDurationFraction * machete.impactNormalizedTime);

            if (elapsed < impactTime)
            {
                float windup = Mathf.Clamp01(elapsed / impactTime);
                c = Color.Lerp(idleColor, chargingColor, windup * windup);
            }
            else
            {
                c = idleColor;
            }
        }

        lr.startColor = c;
        lr.endColor = c;
    }

    void DrawCircle(float radius)
    {
        Vector3 s = transform.lossyScale;
        float sx = Mathf.Approximately(s.x, 0f) ? 1f : s.x;
        float sz = Mathf.Approximately(s.z, 0f) ? 1f : s.z;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius / sx,
                heightOffset,
                Mathf.Sin(angle) * radius / sz));
        }
    }

    void OnImpact()
    {
        timeSinceImpact = 0f;
    }
}