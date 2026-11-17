using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Ajustes")]
    [Tooltip("Desplazamiento máximo (en unidades) cuando la intensidad es 1")]
    public float maxOffset = 0.05f;
    [Tooltip("Qué tan rápido se calma el shake")]
    public float decay = 8f;
    [Tooltip("Velocidad del temblor")]
    public float frequency = 40f;

    float intensity;
    float seed;
    Vector3 lastOffset;

    void Awake()
    {
        Instance = this;
        seed = Random.value * 100f;
    }

    public void Shake(float amount)
    {
        intensity = Mathf.Clamp01(Mathf.Max(intensity, amount));
    }

    void LateUpdate()
    {
        transform.localPosition -= lastOffset;

        intensity = Mathf.MoveTowards(intensity, 0f, decay * Time.unscaledDeltaTime);

        float t = Time.unscaledTime * frequency;
        lastOffset = new Vector3(
            (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f,
            (Mathf.PerlinNoise(seed + 50f, t) - 0.5f) * 2f,
            0f) * (maxOffset * intensity);

        transform.localPosition += lastOffset;
    }
}