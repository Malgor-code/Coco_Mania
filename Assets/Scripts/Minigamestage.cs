using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(500)]
public class MinigameStage : MonoBehaviour
{
    public static MinigameStage Instance { get; private set; }

    [Header("Camara de minijuegos")]
    [Tooltip("Camara extra, con el componente Camera DESACTIVADO. Se enciende solo durante un minijuego 3D y la camara principal se apaga. " +
             "Si la dejas vacia, se mueve temporalmente la camara principal (puede pelearse con Cinemachine u otros scripts de seguimiento).")]
    public Camera minigameCamera;

    [Header("Gamefeel de camara")]
    [Tooltip("Cuanto se mueve la camara (en unidades de mundo) con un temblor al maximo. Ajustalo a la escala de tu escena.")]
    public float shakeUnits = 0.15f;
    [Range(0f, 1f)]
    [Tooltip("0 = sin temblor (accesibilidad).")]
    public float shakeStrength = 1f;
    [Tooltip("La camara entra empujando desde esta distancia (unidades) al iniciar un minijuego. 0 = sin entrada.")]
    public float introDollyDistance = 0.6f;

    [Header("Ayuda en el editor")]
    [Tooltip("Dibuja en la escena todos los empties, camaras y recorridos de los minijuegos.")]
    public bool showGizmos = true;
    public float gizmoSize = 0.1f;

    [System.Serializable]
    public class BolosSetup
    {
        [Header("Prefabs")]
        public GameObject pinPrefab;
        public GameObject ballPrefab;

        [Header("Empties")]
        [Tooltip("Posicion y rotacion de la camara durante Bolos.")]
        public Transform cameraPoint;
        [Tooltip("De donde sale la bola.")]
        public Transform ballStart;
        [Tooltip("Hacia donde rueda la bola con el angulo al extremo izquierdo de la barra (a la altura/profundidad de los pinos).")]
        public Transform aimLeft;
        [Tooltip("Idem con el angulo al extremo derecho de la barra.")]
        public Transform aimRight;
        [Tooltip("Padre con un hijo por pino (hasta 10). Ahi aparece cada pino.")]
        public Transform pinsRoot;
        [Tooltip("Opcional: hacia donde sigue rodando la bola despues de pegar. Si esta vacio, la bola sigue de largo y se encoge.")]
        public Transform pit;

        public bool IsReady =>
            pinPrefab != null && ballPrefab != null && cameraPoint != null && ballStart != null &&
            aimLeft != null && aimRight != null && pinsRoot != null && pinsRoot.childCount > 0;
    }

    [Header("Bolos de Coco")]
    public BolosSetup bolos;

    [System.Serializable]
    public class TopoSetup
    {
        [Header("Prefabs")]
        [Tooltip("Coco normal.")]
        public GameObject cocoPrefab;
        [Tooltip("Opcional: coco dorado (x3). Si esta vacio se usa el coco normal tenido de dorado.")]
        public GameObject cocoGoldPrefab;
        [Tooltip("Opcional: coco podrido (-2). Si esta vacio se usa el coco normal tenido de morado.")]
        public GameObject cocoRottenPrefab;
        [Tooltip("Opcional: martillo que sigue al mouse y golpea al hacer clic.")]
        public GameObject hammerPrefab;

        [Header("Empties")]
        [Tooltip("Posicion y rotacion de la camara durante Topo-Coco.")]
        public Transform cameraPoint;
        [Tooltip("Padre con un hijo por hoyo. El eje Y (verde) de cada hijo apunta hacia donde asoma el coco. Los hoyos deben estar sobre un plano horizontal.")]
        public Transform holesRoot;

        [Header("Opciones")]
        [Tooltip("Activo: el coco sube desde el hoyo. Desactivado: aparece creciendo en su sitio.")]
        public bool riseFromHole = true;
        [Tooltip("Activo: la BASE del coco queda apoyada en el empty del hoyo, sin importar donde este el pivote del prefab. " +
                 "Desactivado: el pivote del prefab queda en el empty (comportamiento anterior).")]
        public bool alignBaseToHole = true;

        public bool IsReady =>
            cocoPrefab != null && cameraPoint != null && holesRoot != null && holesRoot.childCount > 0;
    }

    [Header("Topo-Coco")]
    public TopoSetup topo;

    [System.Serializable]
    public class AtrapaSetup
    {
        [Header("Prefabs")]
        public GameObject basketPrefab;
        [Tooltip("Coco normal.")]
        public GameObject cocoPrefab;
        [Tooltip("Opcional: coco dorado (x3). Si esta vacio se usa el coco normal tenido de dorado.")]
        public GameObject cocoGoldPrefab;
        [Tooltip("Opcional: piedra (-2). Si esta vacio se usa el coco normal tenido de gris.")]
        public GameObject stonePrefab;

