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

    [Header("Camera Shake (PASIVO: trauma acumulado, no un golpe de camara)")]
    [Tooltip("Trauma que agrega un golpe normal. Bajo a proposito - casi no deberia notarse en un golpe suelto.")]
    [Range(0f, 1f)] public float traumaPerHit = 0.05f;
    [Tooltip("Trauma que agrega un golpe que mata. Un poco mas, pero sigue siendo sutil.")]
    [Range(0f, 1f)] public float traumaPerKill = 0.12f;
    [Tooltip("Trauma extra cuando el golpe suelta una moneda (un 'ding' sutil).")]
    [Range(0f, 1f)] public float traumaPerCoin = 0.03f;
    [Tooltip("Que tan rapido se disipa el trauma por segundo (mas alto = shake mas corto).")]
    public float traumaDecay = 1.8f;
    [Tooltip("Desplazamiento MAXIMO de camara si el trauma llegara a 1.0 (casi nunca pasa).")]
    public float maxShakeOffset = 0.05f;
    [Tooltip("Curva de respuesta: 2 = el trauma bajo se nota MUCHO menos que el alto (recomendado). 1 = respuesta lineal (mas fuerte a simple vista).")]
    public float traumaExponent = 2f;

    private float trauma = 0f;
    private Vector3 camOriginalLocalPos;

    [Header("Numeros flotantes (opcional)")]
    [Tooltip("Prefab con un TextMeshPro (3D, world space) o un Canvas World Space con TMP_Text adentro, mas el script DamageNumberPopup. Si lo dejas vacio, simplemente no se muestran numeros.")]
    public GameObject damageNumberPrefab;

    [Header("Reporte de golpe (textos que salen uno por uno)")]
    [Tooltip("Segundos entre que sale un texto y el siguiente (dano -> agua -> moneda).")]
    public float popupStagger = 0.5f;
    [Tooltip("Separacion vertical entre los textos de un mismo golpe.")]
    public float popupLineSpacing = 0.35f;
    [Tooltip("Variacion horizontal aleatoria del punto de aparicion.")]
    public float popupJitterX = 0.1f;

    private Coroutine hitStopRoutine;

    void Awake()
    {
        Instance = this;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) camOriginalLocalPos = targetCamera.transform.localPosition;
    }
    public void SpawnKillRewards(Vector3 worldPos, float waterML, bool gaveCoin)
    {
        if (damageNumberPrefab == null) return;

        float delay = popupStagger;
        int line = 1;

        if (waterML > 0.0001f)
        {
            SpawnPopup(worldPos, FormatWater(waterML), PopupStyle.Water, false, line++, delay);
            delay += popupStagger;
        }

        if (gaveCoin)
        {
            SpawnPopup(worldPos, "+1 Moneda", PopupStyle.Coin, false, line++, delay);
            trauma = Mathf.Clamp01(trauma + traumaPerCoin);
        }
    }
    void Update()
    {
        if (targetCamera == null) return;

        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
        }

        float amount = Mathf.Pow(trauma, traumaExponent);

        if (amount > 0.0001f)
        {
            float offsetX = (Mathf.PerlinNoise(Time.unscaledTime * 20f, 0.37f) - 0.5f) * 2f;
            float offsetY = (Mathf.PerlinNoise(0.91f, Time.unscaledTime * 20f) - 0.5f) * 2f;
            Vector3 shakeOffset = new Vector3(offsetX, offsetY, 0f) * maxShakeOffset * amount;
            targetCamera.transform.localPosition = camOriginalLocalPos + shakeOffset;
        }
        else
        {
            targetCamera.transform.localPosition = camOriginalLocalPos;
        }
    }

    public void OnSwingConnect(bool killedSomething)
    {
        StartHitStop(killedSomething ? hitStopDurationKill : hitStopDurationNormal);
        trauma = Mathf.Clamp01(trauma + (killedSomething ? traumaPerKill : traumaPerHit));
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

    public void SpawnDamageNumber(Vector3 worldPos, string text, bool big)
    {
        SpawnPopup(worldPos, text, PopupStyle.Damage, big, 0, 0f);
    }

    public void SpawnHitReport(Vector3 worldPos, float damage, float waterML, bool gaveCoin, bool crit)
    {
        if (damageNumberPrefab == null) return;

        int line = 0;
        float delay = 0f;

        SpawnPopup(worldPos, damage.ToString("0.#"), PopupStyle.Damage, crit, line++, delay);

        if (waterML > 0.0001f)
        {
            delay += popupStagger;
            SpawnPopup(worldPos, FormatWater(waterML), PopupStyle.Water, false, line++, delay);
        }

        if (gaveCoin)
        {
            delay += popupStagger;
            SpawnPopup(worldPos, "+1 Moneda", PopupStyle.Coin, false, line++, delay);
            trauma = Mathf.Clamp01(trauma + traumaPerCoin);
        }
    }

    static string FormatWater(float ml)
    {
        if (ml >= 1000f) return "+" + (ml / 1000f).ToString("0.##") + " L";
        return "+" + ml.ToString("0") + " mL";
    }

    void SpawnPopup(Vector3 worldPos, string text, PopupStyle style, bool big, int line, float delay)
    {
        if (damageNumberPrefab == null) return;

        Vector3 pos = worldPos
                      + Vector3.up * (line * popupLineSpacing)
                      + Vector3.right * Random.Range(-popupJitterX, popupJitterX);

        GameObject go = Instantiate(damageNumberPrefab, pos, Quaternion.identity);
        DamageNumberPopup popup = go.GetComponent<DamageNumberPopup>();
        if (popup != null) popup.Setup(text, style, big, delay);
    }
}