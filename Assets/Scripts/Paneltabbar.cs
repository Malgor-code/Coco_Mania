using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;
using System.Reflection;

public class PanelTabBar : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public string label = "Panel";
        [Tooltip("Botón de la pestaña. Si lo dejas vacío, la barra lo crea sola.")]
        public Button button;
        [Tooltip("Panel normal que se abre/cierra con esta pestaña (ej. tu panel del Árbol o del Gacha).")]
        public GameObject panel;
        [Tooltip("Opcional: galería de cocos que se abre/cierra con esta pestaña.")]
        public CoconutGallery3D coconutGallery;
        [Tooltip("Opcional: galería de machetes que se abre/cierra con esta pestaña.")]
        public MacheteGallery3D macheteGallery;
        [Tooltip("Opcional: tecla para abrir/cerrar esta pestaña.")]
        public KeyCode hotkey = KeyCode.None;
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();
    }

    [Header("Pestañas")]
    public List<Tab> tabs = new List<Tab>
    {
        new Tab { label = "Recaudación" },
        new Tab { label = "Árbol" },
        new Tab { label = "Galería de tienda" },
        new Tab { label = "Gacha" },
        new Tab { label = "Galería de cocos" },
    };
    [Tooltip("Pestaña abierta al empezar (-1 = ninguna).")]
    public int startTab = -1;
    [Tooltip("Si pulsas la pestaña que ya está abierta, se cierra.")]
    public bool allowCloseActive = true;
    [Tooltip("Si otro script abre un panel por su cuenta, la barra cierra los demás y se actualiza sola.")]
    public bool enforceSingleOpen = true;
    [Tooltip("Llama al RefreshAllUI de tu GameManager cada vez que cambia la pestaña (actualiza el panel de indicaciones).")]
    public bool refreshGameManagerUI = true;
    [Tooltip("Pone esta barra como 'Keep Active Panel' de las galerías para que no se apague al abrirlas.")]
    public bool overrideGalleryKeepPanel = true;

    [Header("Barra (se crea sola si hay pestañas sin botón)")]
    [Tooltip("Contenedor de los botones. Si lo dejas vacío se crea un Canvas con una barra arriba.")]
    public RectTransform barContainer;
    public float barHeight = 70f;
    public float spacing = 8f;
    public int fontSize = 28;
    public static PanelTabBar Instance;
    [Header("Colores")]
    public bool tintButtons = true;
    public Color normalColor = new Color(0.15f, 0.15f, 0.18f, 0.92f);
    public Color selectedColor = new Color(0.9f, 0.62f, 0.2f, 1f);
    public Color textColor = Color.white;

    int current = -1;
    bool[] isOpen;
    GameObject barRoot;
    MethodInfo refreshMethod;

    void Awake()
    {
        Instance = this;
        isOpen = new bool[tabs.Count];
        BuildBarIfNeeded();
        WireGalleries();

        for (int i = 0; i < tabs.Count; i++)
        {
            int idx = i;
            if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => Select(idx));
        }
    }

    void Start()
    {
        for (int i = 0; i < tabs.Count; i++) CloseTabObject(i);
        current = -1;

        if (startTab >= 0 && startTab < tabs.Count) Select(startTab);
        else { UpdateVisuals(); RefreshGM(); }
    }

    void Update()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            KeyCode k = tabs[i].hotkey;
            if (k != KeyCode.None && Input.GetKeyDown(k)) Select(i);
        }
        HandleGamepad();
        if (enforceSingleOpen) SyncExternal();
    }

    public void Select(int i)
    {
        if (i < 0 || i >= tabs.Count) { CloseAll(); return; }

        if (current == i && isOpen[i])
        {
            if (allowCloseActive) CloseAll();
            return;
        }

        for (int k = 0; k < tabs.Count; k++)
            if (k != i) CloseTabObject(k);

        OpenTabObject(i);
        current = i;
        UpdateVisuals();
        RefreshGM();
    }

    public void CloseAll()
    {
        for (int k = 0; k < tabs.Count; k++) CloseTabObject(k);
        current = -1;
        UpdateVisuals();
        RefreshGM();
    }

    void OpenTabObject(int i)
    {
        var t = tabs[i];
        if (t.panel != null) t.panel.SetActive(true);
        if (t.coconutGallery != null) t.coconutGallery.OpenGallery();
        if (t.macheteGallery != null) t.macheteGallery.OpenGallery();

        bool was = isOpen[i];
        isOpen[i] = true;
        if (!was) t.onOpened.Invoke();
    }

    void CloseTabObject(int k)
    {
        var t = tabs[k];
        bool was = isOpen[k];

        if (t.panel != null && t.panel.activeSelf) t.panel.SetActive(false);
        if (t.coconutGallery != null) t.coconutGallery.CloseGallery();
        if (t.macheteGallery != null) t.macheteGallery.CloseGallery();

        isOpen[k] = false;
        if (current == k) current = -1;
        if (was) t.onClosed.Invoke();
    }

    void SyncExternal()
    {
        if (current >= 0 && current < tabs.Count &&
            (tabs[current].coconutGallery != null || tabs[current].macheteGallery != null))
            return;

        bool changed = false;

        for (int j = 0; j < tabs.Count; j++)
        {
            var p = tabs[j].panel;
            if (p == null || tabs[j].coconutGallery != null || tabs[j].macheteGallery != null) continue;
            if (!p.activeSelf && isOpen[j])
            {
                isOpen[j] = false;
                if (current == j) current = -1;
                tabs[j].onClosed.Invoke();
                changed = true;
            }
        }

        if (changed) { UpdateVisuals(); RefreshGM(); }

        for (int j = 0; j < tabs.Count; j++)
        {
            var p = tabs[j].panel;
            if (p == null || tabs[j].coconutGallery != null || tabs[j].macheteGallery != null) continue;
            if (p.activeSelf && !isOpen[j])
            {
                Select(j);
                break;
            }
        }
    }

    void UpdateVisuals()
    {
        if (!tintButtons) return;
        for (int i = 0; i < tabs.Count; i++)
        {
            var b = tabs[i].button;
            if (b == null || b.targetGraphic == null) continue;
            b.targetGraphic.color = (i == current) ? selectedColor : normalColor;
        }
    }

    void RefreshGM()
    {
        if (!refreshGameManagerUI || GameManager.Instance == null) return;
        if (refreshMethod == null)
            refreshMethod = typeof(GameManager).GetMethod("RefreshAllUI",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (refreshMethod == null) return;
        try { refreshMethod.Invoke(GameManager.Instance, null); }
        catch (Exception e) { Debug.LogWarning("PanelTabBar: no pude refrescar la UI: " + e.Message); }
    }

    void WireGalleries()
    {
        if (barRoot == null)
        {
            foreach (var t in tabs)
                if (t.button != null) { barRoot = t.button.transform.parent != null ? t.button.transform.parent.gameObject : t.button.gameObject; break; }
        }
        if (barRoot == null || !overrideGalleryKeepPanel) return;

        foreach (var t in tabs)
        {
            if (t.coconutGallery != null) t.coconutGallery.keepActivePanel = barRoot;
            if (t.macheteGallery != null) t.macheteGallery.keepActivePanel = barRoot;
        }
    }

    void BuildBarIfNeeded()
    {
        bool needBuild = false;
        foreach (var t in tabs) if (t.button == null) { needBuild = true; break; }
        if (!needBuild) return;

        if (barContainer == null)
        {
            var canvasGO = new GameObject("TabBarCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var barGO = new GameObject("TabBar", typeof(RectTransform));
            barGO.transform.SetParent(canvasGO.transform, false);
            barContainer = barGO.GetComponent<RectTransform>();
            barContainer.anchorMin = new Vector2(0f, 1f);
            barContainer.anchorMax = new Vector2(1f, 1f);
            barContainer.pivot = new Vector2(0.5f, 1f);
            barContainer.sizeDelta = new Vector2(0f, barHeight);
            barContainer.anchoredPosition = Vector2.zero;

            var bg = barGO.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        }

        if (barContainer.GetComponent<HorizontalLayoutGroup>() == null &&
            barContainer.GetComponent<GridLayoutGroup>() == null &&
            barContainer.GetComponent<VerticalLayoutGroup>() == null)
        {
            var layout = barContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        barRoot = barContainer.gameObject;

        foreach (var t in tabs)
        {
            if (t.button != null) continue;
            t.button = CreateButton(t.label);
        }
    }

    Button CreateButton(string label)
    {
        var go = new GameObject("Tab_" + label, typeof(RectTransform));
        go.transform.SetParent(barContainer, false);

        var img = go.AddComponent<Image>();
        img.color = normalColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Texto", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6f, 4f);
        rt.offsetMax = new Vector2(-6f, -4f);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = fontSize;
        tmp.raycastTarget = false;

        return btn;
    }
    void HandleGamepad()
    {
        if (!GameInput.GamepadConnected) return;
        if (!AnyTabButtonVisible()) return;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;
        if (StoryManager.BlocksPause || CoconutUnlockPanel.BlocksPause) return;

        if (GameInput.TabNextDown) Cycle(1);
        else if (GameInput.TabPrevDown) Cycle(-1);
        else if (GameInput.BackDown && current >= 0) CloseAll();
    }

    bool AnyTabButtonVisible()
    {
        foreach (var t in tabs)
            if (t.button != null && t.button.gameObject.activeInHierarchy) return true;
        return false;
    }

    void Cycle(int dir)
    {
        int n = tabs.Count;
        if (n == 0) return;
        int idx = current;
        for (int tries = 0; tries < n; tries++)
        {
            idx = idx < 0 ? (dir > 0 ? 0 : n - 1) : (idx + dir + n) % n;
            var b = tabs[idx].button;
            if (b == null || b.gameObject.activeInHierarchy)
            {
                Select(idx);
                return;
            }
        }
    }
}