        [Header("Empties")]
        [Tooltip("Posicion y rotacion de la camara durante Lluvia de Cocos.")]
        public Transform cameraPoint;
        [Tooltip("Extremo izquierdo del recorrido de la canasta (a la altura donde se atrapa).")]
        public Transform basketLeft;
        [Tooltip("Extremo derecho del recorrido de la canasta.")]
        public Transform basketRight;
        [Tooltip("Extremo izquierdo de la linea de donde caen los objetos (arriba, en el mismo plano vertical que el recorrido).")]
        public Transform spawnLeft;
        [Tooltip("Extremo derecho de la linea de donde caen los objetos.")]
        public Transform spawnRight;

        public bool IsReady =>
            basketPrefab != null && cocoPrefab != null && cameraPoint != null &&
            basketLeft != null && basketRight != null && spawnLeft != null && spawnRight != null;
    }

    [Header("Lluvia de Cocos")]
    public AtrapaSetup atrapa;

    [System.Serializable]
    public class MachetazoSetup
    {
        [Header("Prefabs")]
        [Tooltip("El coco que se corta.")]
        public GameObject cocoPrefab;
        [Tooltip("Opcional: machete que da el tajo al hacer clic.")]
        public GameObject machetePrefab;

        [Header("Empties")]
        [Tooltip("Posicion y rotacion de la camara durante Machetazo.")]
        public Transform cameraPoint;
        [Tooltip("Padre con un hijo por posicion posible del coco. Con dificultad baja salen las mas cercanas al centro del grupo; con dificultad alta, todas.")]
        public Transform spotsRoot;

        public bool IsReady =>
            cocoPrefab != null && cameraPoint != null && spotsRoot != null && spotsRoot.childCount > 0;
    }

    [Header("Machetazo Perfecto")]
    public MachetazoSetup machetazo;

    public Camera ActiveCamera { get; private set; }

    bool active;
    bool usingExtraCamera;
    Camera prevMain;
    bool prevMainWasEnabled;
    Camera movedMain;
    Vector3 savedPos;
    Quaternion savedRot;
    Transform camTr;
    Vector3 pointPos;
    Quaternion pointRot;
    float shakeAmp, shakeTime, shakeDur;
    float introT = 1f;

    void Awake()
    {
        Instance = this;
        if (minigameCamera != null)
        {
            minigameCamera.enabled = false;
            var al = minigameCamera.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
        }
    }

    void OnDestroy()
    {
        ReleaseCamera();
        if (Instance == this) Instance = null;
    }

    public void UseCamera(Transform point)
    {
        if (point == null) return;
        ReleaseCamera();

        if (minigameCamera != null)
        {
            prevMain = Camera.main;
            if (prevMain == minigameCamera) prevMain = null;
            if (prevMain != null)
            {
                prevMainWasEnabled = prevMain.enabled;
                prevMain.enabled = false;
            }
            minigameCamera.transform.SetPositionAndRotation(point.position, point.rotation);
            minigameCamera.enabled = true;
            usingExtraCamera = true;
            ActiveCamera = minigameCamera;
        }
        else
        {
            movedMain = Camera.main;
            if (movedMain == null)
            {
                Debug.LogWarning("MinigameStage: no hay minigameCamera ni Camera.main.");
                return;
            }
            savedPos = movedMain.transform.position;
            savedRot = movedMain.transform.rotation;
            movedMain.transform.SetPositionAndRotation(point.position, point.rotation);
            usingExtraCamera = false;
            ActiveCamera = movedMain;
        }

        camTr = ActiveCamera.transform;
        pointPos = point.position;
        pointRot = point.rotation;
        introT = introDollyDistance > 0f ? 0f : 1f;
        shakeTime = 0f;
        active = true;
    }

    public void ReleaseCamera()
    {
        if (!active) return;
        active = false;

        if (usingExtraCamera)
        {
            if (minigameCamera != null) minigameCamera.enabled = false;
            if (prevMain != null) prevMain.enabled = prevMainWasEnabled;
        }
        else if (movedMain != null)
        {
            movedMain.transform.SetPositionAndRotation(savedPos, savedRot);
        }
        prevMain = null;
        movedMain = null;
        camTr = null;
        ActiveCamera = null;
        shakeTime = 0f;
    }

    public void Shake(float amount, float duration = 0.25f)
    {
        if (!active) return;
        amount = Mathf.Clamp01(amount);
        float left = shakeTime > 0f ? shakeAmp * Mathf.Clamp01(shakeTime / shakeDur) : 0f;
        if (amount < left) return;
        shakeAmp = amount;
        shakeDur = Mathf.Max(0.05f, duration);
        shakeTime = shakeDur;
    }

