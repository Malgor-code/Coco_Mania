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

    private LineRenderer lr;
    private float flashTimer;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = segments;
        lr.widthMultiplier = lineWidth;

        DrawCircle();
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

        // El radio del anillo siempre refleja el hitRadius real del machete
        DrawCircle();

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            lr.startColor = impactColor;
            lr.endColor = impactColor;
            return;
        }

        Color c = Color.Lerp(idleColor, chargingColor, machete.SwingProgress01);
        lr.startColor = c;
        lr.endColor = c;
    }

    void DrawCircle()
    {
        float radius = machete != null ? machete.hitRadius : 1.5f;
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