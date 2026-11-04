using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-2000)]
public class GamepadUICursor : MonoBehaviour
{
    public float size = 40f;
    [Tooltip("Distancia (px) a la que el cursor se 'pega' al boton mas cercano.")]
    public float snapRadius = 60f;
    [Tooltip("Velocidad del cursor cuando esta sobre un boton (1 = normal).")]
    [Range(0.2f, 1f)] public float slowOverButton = 0.45f;

    GameObject cursorGO;
    RectTransform cursorRT;
    readonly List<RaycastResult> results = new List<RaycastResult>();
    readonly List<RaycastResult> tmp = new List<RaycastResult>();
    GameObject hovered;
    PointerEventData ped;
    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        var canvasGO = new GameObject("GamepadCursorCanvas", typeof(Canvas));
        canvasGO.transform.SetParent(null, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;

        cursorGO = new GameObject("Cursor", typeof(Image));
        cursorGO.transform.SetParent(canvasGO.transform, false);
        var img = cursorGO.GetComponent<Image>();
        img.sprite = MakeCircle();
        img.raycastTarget = false;
        cursorRT = img.rectTransform;
        cursorRT.sizeDelta = new Vector2(size, size);
        cursorRT.anchorMin = cursorRT.anchorMax = Vector2.zero;
        cursorRT.pivot = new Vector2(0.5f, 0.5f);
        cursorGO.SetActive(false);
    }

    void OnDestroy()
    {
        GameInput.CursorSlow = 1f;
        if (cursorGO != null && cursorGO.transform.parent != null)
            Destroy(cursorGO.transform.parent.gameObject);
    }

    static Sprite MakeCircle()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[n * n];
        float r = n * 0.5f;
        var c = new Vector2(r - 0.5f, r - 0.5f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(r - d);
                Color32 col = d > r * 0.7f ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                col.a = (byte)(a * 255f);
                px[y * n + x] = col;
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        Vector2 pos = GameInput.PointerPosition;

        if (GameInput.UsingGamepad && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        bool inMinigame3D = MinigameStage.Instance != null && MinigameStage.Instance.ActiveCamera != null;
        bool show = GameInput.UsingGamepad && Cursor.visible && !inMinigame3D && EventSystem.current != null;

        if (cursorGO.activeSelf != show) cursorGO.SetActive(show);
        if (!show) { ClearHover(); GameInput.CursorSlow = 1f; return; }

        cursorRT.position = pos;

        if (ped == null) ped = new PointerEventData(EventSystem.current);
        ped.position = pos;
        ped.button = PointerEventData.InputButton.Left;

        GameObject target = FindTarget(pos, out RaycastResult hitResult);
        GameInput.CursorSlow = target != null ? slowOverButton : 1f;

        if (target != hovered)
        {
            ClearHover();
            hovered = target;
            if (hovered != null) ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerEnterHandler);
        }

        if (GameInput.ClickDown && target != null)
        {
            ped.pointerPress = ped.rawPointerPress = target;
            ped.pointerPressRaycast = ped.pointerCurrentRaycast = hitResult;
            ped.clickCount = 1;
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerClickHandler);
            ped.pointerPress = ped.rawPointerPress = null;
        }
    }
    GameObject FindTarget(Vector2 pos, out RaycastResult hitResult)
    {
        hitResult = default;
        GameObject tab = FindTabButton(pos);
        if (tab != null) return tab;
        results.Clear();
        EventSystem.current.RaycastAll(ped, results);

        GameObject top = null;
        foreach (var r in results)
            if (r.gameObject != null) { top = r.gameObject; hitResult = r; break; }

        if (top != null)
        {
            GameObject direct = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top);
            if (direct != null)
            {
                Selectable s = direct.GetComponent<Selectable>();
                if (s == null || s.IsInteractable()) return direct;
            }
        }
        Selectable best = null;
        float bestD = snapRadius;
        foreach (var s in Selectable.allSelectablesArray)
        {
            if (s == null || !s.IsInteractable() || !s.gameObject.activeInHierarchy) continue;
            var rt = s.transform as RectTransform;
            if (rt == null) continue;
            float d = DistanceToRect(rt, pos, out Vector2 center);
            if (d >= bestD) continue;
            if (!Reachable(s, center)) continue;
            best = s; bestD = d;
        }
        if (best != null) { hitResult = default; return best.gameObject; }
        return null;
    }

    float DistanceToRect(RectTransform rt, Vector2 pos, out Vector2 center)
    {
        Canvas c = rt.GetComponentInParent<Canvas>();
        Camera cam = (c != null && c.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? c.rootCanvas.worldCamera : null;
        rt.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min = Vector2.Min(min, sp);
            max = Vector2.Max(max, sp);
        }
        center = (min + max) * 0.5f;
        float dx = Mathf.Max(min.x - pos.x, 0f, pos.x - max.x);
        float dy = Mathf.Max(min.y - pos.y, 0f, pos.y - max.y);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    bool Reachable(Selectable s, Vector2 screenPoint)
    {
        var p = new PointerEventData(EventSystem.current) { position = screenPoint };
        tmp.Clear();
        EventSystem.current.RaycastAll(p, tmp);
        foreach (var r in tmp)
        {
            if (r.gameObject == null) continue;
            return r.gameObject.transform.IsChildOf(s.transform);
        }
        return false;
    }

    void ClearHover()
    {
        if (hovered != null && ped != null)
            ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerExitHandler);
        hovered = null;
    }
    GameObject FindTabButton(Vector2 pos)
    {
        var bar = PanelTabBar.Instance;
        if (bar == null || bar.tabs == null) return null;
        if (StoryManager.BlocksPause || CoconutUnlockPanel.BlocksPause) return null;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return null;

        Button best = null;
        float bestD = snapRadius * 0.5f;
        foreach (var t in bar.tabs)
        {
            Button b = t.button;
            if (b == null || !b.IsInteractable() || !b.gameObject.activeInHierarchy) continue;
            var rt = b.transform as RectTransform;
            if (rt == null) continue;
            float d = DistanceToRect(rt, pos, out Vector2 c);
            if (d < bestD) { best = b; bestD = d; }
        }
        return best != null ? best.gameObject : null;
    }
    void OnDisable() { ClearHover(); }
}