    void LateUpdate()
    {
        if (!active || camTr == null) return;
        float dt = Time.unscaledDeltaTime;
        Vector3 pos = pointPos;

        if (introT < 1f)
        {
            introT = Mathf.Min(1f, introT + dt / 0.6f);
            float u = 1f - introT;
            float e = 1f - u * u * u;
            pos -= pointRot * Vector3.forward * (introDollyDistance * (1f - e));
        }

        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            float k = Mathf.Clamp01(shakeTime / shakeDur);
            float a = shakeAmp * k * k * shakeUnits * shakeStrength;
            pos += pointRot * new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * a;
        }

        camTr.SetPositionAndRotation(pos, pointRot);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        float r = gizmoSize;

        // Bolos
        Color cb = new Color(1f, 0.6f, 0.2f);
        DrawCam(bolos.cameraPoint, cb, "Bolos: camara");
        Dot(bolos.ballStart, cb, r, "Bolos: bola");
        if (bolos.ballStart != null)
        {
            Gizmos.color = cb;
            if (bolos.aimLeft != null) { Gizmos.DrawLine(bolos.ballStart.position, bolos.aimLeft.position); Dot(bolos.aimLeft, cb, r * 0.7f, "aim L"); }
            if (bolos.aimRight != null) { Gizmos.DrawLine(bolos.ballStart.position, bolos.aimRight.position); Dot(bolos.aimRight, cb, r * 0.7f, "aim R"); }
        }
        if (bolos.pinsRoot != null)
            for (int i = 0; i < bolos.pinsRoot.childCount && i < 10; i++)
                Dot(bolos.pinsRoot.GetChild(i), cb, r * 0.8f, (i + 1).ToString());
        Dot(bolos.pit, cb, r, "Bolos: fosa");

        // Topo
        Color ct = new Color(0.4f, 0.9f, 0.4f);
        DrawCam(topo.cameraPoint, ct, "Topo: camara");
        if (topo.holesRoot != null)
            for (int i = 0; i < topo.holesRoot.childCount; i++)
            {
                Transform h = topo.holesRoot.GetChild(i);
                Dot(h, ct, r, null);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(h.position, h.position + h.up * r * 4f);
            }

        // Atrapa
        Color ca = new Color(0.4f, 0.8f, 1f);
        DrawCam(atrapa.cameraPoint, ca, "Atrapa: camara");
        Gizmos.color = ca;
        if (atrapa.basketLeft != null && atrapa.basketRight != null)
        {
            Gizmos.DrawLine(atrapa.basketLeft.position, atrapa.basketRight.position);
            Dot(atrapa.basketLeft, ca, r, "canasta L");
            Dot(atrapa.basketRight, ca, r, "canasta R");
        }
        if (atrapa.spawnLeft != null && atrapa.spawnRight != null)
        {
            Gizmos.DrawLine(atrapa.spawnLeft.position, atrapa.spawnRight.position);
            Dot(atrapa.spawnLeft, ca, r, "caida L");
            Dot(atrapa.spawnRight, ca, r, "caida R");
            if (atrapa.basketLeft != null) Gizmos.DrawLine(atrapa.spawnLeft.position, atrapa.basketLeft.position);
            if (atrapa.basketRight != null) Gizmos.DrawLine(atrapa.spawnRight.position, atrapa.basketRight.position);
        }

        // Machetazo
        Color cm = new Color(1f, 0.4f, 0.8f);
        DrawCam(machetazo.cameraPoint, cm, "Machetazo: camara");
        if (machetazo.spotsRoot != null)
            for (int i = 0; i < machetazo.spotsRoot.childCount; i++)
                Dot(machetazo.spotsRoot.GetChild(i), cm, r, null);
    }

    void DrawCam(Transform t, Color c, string label)
    {
        if (t == null) return;
        Gizmos.color = c;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
        float fov = minigameCamera != null ? minigameCamera.fieldOfView : 50f;
        Gizmos.DrawFrustum(Vector3.zero, fov, gizmoSize * 25f, 0.01f, 16f / 9f);
        Gizmos.matrix = old;
        Label(t.position, label);
    }

    void Dot(Transform t, Color c, float radius, string label)
    {
        if (t == null) return;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(t.position, radius);
        if (!string.IsNullOrEmpty(label)) Label(t.position + Vector3.up * radius * 1.5f, label);
    }

    static void Label(Vector3 pos, string text)
    {
#if UNITY_EDITOR
        Handles.Label(pos, text);
#endif
    }
}