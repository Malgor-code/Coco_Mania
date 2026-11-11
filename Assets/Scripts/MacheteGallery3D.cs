using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Globalization;

public class MacheteGallery3D : MonoBehaviour
{
    [Header("Abrir / cerrar")]
    [Tooltip("Panel (dentro de tu Canvas) que debe seguir visible mientras la galería está abierta, " +
             "normalmente el que contiene tu botón. Los demás paneles de ese Canvas se desactivan. " +
             "Conecta tu botón a MacheteGallery3D > ToggleGallery().")]
    public GameObject keepActivePanel;
    [Tooltip("Otros paneles/botones que NO deben apagarse al abrir la galería (ej. el panel donde están tus botones para cambiar de panel o salir). " +
             "Los botones de PanelTabBar se protegen solos.")]
    public GameObject[] alsoKeepActive;

    [Header("Panel de información (el tuyo)")]
    [Tooltip("Panel con Titulo, Descripcion y Price. Los textos se buscan solos por nombre. " +
             "Aparece cuando el machete queda enfrente y se oculta mientras la fila se mueve.")]
    public GameObject infoPanel;
    [Header("Opcional: solo si no se detectan solos por nombre")]
    public GameObject titleObject;
    public GameObject descriptionObject;
    public GameObject priceObject;

    [Header("Botón de compra")]
    [Tooltip("Tu Button de compra. Si está dentro del panel de información y se llama 'Comprar'/'Buy', se detecta solo. " +
             "Solo se muestra cuando el machete queda enfrente.")]
    public Button buyButton;
    [Tooltip("Nombre del método de GameManager que compra un machete y recibe su índice (int). " +
             "Si lo dejas vacío se prueban nombres comunes: BuyMachete, PurchaseMachete, ComprarMachete, UnlockMachete, TryBuyMachete...")]
    public string buyMethodName = "";
    [Tooltip("Opcional: nombre del método de GameManager (int -> bool) que dice si ya tienes ese machete. " +
             "Si lo dejas vacío se prueban: IsMacheteOwned, IsMacheteUnlocked, HasMachete, OwnsMachete...")]
    public string ownedMethodName = "";
    [Tooltip("Alternativa/extra: se dispara al pulsar comprar, con el índice del machete. " +
             "Úsalo si prefieres conectar tu propia función desde el Inspector.")]
    public UnityEvent<int> onBuyPressed;
    public string buyLabel = "Comprar";
    public string ownedLabel = "Comprado";

    [Header("Indicaciones (tu Text)")]
    public GameObject hintObject;
    [TextArea]
    public string hintMessage = "A / D  o  ← / →  para moverte";

    [Header("Apagar mientras la galería está activa")]
    [Tooltip("Arrastra aquí el/los Text (u objetos) que quieres apagar mientras la galería esté activa. Se restauran al cerrar.")]
    public GameObject[] hideWhileOpen;
    [Tooltip("Activo: se apagan mientras la galería está abierta. " +
             "Desactivado: se apagan SOLO mientras el panel de información está visible (cuando el machete queda quieto).")]
    public bool hideForWholeGallery = true;

    [Header("Modelos de los machetes")]
    [Tooltip("Prefab de cada machete, en el MISMO orden que GameManager.macheteOptions. " +
             "Si dejas uno vacío se usa el GameObject que tenga MacheteData (si existe) o un machete de cubos.")]
    public GameObject[] macheteModels;
    [Tooltip("Rotación aplicada a cada modelo (útil si tu machete viene acostado).")]
    public Vector3 modelEuler = Vector3.zero;

    [Header("Escena 3D (posicion fija en el mapa)")]
    [Tooltip("Posicion MUNDIAL de la galeria (el primer slot queda aqui). Copia aqui el valor que te guste de Transform > Position.")]
    public Vector3 galleryWorldPosition = new Vector3(0.0453186f, 13.7f, -9.7f);
    [Tooltip("Rotacion MUNDIAL de la galeria (la fila se desliza hacia el -X local de esta rotacion).")]
    public Vector3 galleryWorldEuler = new Vector3(62.2f, 0f, 0f);
    [Tooltip("Desplazamiento local del carrusel (normalmente 0,0,0).")]
    public Vector3 carouselStartPosition = Vector3.zero;
    [Tooltip("Offset local de cada slot respecto al carrusel (normalmente 0,0,0).")]
    public Vector3 slotOffset = Vector3.zero;
    [Tooltip("Tamaño (dimension mayor) al que se escala cada machete.")]
    public float displaySize = 2f;
    public float spacing = 3f;
    public float slideSmoothTime = 0.25f;
    public float spinSpeed = 25f;
    public Vector3[] extraModelEuler;
    [Header("Textos")]
    public string freePriceText = "Gratis";
    public string pricePrefix = "Precio: ";
    public string priceSuffix = " mL";

