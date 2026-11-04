using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GamepadUICursor : MonoBehaviour
{
    public float size = 40f;

    GameObject cursorGO;
    RectTransform cursorRT;
    readonly List<RaycastResult> results = new List<RaycastResult>();
    GameObject hovered;
    PointerEventData ped;

    void Awake()
    {
        var canvasGO = new GameObject("GamepadCursorCanvas", typeof(Canvas));
        canvasGO.transform.SetParent(transform, false);
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

        bool inMinigame3D = MinigameStage.Instance != null && MinigameStage.Instance.ActiveCamera != null;
        bool show = GameInput.UsingGamepad && Cursor.visible && !inMinigame3D && EventSystem.current != null;

        if (cursorGO.activeSelf != show) cursorGO.SetActive(show);
        if (!show) { ClearHover(); return; }

        cursorRT.position = pos;

        if (ped == null || ped.currentInputModule == null) ped = new PointerEventData(EventSystem.current);
        ped.position = pos;
        ped.button = PointerEventData.InputButton.Left;

        results.Clear();
        EventSystem.current.RaycastAll(ped, results);
        GameObject hit = null;
        RaycastResult hitResult = default;
        foreach (var r in results)
        {
            if (r.gameObject != null) { hit = r.gameObject; hitResult = r; break; }
        }

        GameObject enterTarget = hit != null ? ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hit) : null;
        if (enterTarget != hovered)
        {
            ClearHover();
            hovered = enterTarget;
            if (hovered != null) ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerEnterHandler);
        }

        if (GameInput.ClickDown && hit != null)
        {
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            if (target == null) target = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit);
            if (target != null)
            {
                ped.pointerPress = ped.rawPointerPress = target;
                ped.pointerPressRaycast = ped.pointerCurrentRaycast = hitResult;
                ped.clickCount = 1;
                ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerClickHandler);
            }
        }
    }

    void ClearHover()
    {
        if (hovered != null && ped != null)
            ExecuteEvents.Execute(hovered, ped, ExecuteEvents.pointerExitHandler);
        hovered = null;
    }

    void OnDisable() { ClearHover(); }
}