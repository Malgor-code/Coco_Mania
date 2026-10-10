using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class LegacyTooltip : MonoBehaviour
{
    public static LegacyTooltip Instance;

    [Header("Textos")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descriptionText;

    [Header("Posicion")]
    [Tooltip("Separacion respecto al cursor, en unidades del canvas.")]
    public Vector2 offset = new Vector2(20f, -20f);

    [Header("Colores del costo")]
    public Color costColor = Color.white;
    public Color notEnoughColor = new Color(1f, 0.35f, 0.35f);
    public Color purchasedColor = new Color(0.5f, 1f, 0.55f);

    private RectTransform panel;
    private RectTransform canvasRect;
    private Canvas canvas;
    private CanvasGroup group;
    private bool visible;

    void Awake()
    {
        Instance = this;
        panel = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        canvas = GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;

        Hide();
    }

    public void Show(LegacyItem item, bool purchased, bool canAfford)
    {
        if (item == null) return;

        if (nameText != null) nameText.text = item.itemName;

        if (costText != null)
        {
            if (purchased)
            {
                costText.text = "Adquirido";
                costText.color = purchasedColor;
            }
            else
            {
                costText.text = "Costo: " + item.cost + " legado";
                costText.color = canAfford ? costColor : notEnoughColor;
            }
        }

        if (descriptionText != null) descriptionText.text = item.description;

        transform.SetAsLastSibling(); 
        visible = true;
        group.alpha = 1f;

       Canvas.ForceUpdateCanvases();
        FollowMouse();
    }

    public void Hide()
    {
        visible = false;
        if (group != null) group.alpha = 0f;
    }

    void LateUpdate()
    {
        if (visible) FollowMouse();
    }

    void FollowMouse()
    {
        Vector2 mouse = Input.mousePosition;
        float px = mouse.x > Screen.width * 0.5f ? 1f : 0f;
        float py = mouse.y < Screen.height * 0.5f ? 0f : 1f;
        panel.pivot = new Vector2(px, py);

        Vector2 off = new Vector2(px == 1f ? -offset.x : offset.x, py == 0f ? -offset.y : offset.y);
        Vector2 screenPoint = mouse + off * canvas.scaleFactor;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPoint, cam, out Vector3 world))
            panel.position = world;
    }
}