    static readonly string[] BuyCandidates =
    {
        "BuyMachete", "PurchaseMachete", "ComprarMachete", "TryBuyMachete", "TryPurchaseMachete",
        "UnlockMachete", "BuyMacheteByIndex", "BuyMacheteAt", "TryUnlockMachete"
    };
    static readonly string[] OwnedCandidates =
    {
        "IsMacheteOwned", "IsMacheteUnlocked", "HasMachete", "OwnsMachete", "IsMachetePurchased", "MacheteOwned", "MacheteUnlocked"
    };

    class GalleryItem
    {
        public Transform slot;
        public Transform spinner;
    }

    bool isOpen;
    int index;
    float slideVel;
    int shownIndex = -1;
    GameObject galleryRoot;
    Transform carousel;
    readonly List<GalleryItem> items = new List<GalleryItem>();
    bool infoPanelWasActive;
    bool hintWasActive;
    bool buyButtonWasActive;
    bool infoResolved;
    bool buyListenerAdded;
    bool buyMethodsResolved;
    MethodInfo buyMethod;
    MethodInfo ownedMethod;
    bool[] hiddenWasActive;
    readonly List<GameObject> disabledObjects = new List<GameObject>();
    readonly List<Transform> keepList = new List<Transform>();

    void Update()
    {
        if (!isOpen) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || GameInput.NavLeftDown) Move(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || GameInput.NavRightDown) Move(1);
        float targetX = carouselStartPosition.x - index * spacing;
        float x = Mathf.SmoothDamp(carousel.localPosition.x, targetX, ref slideVel,
                                   slideSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        carousel.localPosition = new Vector3(x, carouselStartPosition.y, carouselStartPosition.z);

        for (int i = 0; i < items.Count; i++)
            items[i].spinner.Rotate(0f, spinSpeed * Time.unscaledDeltaTime, 0f);

        bool settled = Mathf.Abs(x - targetX) < 0.02f;
        if (infoPanel != null) infoPanel.SetActive(settled);
        if (buyButton != null) buyButton.gameObject.SetActive(settled);
        if (!hideForWholeGallery) SetHidden(settled);
        if (settled && shownIndex != index) UpdateInfo();
    }

    public void OpenGallery()
    {
        if (isOpen) return;
        if (GameManager.Instance == null || GameManager.Instance.macheteOptions == null ||
            GameManager.Instance.macheteOptions.Count == 0)
        {
            Debug.LogWarning("MacheteGallery3D: no hay GameManager o macheteOptions está vacío.");
            return;
        }

        if (galleryRoot == null) BuildScene();

        isOpen = true;
        index = 0;
        slideVel = 0f;
        carousel.localPosition = carouselStartPosition;
        shownIndex = -1;

        ResolveInfoObjects();
        if (infoPanel != null)
        {
            infoPanelWasActive = infoPanel.activeSelf;
            infoPanel.SetActive(false);
        }

        if (buyButton != null)
        {
            buyButtonWasActive = buyButton.gameObject.activeSelf;
            buyButton.gameObject.SetActive(false);
            if (!buyListenerAdded)
            {
                buyButton.onClick.AddListener(OnBuyClicked);
                buyListenerAdded = true;
            }
        }

        if (hintObject != null)
        {
            hintWasActive = hintObject.activeSelf;
            SetText(hintObject, hintMessage);
            hintObject.SetActive(true);
        }

        StoreHiddenState();
        if (hideForWholeGallery) SetHidden(true);

        DisableOtherUI();

        ApplyRootTransform();
        galleryRoot.SetActive(true);
    }

    public void ToggleGallery()
    {
        if (isOpen) CloseGallery(); else OpenGallery();
    }

