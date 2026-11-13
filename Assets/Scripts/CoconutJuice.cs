using UnityEngine;

public class CoconutJuice : MonoBehaviour
{
    public bool burstEnabled = true;
    public Color juiceColor = new Color(0.92f, 0.97f, 1f);
    public int hitCount = 10;
    public int deathCount = 22;
    public float speed = 4f;
    public float size = 0.14f;
    public float lifetime = 0.65f;
    [Range(0f, 3f)] public float gravity = 1.5f;
    public float scale = 1f;
    [Range(10f, 120f)] public float coneAngle = 60f;
    [Tooltip("Opcional: si lo asignas, la salpicadura sale desde ahí.")]
    public Transform anchor;

    const float ReferenceRadius = 0.5f;
    const float SurfaceFactor = 0.8f;
    const float DeathSpeedMul = 1.4f;
    const float UpwardBias = 0.2f;

    Renderer rend;

    public void Init(Renderer r) => rend = r;

    public void BurstHit() => Burst(hitCount, 1f);
    public void BurstDeath() => Burst(deathCount, DeathSpeedMul);

    void Burst(int count, float speedMul)
    {
        if (!burstEnabled) return;

        Vector3 pos = transform.position;
        Vector3 dir = Vector3.up;
        float s = scale;

        if (anchor != null)
        {
            pos = anchor.position;
        }
        else if (rend != null)
        {
            Bounds b = rend.bounds;
            float r = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
            s = scale * Mathf.Max(0.5f, r / ReferenceRadius);

            Camera cam = Camera.main;
            Vector3 toCam = cam != null ? (cam.transform.position - b.center).normalized : Vector3.back;
            pos = b.center + toCam * (r * SurfaceFactor);
            dir = (toCam + Vector3.up * UpwardBias).normalized;
        }

        JuiceFX.Burst(pos, juiceColor, count, dir, s, speed, size, lifetime, gravity,
                      coneAngle, speedMul, gameObject.layer);
    }
}