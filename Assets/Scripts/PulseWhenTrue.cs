using UnityEngine;

public class PulseWhenTrue : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float minScale = 0.95f;
    public float maxScale = 1.1f;

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        bool active = GameManager.Instance != null && GameManager.Instance.IsOrderUrgent;

        if (active)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = baseScale * scale;
        }
        else
        {
            transform.localScale = baseScale;
        }
    }
}