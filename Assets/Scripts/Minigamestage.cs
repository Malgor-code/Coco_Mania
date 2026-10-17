using UnityEngine;

public class MinigameStage : MonoBehaviour
{
    public static MinigameStage Instance { get; private set; }

    [Header("Camara de minijuegos")]
    [Tooltip("Camara extra, con el componente Camera DESACTIVADO. Se enciende solo durante un minijuego 3D y la camara principal se apaga. " +
             "Si la dejas vacia, se mueve temporalmente la camara principal (puede pelearse con scripts de seguimiento).")]
    public Camera minigameCamera;

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
        [Tooltip("Opcional: hacia donde sigue rodando la bola despues de pegar. Si esta vacio, la bola desaparece al pegar.")]
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
        [Tooltip("Opcional: coco dorado (x3). Si esta vacio se usa el coco normal teñido de dorado.")]
        public GameObject cocoGoldPrefab;
        [Tooltip("Opcional: coco podrido (-2). Si esta vacio se usa el coco normal teñido de morado.")]
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
        [Tooltip("Opcional: coco dorado (x3). Si esta vacio se usa el coco normal teñido de dorado.")]
        public GameObject cocoGoldPrefab;
        [Tooltip("Opcional: piedra (-2). Si esta vacio se usa el coco normal teñido de gris.")]
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
        ActiveCamera = null;
    }
}