using System.Collections;
using UnityEngine;

public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance;

    [Header("Camara (si la dejas vacia, usa Camera.main)")]
    public Camera targetCamera;

    [Header("Hit Stop (congelar un instante en el impacto)")]
    public float hitStopDurationNormal = 0.02f;
    public float hitStopDurationKill = 0.06f;
    [Tooltip("A que velocidad queda el tiempo DURANTE el hit-stop (no 0 total, para que no se vea como un freeze feo)")]
    public float hitStopTimeScale = 0.02f;

    [Header("Camera Shake")]
    public float shakeIntensityNormal = 0.06f;
    public float shakeIntensityKill = 0.18f;
    public float shakeDuration = 0.15f;

    [Header("Numeros de dano flotantes (opcional)")]
    [Tooltip("Prefab con un TextMeshPro (3D, world space) o un Canvas World Space con TMP_Text adentro, mas el script DamageNumberPopup. Si lo dejas vacio, simplemente no se muestran numeros.")]
    public GameObject damageNumberPrefab;

    private Vector3 camOriginalLocalPos;
    private Coroutine shakeRoutine;
    private Coroutine hitStopRoutine;

    void Awake()
    {
        Instance = this;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) camOriginalLocalPos = targetCamera.transform.localPosition;
    }

    public void OnSwingConnect(bool killedSomething)
    {
        StartHitStop(killedSomething ? hitStopDurationKill : hitStopDurationNormal);
        StartShake(killedSomething ? shakeIntensityKill : shakeIntensityNormal);
    }

    void StartHitStop(float duration)
    {
        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    void StartShake(float intensity)
    {
        if (targetCamera == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(intensity));
    }

    IEnumerator ShakeRoutine(float intensity)
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(t / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * intensity * damper;
            targetCamera.transform.localPosition = camOriginalLocalPos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }
        targetCamera.transform.localPosition = camOriginalLocalPos;
    }
    public void SpawnDamageNumber(Vector3 worldPos, string text, bool big)
    {
        if (damageNumberPrefab == null) return;

        GameObject go = Instantiate(damageNumberPrefab, worldPos, Quaternion.identity);
        DamageNumberPopup popup = go.GetComponent<DamageNumberPopup>();
        if (popup != null) popup.Setup(text, big);
    }
}