    public void CloseGallery()
    {
        if (!isOpen) return;
        isOpen = false;

        galleryRoot.SetActive(false);

        RestoreUI();
        if (infoPanel != null) infoPanel.SetActive(infoPanelWasActive);
        if (hintObject != null) hintObject.SetActive(hintWasActive);

        if (buyButton != null)
        {
            if (buyListenerAdded)
            {
                buyButton.onClick.RemoveListener(OnBuyClicked);
                buyListenerAdded = false;
            }
            buyButton.gameObject.SetActive(buyButtonWasActive);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshAllUI();
            StartCoroutine(RefreshUINextFrame());
        }
        RestoreHidden();
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

    void StoreHiddenState()
    {
        if (hideWhileOpen == null) { hiddenWasActive = null; return; }
        hiddenWasActive = new bool[hideWhileOpen.Length];
        for (int i = 0; i < hideWhileOpen.Length; i++)
            hiddenWasActive[i] = hideWhileOpen[i] != null && hideWhileOpen[i].activeSelf;
    }

    void SetHidden(bool hidden)
    {
        if (hideWhileOpen == null || hiddenWasActive == null) return;
        for (int i = 0; i < hideWhileOpen.Length; i++)
        {
            if (hideWhileOpen[i] == null || !hiddenWasActive[i]) continue;
            hideWhileOpen[i].SetActive(!hidden);
        }
    }

    void RestoreHidden()
    {
        if (hideWhileOpen == null || hiddenWasActive == null) return;
        for (int i = 0; i < hideWhileOpen.Length; i++)
            if (hideWhileOpen[i] != null) hideWhileOpen[i].SetActive(hiddenWasActive[i]);
        hiddenWasActive = null;
    }

    void ResolveBuyMethods()
    {
        if (buyMethodsResolved) return;
        buyMethodsResolved = true;

        Type gm = typeof(GameManager);
        buyMethod = FindIntMethod(gm, buyMethodName, BuyCandidates, false);
        ownedMethod = FindIntMethod(gm, ownedMethodName, OwnedCandidates, true);

        if (buyMethod == null && (onBuyPressed == null || onBuyPressed.GetPersistentEventCount() == 0))
            Debug.LogWarning("MacheteGallery3D: no encontré el método de compra en GameManager. " +
                             "Escribe su nombre en 'Buy Method Name' (debe recibir un int con el índice del machete) " +
                             "o conecta tu función en 'On Buy Pressed'.");
    }

    static MethodInfo FindIntMethod(Type t, string explicitName, string[] candidates, bool needsBool)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var names = new List<string>();
        if (!string.IsNullOrEmpty(explicitName)) names.Add(explicitName);
        else names.AddRange(candidates);

        foreach (var n in names)
        {
            foreach (var m in t.GetMethods(flags))
            {
                if (m.Name != n) continue;
                var ps = m.GetParameters();
                if (ps.Length != 1 || ps[0].ParameterType != typeof(int)) continue;
                if (needsBool && m.ReturnType != typeof(bool)) continue;
                return m;
            }
        }
        return null;
    }

    bool IsOwned(int i)
    {
        ResolveBuyMethods();
        if (ownedMethod != null && GameManager.Instance != null)
        {
            try { return (bool)ownedMethod.Invoke(GameManager.Instance, new object[] { i }); }
            catch (Exception e) { Debug.LogWarning("MacheteGallery3D: error al consultar si el machete es tuyo: " + e.Message); }
        }
        var list = GameManager.Instance.macheteOptions;
        return i >= 0 && i < list.Count && list[i].unlockCost <= 0;
    }

    void RefreshBuyButton()
    {
        if (buyButton == null) return;
        bool owned = IsOwned(index);
        buyButton.interactable = !owned;
        SetText(buyButton.gameObject, owned ? ownedLabel : buyLabel);
    }

