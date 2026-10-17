using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public static class MiniUI
{
    static Sprite circle;

    public static Sprite Circle
    {
        get
        {
            if (circle == null) circle = MakeCircleSprite(128);
            return circle;
        }
    }
    static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = size * 0.5f;
        var center = new Vector2(r - 0.5f, r - 0.5f);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01(r - d);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    public static Image Fill(string name, Transform parent, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var im = go.GetComponent<Image>();
        im.color = c;
        return im;
    }
    public static Image Img(string name, Transform parent, Color c, Vector2 pos, Vector2 size, bool round = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Center;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var im = go.GetComponent<Image>();
        im.color = c;
        im.raycastTarget = false;
        if (round) im.sprite = Circle;
        return im;
    }

    public static TMP_Text Label(Transform parent, string text, float size, Vector2 pos, Vector2 box,
                                 Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Center;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color ?? Color.white;
        t.raycastTarget = false;
        return t;
    }

    public static Button Btn(Transform parent, string text, Vector2 pos, Vector2 size, Color col, UnityAction onClick,
                             bool round = false, float fontSize = 36f)
    {
        Image im = Img("Btn", parent, col, pos, size, round);
        im.raycastTarget = true;
        var b = im.gameObject.AddComponent<Button>();
        b.targetGraphic = im;
        if (onClick != null) b.onClick.AddListener(onClick);
        if (!string.IsNullOrEmpty(text)) Label(im.transform, text, fontSize, Vector2.zero, size);
        return b;
    }

    public static Vector2 MouseLocal(RectTransform area)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, Input.mousePosition, null, out Vector2 local);
        return local;
    }
}