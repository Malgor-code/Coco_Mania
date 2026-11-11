using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

public class CoconutGallery3D : MonoBehaviour
{
    [Header("Abrir / cerrar")]
    [Tooltip("Arrastra aquí el PANEL (dentro de tu Canvas) que debe seguir visible mientras la galería está abierta, " +
             "normalmente el que contiene tu botón de la galería. Todos los demás paneles de ese Canvas se desactivan. " +
             "Conecta tu botón a CoconutGallery3D > ToggleGallery().")]
    public GameObject keepActivePanel;
    [Tooltip("Otros paneles/botones que NO deben apagarse al abrir la galería (ej. el panel donde están tus botones para cambiar de panel o salir). " +
             "Los botones de PanelTabBar se protegen solos.")]
    public GameObject[] alsoKeepActive;

    [Header("Panel de información (el tuyo)")]
    [Tooltip("Arrastra aquí tu panel que tiene Titulo, Descripcion y Price. Los textos se buscan solos por nombre. " +
             "Aparece cuando el coco queda enfrente y se oculta mientras la fila se mueve.")]
    public GameObject infoPanel;
    [Header("Opcional: solo si no se detectan solos por nombre")]
    public GameObject titleObject;
    public GameObject descriptionObject;
    public GameObject priceObject;

    [Header("Indicaciones (tu Text)")]
    [Tooltip("Arrastra aquí tu Text (normal o TextMeshPro) donde quieres que se muestren las indicaciones. " +
             "Se muestra al abrir la galería y se restaura al cerrarla.")]
    public GameObject hintObject;
    [TextArea]
    public string hintMessage = "A / D  o  \u2190 / \u2192  para moverte";

    [Header("Escena 3D (posicion fija en el mapa)")]
    [Tooltip("Posicion MUNDIAL de la galeria (el primer slot queda aqui). Copia aqui el valor que te guste de Transform > Position.")]
    public Vector3 galleryWorldPosition = new Vector3(0.0453186f, 13.7f, -9.7f);
    [Tooltip("Rotacion MUNDIAL de la galeria (la fila se desliza hacia el -X local de esta rotacion).")]
    public Vector3 galleryWorldEuler = new Vector3(62.2f, 0f, 0f);
    [Tooltip("Desplazamiento local del carrusel (normalmente 0,0,0).")]
    public Vector3 carouselStartPosition = Vector3.zero;
    [Tooltip("Offset local de cada slot respecto al carrusel (normalmente 0,0,0).")]
    public Vector3 slotOffset = Vector3.zero;
    [Tooltip("Tamaño (dimensión mayor) al que se escala cada coco.")]
    public float displaySize = 1.6f;
    public float spacing = 3f;
    public float slideSmoothTime = 0.25f;
    public float spinSpeed = 25f;
    [Tooltip("Escala de los cocos que NO estan enfrente (1 = igual que el de enfrente, 0.5 = la mitad).")]
    [Range(0.1f, 1f)] public float sideScale = 0.55f;
    [Tooltip("Opcional: material para los cocos bloqueados. Si está vacío se tiñen de negro automáticamente.")]
    public Material silhouetteMaterial;

    const string PrefKey = "CoconutGallery_MaxCycle";

    class GalleryItem
    {
        public Transform slot;
        public Transform spinner;
        public Renderer[] renderers;
        public Material[][] original;
        public Material[][] dark;
    }
    bool isOpen;
    int index;
    float slideVel;
    int shownIndex = -1;
    int maxCycleReached = 1;
    GameObject galleryRoot;
    Transform carousel;
    readonly List<GalleryItem> items = new List<GalleryItem>();
    bool infoPanelWasActive;
    bool hintWasActive;
    bool infoResolved;
    readonly List<GameObject> disabledObjects = new List<GameObject>();
    readonly List<Transform> keepList = new List<Transform>();

    void Awake()
    {
        maxCycleReached = PlayerPrefs.GetInt(PrefKey, 1);
    }