    void OnBuyClicked()
    {
        if (!isOpen) return;
        ResolveBuyMethods();

        int i = index;
        if (IsOwned(i)) return;

        if (buyMethod != null)
        {
            try
            {
                object result = buyMethod.Invoke(GameManager.Instance, new object[] { i });
                if (result is bool ok && !ok)
                    Debug.Log("MacheteGallery3D: no se pudo comprar el machete " + i + " (¿no alcanza el agua?).");
            }
            catch (Exception e)
            {
                Debug.LogWarning("MacheteGallery3D: error al comprar el machete " + i + ": " + e.Message);
            }
        }

        if (onBuyPressed != null) onBuyPressed.Invoke(i);

        RefreshBuyButton();
        shownIndex = -1;
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
            Debug.LogWarning("MacheteGallery3D: 'Keep Active Panel' no está dentro de un Canvas.");

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
        if (buyButton != null && buyButton.transform.IsChildOf(t)) return true;
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

    void UpdateInfo()
    {
        shownIndex = index;
        RefreshBuyButton();
        if (infoPanel == null) return;

        var list = GameManager.Instance.macheteOptions;
        if (index < 0 || index >= list.Count) return;
        MacheteData m = list[index];

        SetText(titleObject, m.macheteName);
        SetText(descriptionObject, GetDescriptionText(m));
        SetText(priceObject, GetPriceText(m));
    }

    string GetDescriptionText(MacheteData m)
    {
        var sb = new StringBuilder();
        sb.Append("Daño: ").Append(m.baseDamage.ToString("0.##"));
        sb.Append("\nVelocidad: ").Append(m.baseSwingInterval.ToString("0.##")).Append(" s entre golpes");
        sb.Append("\nRadio: ").Append(m.baseHitRadius.ToString("0.##"));
        if (!string.IsNullOrEmpty(m.passiveDescription))
            sb.Append("\nMejora: ").Append(m.passiveDescription);
        return sb.ToString();
    }

    string GetPriceText(MacheteData m)
    {
        if (m.unlockCost <= 0) return freePriceText;
        return pricePrefix + m.unlockCost.ToString("N0", CultureInfo.InvariantCulture) + priceSuffix;
    }

    void ResolveInfoObjects()
    {
        if (infoResolved || infoPanel == null) return;
        infoResolved = true;

        if (titleObject == null) titleObject = FindChildByNames(infoPanel.transform, "titulo", "title", "nombre", "name");
        if (descriptionObject == null) descriptionObject = FindChildByNames(infoPanel.transform, "descripcion", "description", "desc");
        if (priceObject == null) priceObject = FindChildByNames(infoPanel.transform, "price", "precio");

        if (buyButton == null)
        {
            GameObject found = FindChildByNames(infoPanel.transform, "comprar", "compra", "buy");
            if (found != null) buyButton = found.GetComponent<Button>();
        }

        if (titleObject == null) Debug.LogWarning("MacheteGallery3D: no encontré 'Titulo' dentro del panel. Arrástralo en 'Title Object'.");
        if (descriptionObject == null) Debug.LogWarning("MacheteGallery3D: no encontré 'Descripcion' dentro del panel. Arrástralo en 'Description Object'.");
        if (priceObject == null) Debug.LogWarning("MacheteGallery3D: no encontré 'Price' dentro del panel. Arrástralo en 'Price Object'.");
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
        var list = GameManager.Instance.macheteOptions;
        int count = list.Count;
        galleryRoot = new GameObject("MacheteGalleryScene");
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

            GameObject model = CreateModel(list[i], i, holder.transform);
            StripGameplayComponents(model);
            model.transform.SetParent(spinner, false);
            Vector3 extra = (extraModelEuler != null && i < extraModelEuler.Length) ? extraModelEuler[i] : Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(modelEuler) * Quaternion.Euler(extra);
            FitModel(model.transform, spinner, slot);

            items.Add(new GalleryItem { slot = slot, spinner = spinner });
        }

        Destroy(holder);
        galleryRoot.SetActive(false);
    }

    GameObject CreateModel(MacheteData data, int i, Transform parent)
    {
        if (macheteModels != null && i < macheteModels.Length && macheteModels[i] != null)
            return Instantiate(macheteModels[i], parent);

        if (data != null)
        {
            foreach (var f in data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(GameObject))
                {
                    var go = f.GetValue(data) as GameObject;
                    if (go != null) return Instantiate(go, parent);
                }
            }
        }

        return BuildPlaceholder(parent, i);
    }

    GameObject BuildPlaceholder(Transform parent, int i)
    {
        var root = new GameObject("MachetePlaceholder_" + i);
        root.transform.SetParent(parent, false);

        float t = GameManager.Instance.macheteOptions.Count > 1
            ? (float)i / (GameManager.Instance.macheteOptions.Count - 1) : 0f;
        Color bladeColor = Color.Lerp(new Color(0.55f, 0.4f, 0.3f), new Color(0.85f, 0.85f, 0.95f), t);

        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.transform.SetParent(root.transform, false);
        blade.transform.localScale = new Vector3(0.18f, 1.2f, 0.04f);
        blade.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        blade.GetComponent<Renderer>().material.color = bladeColor;

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.transform.SetParent(root.transform, false);
        handle.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
        handle.transform.localPosition = new Vector3(0f, 0f, 0f);
        handle.GetComponent<Renderer>().material.color = new Color(0.25f, 0.15f, 0.08f);

        return root;
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

    System.Collections.IEnumerator RefreshUINextFrame()
    {
        yield return null;
        if (GameManager.Instance != null) GameManager.Instance.RefreshAllUI();
    }
}