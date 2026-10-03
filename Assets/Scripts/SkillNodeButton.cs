using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNodeButton : MonoBehaviour
{
    [Header("Datos del nodo")]
    public string nodeId;

    [Header("UI")]
    public TMP_Text label;
    public Button button;
    public Image backgroundImage;

    [Tooltip("Opcional: un overlay/candado que se prende si todavia no se puede comprar")]
    public GameObject lockedOverlay;

    [Header("Colores segun estado")]
    public Color colorComprado = new Color(0.30f, 0.85f, 0.30f);
    public Color colorDisponible = new Color(1.00f, 0.85f, 0.20f);
    public Color colorBloqueado = new Color(0.35f, 0.35f, 0.35f);
    public Color colorSinDinero = new Color(0.55f, 0.30f, 0.30f);

    [Header("Ocultar nodo cuando esta Bloqueado (opcional pero recomendado)")]
    [Tooltip("Si lo asignas, se usa para ocultar TODO el nodo (fondo, texto, candado) poniendo su alpha en 0 cuando el nodo esta Bloqueado, en vez de tocar colores a mano. Necesita un CanvasGroup en el mismo GameObject que el boton.")]
    public CanvasGroup canvasGroup;

    [Header("Lineas hacia los prerequisitos (SOLO VISUAL)")]
    [Tooltip("Esto UNICAMENTE dibuja las lineas de conexion en pantalla. NO afecta si el nodo se puede comprar. El requisito real para comprar se configura en GameManager > Skill Tree > (el nodo) > Prerequisite Ids, escribiendo ahi el id de texto de cada nodo requerido.")]
    public SkillNodeButton[] prerequisiteButtons;
    public float lineWidth = 4f;
    public Color lineColorInactiva = new Color(0.35f, 0.35f, 0.35f, 0.6f);
    public Color lineColorActiva = new Color(1f, 0.85f, 0.2f, 0.9f);

    [Header("Efecto de aparicion (pop) al habilitarse")]
    [Tooltip("Que tan grande es el 'salto' de escala cuando el nodo pasa de bloqueado a disponible")]
    public float popStrengthDisponible = 0.25f;
    public float popDurationDisponible = 0.25f;

    [Header("Efecto de COMPRA (se achica, tiembla y crece con el nuevo color)")]
    [Tooltip("Escala a la que se achica (0.6 = 60% del tamano)")]
    public float purchaseShrinkScale = 0.6f;
    public float purchaseShrinkDuration = 0.12f;
    [Tooltip("Cuanto dura el temblor mientras esta chiquito")]
    public float purchaseShakeDuration = 0.25f;
    [Tooltip("Angulo maximo del temblor en grados")]
    public float purchaseShakeAngle = 12f;
    [Tooltip("Cuantas oscilaciones tiene el temblor")]
    public float purchaseShakeFrequency = 4f;
    [Tooltip("Cuanto dura el crecimiento final (con rebote)")]
    public float purchaseGrowDuration = 0.35f;

    private enum NodeVisualState { Bloqueado, SinDinero, Disponible, Comprado }
    private NodeVisualState previousState = NodeVisualState.Bloqueado;
    private NodeVisualState currentState = NodeVisualState.Bloqueado;
    private bool stateInitialized = false;
    private Vector3 originalScale = Vector3.one;
    private Quaternion originalRotation = Quaternion.identity;
    private Coroutine popCoroutine;
    private Coroutine purchaseCoroutine;
    private bool purchaseAnimating = false;
    private float purchaseColorBlend = 0f;
    private Image[] lineImages;
    private RectTransform[] lineRects;
    private SkillNode cachedNode;
    private RectTransform rt;
    private RectTransform parentRt;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        parentRt = transform.parent as RectTransform;
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        if (button != null) button.onClick.AddListener(OnClick);

        SetupLines();
    }
    void SetupLines()
    {
        if (prerequisiteButtons == null || prerequisiteButtons.Length == 0) return;

        lineImages = new Image[prerequisiteButtons.Length];
        lineRects = new RectTransform[prerequisiteButtons.Length];

        for (int i = 0; i < prerequisiteButtons.Length; i++)
        {
            if (prerequisiteButtons[i] == null) continue;

            GameObject lineObj = new GameObject("Linea_" + nodeId + "_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform lrt = lineObj.GetComponent<RectTransform>();
            lrt.SetParent(transform.parent, false);
            lrt.SetAsFirstSibling();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            LayoutElement le = lineObj.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            Image img = lineObj.GetComponent<Image>();
            img.raycastTarget = false;
            lineImages[i] = img;
            lineRects[i] = lrt;
        }
    }

    public void OnClick()
    {
        if (GameManager.Instance != null) GameManager.Instance.UnlockSkill(nodeId);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (cachedNode == null)
        {
            cachedNode = GameManager.Instance.GetSkillNode(nodeId);
            if (cachedNode == null) return;
        }

        bool unlocked = GameManager.Instance.IsSkillUnlocked(nodeId);
        bool canUnlock = GameManager.Instance.CanUnlockSkillPublic(nodeId);
        bool prereqsMet = ArePrerequisitesMet();

        UpdateLabel(unlocked);
        UpdateVisualState(unlocked, canUnlock, prereqsMet);
        UpdateLines();

        if (button != null) button.interactable = canUnlock;
        if (lockedOverlay != null) lockedOverlay.SetActive(currentState == NodeVisualState.SinDinero);
    }

    bool ArePrerequisitesMet()
    {
        if (cachedNode.prerequisiteIds == null || cachedNode.prerequisiteIds.Length == 0) return true;

        foreach (var pre in cachedNode.prerequisiteIds)
        {
            if (!string.IsNullOrEmpty(pre) && !GameManager.Instance.IsSkillUnlocked(pre)) return false;
        }
        return true;
    }

    void UpdateLabel(bool unlocked)
    {
        if (label == null) return;
        label.text = unlocked ? cachedNode.nodeName + "\n(comprado)" : cachedNode.nodeName + "\n" + cachedNode.cost + " ml" ;
    }

    void UpdateVisualState(bool unlocked, bool canUnlock, bool prereqsMet)
    {
        NodeVisualState newState;
        if (unlocked) newState = NodeVisualState.Comprado;
        else if (!prereqsMet) newState = NodeVisualState.Bloqueado;
        else if (canUnlock) newState = NodeVisualState.Disponible;
        else newState = NodeVisualState.SinDinero;

        HandleStateChange(newState);
        currentState = newState;

        bool visible = newState != NodeVisualState.Bloqueado;

        if (backgroundImage != null)
        {
            Color c;
            switch (newState)
            {
                case NodeVisualState.Comprado: c = colorComprado; break;
                case NodeVisualState.Disponible: c = colorDisponible; break;
                case NodeVisualState.SinDinero: c = colorSinDinero; break;
                default: c = colorBloqueado; break;
            }
            if (purchaseAnimating)
                c = Color.Lerp(colorDisponible, colorComprado, purchaseColorBlend);

            c.a = visible ? 1f : 0f;
            backgroundImage.color = c;
        }

        ApplyVisibility(visible);
    }

    void ApplyVisibility(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (label != null)
        {
            Color c = label.color;
            c.a = visible ? 1f : 0f;
            label.color = c;
        }
    }

    void HandleStateChange(NodeVisualState newState)
    {
        if (!stateInitialized)
        {
            previousState = newState;
            stateInitialized = true;
            return;
        }

        if (newState != previousState)
        {
            if (newState == NodeVisualState.Comprado)
            {
                PlayPurchase();
            }
            else if (newState == NodeVisualState.Disponible && previousState == NodeVisualState.Bloqueado)
            {
                PlayPop(popStrengthDisponible, popDurationDisponible);
            }
        }

        previousState = newState;
    }

    void PlayPop(float strength, float duration)
    {
        if (purchaseCoroutine != null) return;
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopEffect(strength, duration));
    }

    System.Collections.IEnumerator PopEffect(float strength, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float scaleMult = 1f + Mathf.Sin(p * Mathf.PI) * strength;
            transform.localScale = originalScale * scaleMult;
            yield return null;
        }
        transform.localScale = originalScale;
        popCoroutine = null;
    }

    void PlayPurchase()
    {
        if (popCoroutine != null) { StopCoroutine(popCoroutine); popCoroutine = null; }
        if (purchaseCoroutine != null) StopCoroutine(purchaseCoroutine);
        purchaseCoroutine = StartCoroutine(PurchaseEffect());
    }

    System.Collections.IEnumerator PurchaseEffect()
    {
        purchaseAnimating = true;
        purchaseColorBlend = 0f;

        Vector3 smallScale = originalScale * purchaseShrinkScale;

        float t = 0f;
        while (t < purchaseShrinkDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / purchaseShrinkDuration);
            transform.localScale = Vector3.Lerp(originalScale, smallScale, EaseOutQuad(p));
            yield return null;
        }
        transform.localScale = smallScale;
        t = 0f;
        while (t < purchaseShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / purchaseShakeDuration);
            float amplitude = 1f - p;
            float angle = Mathf.Sin(p * purchaseShakeFrequency * Mathf.PI * 2f) * purchaseShakeAngle * amplitude;
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        transform.localRotation = originalRotation;
        t = 0f;
        while (t < purchaseGrowDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / purchaseGrowDuration);
            purchaseColorBlend = Mathf.Clamp01(p * 3f);
            transform.localScale = Vector3.LerpUnclamped(smallScale, originalScale, EaseOutBack(p));
            yield return null;
        }

        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        purchaseColorBlend = 1f;
        purchaseAnimating = false;
        purchaseCoroutine = null;
    }

    static float EaseOutQuad(float p)
    {
        return 1f - (1f - p) * (1f - p);
    }

    static float EaseOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
    }

    void UpdateLines()
    {
        if (lineImages == null || parentRt == null) return;

        bool thisVisible = currentState != NodeVisualState.Bloqueado;

        for (int i = 0; i < lineImages.Length; i++)
        {
            if (lineImages[i] == null || prerequisiteButtons[i] == null) continue;

            lineImages[i].enabled = thisVisible;
            if (!thisVisible) continue;

            RectTransform preRt = prerequisiteButtons[i].GetComponent<RectTransform>();
            if (preRt == null) continue;
            Vector3 a = parentRt.InverseTransformPoint(preRt.position);
            Vector3 b = parentRt.InverseTransformPoint(rt.position);
            Vector3 dir = b - a;
            float length = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            RectTransform lrt = lineRects[i];
            lrt.localPosition = (a + b) * 0.5f;
            lrt.localRotation = Quaternion.Euler(0f, 0f, angle);
            lrt.localScale = Vector3.one;
            lrt.sizeDelta = new Vector2(length, lineWidth);

            bool preUnlocked = GameManager.Instance.IsSkillUnlocked(prerequisiteButtons[i].nodeId);
            bool thisUnlocked = GameManager.Instance.IsSkillUnlocked(nodeId);

            lineImages[i].color = (preUnlocked && thisUnlocked) ? lineColorActiva : lineColorInactiva;
        }
    }
}