    void Update()
    {
        TrackMaxCycle();

        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || GameInput.NavLeftDown) Move(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || GameInput.NavRightDown) Move(1);
        float targetX = carouselStartPosition.x - index * spacing;
        float x = Mathf.SmoothDamp(carousel.localPosition.x, targetX, ref slideVel,
                                   slideSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        carousel.localPosition = new Vector3(x, carouselStartPosition.y, carouselStartPosition.z);
        for (int i = 0; i < items.Count; i++)
            items[i].spinner.Rotate(0f, spinSpeed * Time.unscaledDeltaTime, 0f);
        for (int i = 0; i < items.Count; i++)
        {
            float d = Mathf.Abs(i * spacing + (x - carouselStartPosition.x)) / Mathf.Max(0.01f, spacing);
            items[i].spinner.localScale = Vector3.one * Mathf.Lerp(1f, sideScale, Mathf.Clamp01(d));
        }
        bool settled = Mathf.Abs(x - targetX) < 0.02f;
        if (infoPanel != null) infoPanel.SetActive(settled);
        if (settled && shownIndex != index) UpdateInfo();
    }

    public void OpenGallery()
    {
        if (isOpen) return;
        if (CoconutSpawner.Instance == null)
        {
            Debug.LogWarning("CoconutGallery3D: no hay CoconutSpawner en la escena.");
            return;
        }

        if (galleryRoot == null) BuildScene();

        isOpen = true;
        index = 0;
        slideVel = 0f;
        carousel.localPosition = carouselStartPosition;
        shownIndex = -1;
        RefreshLockState();

        ResolveInfoObjects();
        if (infoPanel != null)
        {
            infoPanelWasActive = infoPanel.activeSelf;
            infoPanel.SetActive(false);
        }

        if (hintObject != null)
        {
            hintWasActive = hintObject.activeSelf;
            SetText(hintObject, hintMessage);
            hintObject.SetActive(true);
        }

        DisableOtherUI();

        ApplyRootTransform();
        galleryRoot.SetActive(true);
    }

    public void ToggleGallery()
    {
        if (isOpen) CloseGallery(); else OpenGallery();
    }

