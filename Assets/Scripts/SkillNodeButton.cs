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

    [Header("Efecto de aparicion (pop) al desbloquearse / habilitarse")]
    [Tooltip("Que tan grande es el 'salto' de escala cuando se COMPRA el nodo")]
    public float popStrengthComprado = 0.45f;
    public float popDurationComprado = 0.35f;
    [Tooltip("Que tan grande es el 'salto' de escala cuando el nodo pasa de bloqueado a disponible")]
    public float popStrengthDisponible = 0.25f;
    public float popDurationDisponible = 0.25f;

    private enum NodeVisualState { Bloqueado, SinDinero, Disponible, Comprado }
    private NodeVisualState previousState = NodeVisualState.Bloqueado;
    private NodeVisualState currentState = NodeVisualState.Bloqueado;
    private bool stateInitialized = false;
    private Vector3 originalScale = Vector3.one;
    private Coroutine popCoroutine;

    private LineRenderer[] lines;
    private SkillNode cachedNode;
    private RectTransform rt;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        originalScale = transform.localScale;

        if (button != null) button.onClick.AddListener(OnClick);

        SetupLines();
    }

    void SetupLines()
    {
        if (prerequisiteButtons == null || prerequisiteButtons.Length == 0) return;

        lines = new LineRenderer[prerequisiteButtons.Length];
        for (int i = 0; i < prerequisiteButtons.Length; i++)
        {
            if (prerequisiteButtons[i] == null) continue;

            GameObject lineObj = new GameObject("Linea_" + nodeId + "_" + i);
            lineObj.transform.SetParent(transform.parent, false);
            lineObj.transform.SetAsFirstSibling(); 

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("UI/Default"));
            lines[i] = lr;
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
        label.text = unlocked ? cachedNode.nodeName + "\n(comprado)" : cachedNode.nodeName + "\n$" + cachedNode.cost;
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
                PlayPop(popStrengthComprado, popDurationComprado);
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
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopEffect(strength, duration));
    }

    System.Collections.IEnumerator PopEffect(float strength, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float scaleMult = 1f + Mathf.Sin(p * Mathf.PI) * strength; 
            transform.localScale = originalScale * scaleMult;
            yield return null;
        }
        transform.localScale = originalScale;
        popCoroutine = null;
    }

    void UpdateLines()
    {
        if (lines == null) return;

        bool thisVisible = currentState != NodeVisualState.Bloqueado;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null || prerequisiteButtons[i] == null) continue;

            lines[i].enabled = thisVisible;
            if (!thisVisible) continue;

            RectTransform preRt = prerequisiteButtons[i].GetComponent<RectTransform>();
            if (preRt == null) continue;

            lines[i].SetPosition(0, preRt.position);
            lines[i].SetPosition(1, rt.position);

            bool preUnlocked = GameManager.Instance.IsSkillUnlocked(prerequisiteButtons[i].nodeId);
            bool thisUnlocked = GameManager.Instance.IsSkillUnlocked(nodeId);

            Color c = (preUnlocked && thisUnlocked) ? lineColorActiva : lineColorInactiva;
            lines[i].startColor = c;
            lines[i].endColor = c;
        }
    }
}