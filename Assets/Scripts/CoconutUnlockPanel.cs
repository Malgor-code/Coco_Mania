using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CoconutUnlockPanel : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    static int closedFrame = -1;
    public static bool BlocksPause => IsOpen || closedFrame == Time.frameCount;

    [Header("Panel UI")]
    [Tooltip("Raiz del panel (fondo oscuro a pantalla completa con Raycast Target activado). Debe empezar APAGADA.")]
    public GameObject panelRoot;
    [Tooltip("Opcional: para el fade-in.")]
    public CanvasGroup canvasGroup;
    [Tooltip("Opcional: la tarjeta central, para el efecto de 'pop'.")]
    public RectTransform card;

    [Header("Textos")]
    public TMP_Text titleText;
    public TMP_Text nameText;
    public TMP_Text abilityNameText;
    public TMP_Text abilityDescText;
    public TMP_Text waterText;
    public TMP_Text phraseText;
    public TMP_Text hintText;

    [Header("Vista previa 3D")]
    [Tooltip("RawImage donde se muestra el coco girando.")]
    public RawImage previewImage;
    [Tooltip("Capa libre que usa la camara de vista previa. Si la 31 ya la usas, cambia el numero.")]
    public int previewLayer = 31;
    public int previewResolution = 512;
    [Tooltip("Tamano al que se escala el modelo dentro de la vista previa.")]
    public float fitSize = 1.6f;
    public float spinSpeed = 60f;
    [Tooltip("Lugar lejano donde se arma el escenario de la vista previa (lejos de tu escena).")]
    public Vector3 stagePosition = new Vector3(10000f, 10000f, 10000f);

    [Header("Tiempos")]
    public float popDuration = 0.3f;
    [Tooltip("Segundos minimos antes de aceptar 'cualquier tecla' (evita cerrarlo sin querer).")]
    public float minShowTime = 0.6f;
    static readonly string[] AbilityNames =
    {
        "Sin habilidad", "Generoso", "Resistente", "Fibra Dura", "Pesado",
        "Tenaz", "Armadura", "Rebote", "Fortificado", "Implacable",
        "Fragil y Valioso", "Regeneracion", "Camuflaje", "Maldicion", "Supremo"
    };

    static readonly string[] AbilityDescs =
    {
        "Un coco normal. Sin trucos.",
        "Suelta mas agua que los cocos comunes.",
        "Recibe 30% menos de daño de los golpes normales. Los criticos lo ignoran.",
        "Cada golpe seguido lo endurece (hasta -25% de daño). Si lo dejas respirar 1.5 s, se reinicia.",
        "Grande y pesado: casi no se mueve al ser golpeado.",
        "Con menos de 30% de vida se aferra y recibe 25% menos de daño.",
        "Recibe 50% menos de daño de los golpes normales. Los criticos lo ignoran.",
        "Cada 5 golpes que recibe, te quita 2 de resistencia.",
        "Cada 4 segundos se blinda durante 3 s y recibe 40% menos de daño.",
        "Si lo golpeas 4 veces en 2 s, se protege 2 s y recibe 30% menos de daño.",
        "Recibe 75% menos de daño normal, pero los criticos le pegan x4.",
        "Si pasa 2 s sin recibir golpes, recupera 3% de su vida por segundo.",
        "Se vuelve casi invisible durante 2 s de cada 5.",
        "Al romperlo lanza una maldicion temporal que te debilita unos segundos.",
        "Mini-jefe de 3 fases: empieza con 20% menos de daño, a 70% de vida se regenera y a 30% se endurece (-35% de daño)."
    };

    static readonly string[] Phrases =
    {
        "Joven, terco y siempre listo para su primer golpe.",
        "Ya probo el sol. Dicen que su agua sabe a gloria.",
        "Mas duro de roer que una suela vieja.",
        "Cada fibra cuenta una historia de machetazos.",
        "No es un coco, es una piedra con aires de grandeza.",
        "Ha visto mil dias de sol y ningun machete lo asusta.",
        "Su cascara ha desafiado mas filos que un herrero.",
        "Suena a campana cuando lo golpeas. No es buena señal.",
        "Se endurece justo cuando crees que ya ganaste.",
        "Nadie lo golpea dos veces sin que se ponga serio.",
        "Brilla de mas. Cuidado con presumirlo... o con pegarle mal.",
        "Los viejos jurabamos que ya lo habiamos cortado.",
        "Ahora lo ves, ahora no.",
        "Guarda rencores de generaciones de cocoteros.",
        "El rey del cocotal. No se inclina ante nadie."
    };

    private readonly Queue<int> queue = new Queue<int>();
    private bool showing = false;

    private Transform spinPivot;
    private Camera previewCam;
    private RenderTexture previewRT;
    private GameObject stageRoot;

    void Awake()
    {
        IsOpen = false;
        BuildStage();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (previewRT != null) { previewRT.Release(); Destroy(previewRT); }
        if (stageRoot != null) Destroy(stageRoot);
    }

    void BuildStage()
    {
        stageRoot = new GameObject("CoconutPreviewStage");
        stageRoot.transform.position = stagePosition;

        spinPivot = new GameObject("SpinPivot").transform;
        spinPivot.SetParent(stageRoot.transform, false);

        GameObject camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(stageRoot.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0.6f, -5f);
        camGo.transform.localRotation = Quaternion.Euler(7f, 0f, 0f);

        previewRT = new RenderTexture(previewResolution, previewResolution, 16, RenderTextureFormat.ARGB32);
        previewRT.antiAliasing = 4;

        previewCam = camGo.AddComponent<Camera>();
        previewCam.orthographic = true;
        previewCam.orthographicSize = fitSize * 0.7f;
        previewCam.nearClipPlane = 0.1f;
        previewCam.farClipPlane = 20f;
        previewCam.clearFlags = CameraClearFlags.SolidColor;
        previewCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCam.cullingMask = 1 << previewLayer;
        previewCam.targetTexture = previewRT;
        previewCam.enabled = false;

        if (previewImage != null) previewImage.texture = previewRT;
    }
   public void Enqueue(int tierIndex)
    {
        queue.Enqueue(tierIndex);
        if (!showing) StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        showing = true;
        IsOpen = true;
        if (previewCam != null) previewCam.enabled = true;

        while (queue.Count > 0)
        {
            int tier = queue.Dequeue();
            BuildContent(tier);

            if (panelRoot != null) panelRoot.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            float t = 0f;
            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;

                float p = popDuration > 0f ? Mathf.Clamp01(t / popDuration) : 1f;
                if (canvasGroup != null) canvasGroup.alpha = p;
                if (card != null) card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(p));

                if (spinPivot != null) spinPivot.Rotate(Vector3.up, spinSpeed * dt, Space.World);
                if (hintText != null)
                {
                    Color c = hintText.color;
                    c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f);
                    hintText.color = c;
                }

                if (t >= minShowTime && Input.anyKeyDown) break;
                yield return null;
            }

            ClearPreview();
        }

        if (panelRoot != null) panelRoot.SetActive(false);
        if (previewCam != null) previewCam.enabled = false;
        closedFrame = Time.frameCount;
        IsOpen = false;
        showing = false;
    }

    void BuildContent(int tier)
    {
        GameManager gm = GameManager.Instance;
        CoconutSpawner sp = CoconutSpawner.Instance;
        if (gm == null || sp == null) return;

        string coconutName = tier < gm.coconutUnlocks.Count ? gm.coconutUnlocks[tier].coconutName : "Coco";
        CoconutTypeData type = tier < sp.coconutTypes.Count ? sp.coconutTypes[tier] : null;
        float loot = type != null ? type.lootMultiplier : 1f;

        if (titleText != null) titleText.text = "¡NUEVO COCO DESBLOQUEADO!";
        if (nameText != null) nameText.text = coconutName;
        if (abilityNameText != null) abilityNameText.text = tier < AbilityNames.Length ? AbilityNames[tier] : "-";
        if (abilityDescText != null) abilityDescText.text = tier < AbilityDescs.Length ? AbilityDescs[tier] : "";
        if (phraseText != null) phraseText.text = tier < Phrases.Length ? "\"" + Phrases[tier] + "\"" : "";
        if (hintText != null) hintText.text = "Presiona cualquier tecla para continuar";

        if (waterText != null)
        {
            float min = gm.baseWaterPerKill * loot;
            float max = (gm.baseWaterPerKill + gm.waterVariance) * loot;
            float avg = (gm.baseWaterPerKill + gm.waterVariance * 0.5f) * loot;
            waterText.text = "Agua por coco: " + GameManager.FormatWater(min) + " - " + GameManager.FormatWater(max)
                           + "\nPromedio: " + GameManager.FormatWater(avg) + "  (x" + loot.ToString("0.##") + " de botin, sin bonos)";
        }

        GameObject prefab = (type != null && type.prefab != null) ? type.prefab : sp.coconutPrefab;
        BuildPreview(prefab);
    }

    void BuildPreview(GameObject prefab)
    {
        ClearPreview();
        if (prefab == null || spinPivot == null) return;
        GameObject holder = new GameObject("tmpHolder");
        holder.SetActive(false);
        GameObject go = Instantiate(prefab, holder.transform);

        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(mb);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);

        go.transform.SetParent(spinPivot, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        Destroy(holder);

        SetLayerRecursive(go, previewLayer);
        spinPivot.rotation = Quaternion.identity;
        FitToView(go);
    }

    void ClearPreview()
    {
        if (spinPivot == null) return;
        for (int i = spinPivot.childCount - 1; i >= 0; i--) Destroy(spinPivot.GetChild(i).gameObject);
    }

    void FitToView(GameObject go)
    {
        if (!TryGetBounds(go, out Bounds b)) return;
        float maxExtent = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxExtent > 0.0001f) go.transform.localScale *= fitSize / maxExtent;
        if (TryGetBounds(go, out b)) go.transform.position += spinPivot.position - b.center;
    }

    static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = new Bounds();
        bool has = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!has) { bounds = r.bounds; has = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return has;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}