    void LateUpdate()
    {
        if (!isOpen) return;

        ApplyRootTransform();

        foreach (var k in keepList)
        {
            if (k == null) continue;
            Transform t = k;
            while (t != null)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                Canvas cv = t.GetComponent<Canvas>();
                if (cv != null && !cv.enabled) cv.enabled = true;
                t = t.parent;
            }
        }
    }

    void ApplyRootTransform()
    {
        if (galleryRoot == null) return;
        galleryRoot.transform.SetPositionAndRotation(galleryWorldPosition, Quaternion.Euler(galleryWorldEuler));
    }

    void DisableOtherUI()
    {
        disabledObjects.Clear();

        BuildKeepList();

        var keepRoots = new HashSet<Canvas>();
        foreach (var k in keepList)
        {
            Canvas c = k.GetComponentInParent<Canvas>();
            if (c != null) keepRoots.Add(c.rootCanvas);
        }

        if (keepActivePanel != null && !keepRoots.Contains(keepActivePanel.GetComponentInParent<Canvas>() != null ? keepActivePanel.GetComponentInParent<Canvas>().rootCanvas : null))
            Debug.LogWarning("CoconutGallery3D: 'Keep Active Panel' no está dentro de un Canvas.");

        foreach (var k in keepList)
        {
            Canvas c = k.GetComponentInParent<Canvas>();
            if (c == null) continue;

            Transform stop = c.rootCanvas.transform;
            Transform node = k;
            while (node != null && node != stop)
            {
                Transform parent = node.parent;
                if (parent == null) break;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform sibling = parent.GetChild(i);
                    if (sibling == node) continue;
                    if (IsProtected(sibling)) continue;
                    if (!sibling.gameObject.activeSelf) continue;
                    sibling.gameObject.SetActive(false);
                    disabledObjects.Add(sibling.gameObject);
                }
                node = parent;
            }
        }
    }

    void BuildKeepList()
    {
        keepList.Clear();
        AddKeep(keepActivePanel);
        if (alsoKeepActive != null)
            foreach (var g in alsoKeepActive) AddKeep(g);

        var bar = FindObjectOfType<PanelTabBar>();
        if (bar != null)
        {
            if (bar.barContainer != null) AddKeep(bar.barContainer.gameObject);
            if (bar.tabs != null)
                foreach (var t in bar.tabs)
                    if (t != null && t.button != null) AddKeep(t.button.gameObject);
        }
    }

    void AddKeep(GameObject g)
    {
        if (g != null && !keepList.Contains(g.transform)) keepList.Add(g.transform);
    }

    bool IsProtected(Transform t)
    {
        if (t == transform || transform.IsChildOf(t)) return true;
        if (infoPanel != null && infoPanel.transform.IsChildOf(t)) return true;
        if (hintObject != null && hintObject.transform.IsChildOf(t)) return true;
        foreach (var k in keepList)
            if (k != null && k.IsChildOf(t)) return true;
        return false;
    }

    void RestoreUI()
    {
        foreach (var go in disabledObjects)
            if (go != null) go.SetActive(true);
        disabledObjects.Clear();
    }

    public void CloseGallery()
    {
        if (!isOpen) return;
        isOpen = false;

        galleryRoot.SetActive(false);

        RestoreUI();
        if (infoPanel != null) infoPanel.SetActive(infoPanelWasActive);
        if (hintObject != null) hintObject.SetActive(hintWasActive);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshAllUI();
            StartCoroutine(RefreshUINextFrame());
        }
    }

    void OnDestroy()
    {
        if (galleryRoot != null) Destroy(galleryRoot);
    }

    public void Move(int dir)
    {
        int next = Mathf.Clamp(index + dir, 0, items.Count - 1);
        if (next == index) return;
        index = next;
    }
    public void MoveLeft() { Move(-1); }
    public void MoveRight() { Move(1); }

    void TrackMaxCycle()
    {
        if (GameManager.Instance == null) return;
        int cycle = GameManager.Instance.billCycle;
        if (cycle > maxCycleReached)
        {
            maxCycleReached = cycle;
            PlayerPrefs.SetInt(PrefKey, maxCycleReached);
            if (isOpen) { RefreshLockState(); shownIndex = -1; }
        }
    }

    int GetRequiredCycle(int i)
    {
        var gm = GameManager.Instance;
        bool hasThreshold = gm != null && i < gm.coconutUnlocks.Count;
        return hasThreshold ? gm.coconutUnlocks[i].cycleRequired : (i == 0 ? 1 : int.MaxValue);
    }

    bool IsUnlocked(int i)
    {
        return maxCycleReached >= GetRequiredCycle(i);
    }

    void RefreshLockState()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            bool unlocked = IsUnlocked(i);
            for (int r = 0; r < it.renderers.Length; r++)
            {
                if (it.renderers[r] == null) continue;
                it.renderers[r].sharedMaterials = unlocked ? it.original[r] : it.dark[r];
            }
        }
    }

    void UpdateInfo()
    {
        shownIndex = index;
        if (infoPanel == null) return;

        CoconutTypeData type = CoconutSpawner.Instance.coconutTypes[index];
        bool unlocked = IsUnlocked(index);

        SetText(titleObject, unlocked ? type.typeName : "???");
        if (descriptionObject != null) descriptionObject.SetActive(unlocked);
        if (priceObject != null) priceObject.SetActive(unlocked);

        if (unlocked)
        {
            SetText(descriptionObject, GetDescriptionText(type));
            SetText(priceObject, GetPriceText(type));
        }
    }

    string GetDescriptionText(CoconutTypeData type)
    {
        return "Vida: x" + type.hpMultiplier.ToString("0.##") +
               "\nAparece desde el ciclo " + GetRequiredCycle(index);
    }

    string GetPriceText(CoconutTypeData type)
    {
        return "Botín: x" + type.lootMultiplier.ToString("0.##");
    }

    void ResolveInfoObjects()
    {
        if (infoResolved || infoPanel == null) return;
        infoResolved = true;

        if (titleObject == null) titleObject = FindChildByNames(infoPanel.transform, "titulo", "title", "nombre", "name");
        if (descriptionObject == null) descriptionObject = FindChildByNames(infoPanel.transform, "descripcion", "description", "desc");
        if (priceObject == null) priceObject = FindChildByNames(infoPanel.transform, "price", "precio");

        if (titleObject == null) Debug.LogWarning("CoconutGallery3D: no encontré 'Titulo' dentro del panel. Arrástralo en 'Title Object'.");
        if (descriptionObject == null) Debug.LogWarning("CoconutGallery3D: no encontré 'Descripcion' dentro del panel. Arrástralo en 'Description Object'.");
        if (priceObject == null) Debug.LogWarning("CoconutGallery3D: no encontré 'Price' dentro del panel. Arrástralo en 'Price Object'.");
    }

    static GameObject FindChildByNames(Transform root, params string[] names)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (t == root) continue;
            string n = Normalize(t.name);
            foreach (var k in names) if (n == k) return t.gameObject;
        }
        foreach (var t in all)
        {
            if (t == root) continue;
            string n = Normalize(t.name);
            foreach (var k in names) if (n.Contains(k)) return t.gameObject;
        }
        return null;
    }

    static string Normalize(string s)
    {
        string d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().ToLowerInvariant();
    }

    static void SetText(GameObject go, string value)
    {
        if (go == null) return;
        foreach (var c in go.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;
            if (c is Text legacy) { legacy.text = value; return; }
            var prop = c.GetType().GetProperty("text");
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
            {
                prop.SetValue(c, value, null);
                return;
            }
        }
    }

    void BuildScene()
    {
        var spawner = CoconutSpawner.Instance;
        int count = spawner.coconutTypes.Count;
        galleryRoot = new GameObject("CoconutGalleryScene");
        galleryRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        carousel = new GameObject("Carrusel").transform;
        carousel.SetParent(galleryRoot.transform, false);
        carousel.localPosition = carouselStartPosition;
        var holder = new GameObject("TempHolder");
        holder.SetActive(false);

        for (int i = 0; i < count; i++)
        {
            var slot = new GameObject("Slot_" + i).transform;
            slot.SetParent(carousel, false);
            slot.localPosition = new Vector3(i * spacing, 0f, 0f) + slotOffset;

            var spinner = new GameObject("Giro").transform;
            spinner.SetParent(slot, false);

            GameObject model = CreateModel(spawner, i, holder.transform);
            StripGameplayComponents(model);
            model.transform.SetParent(spinner, false);
            FitModel(model.transform, spinner, slot);

            var item = new GalleryItem { slot = slot, spinner = spinner };
            item.renderers = model.GetComponentsInChildren<Renderer>(true);
            item.original = new Material[item.renderers.Length][];
            item.dark = new Material[item.renderers.Length][];
            for (int r = 0; r < item.renderers.Length; r++)
            {
                Material[] orig = item.renderers[r].sharedMaterials;
                item.original[r] = orig;
                Material[] dk = new Material[orig.Length];
                for (int m = 0; m < orig.Length; m++)
                    dk[m] = orig[m] != null ? MakeDark(orig[m]) : null;
                item.dark[r] = dk;
            }
            items.Add(item);
        }

        Destroy(holder);
        galleryRoot.SetActive(false);
    }

    GameObject CreateModel(CoconutSpawner spawner, int i, Transform parent)
    {
        CoconutTypeData type = spawner.coconutTypes[i];
        GameObject prefab = (type != null && type.prefab != null) ? type.prefab : spawner.coconutPrefab;

        if (prefab != null) return Instantiate(prefab, parent);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(parent, false);
        return sphere;
    }

    void StripGameplayComponents(GameObject go)
    {
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) DestroyImmediate(mb);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            if (rb != null) DestroyImmediate(rb);
        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            if (c != null) DestroyImmediate(c);
    }

    void FitModel(Transform model, Transform spinner, Transform slot)
    {
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;

        Bounds b = GetBounds(model.gameObject);
        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (maxDim > 0.0001f) model.localScale *= displaySize / maxDim;

        b = GetBounds(model.gameObject);
        Vector3 delta = new Vector3(
            spinner.position.x - b.center.x,
            slot.position.y - b.min.y,
            spinner.position.z - b.center.z);
        model.position += delta;
    }

    static Bounds GetBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.01f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    Material MakeDark(Material src)
    {
        if (silhouetteMaterial != null) return silhouetteMaterial;

        var m = new Material(src);
        if (m.HasProperty("_Color")) m.SetColor("_Color", Color.black);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.black);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        m.DisableKeyword("_EMISSION");
        return m;
    }

    System.Collections.IEnumerator RefreshUINextFrame()
    {
        yield return null;
        if (GameManager.Instance != null) GameManager.Instance.RefreshAllUI();
    }
}