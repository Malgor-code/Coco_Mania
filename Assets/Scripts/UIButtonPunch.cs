using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public float hoverScale = 1.06f;
    public float pressScale = 0.92f;
    public float speed = 12f;

    [Header("Audio (opcional)")]
    [Tooltip("Si lo dejas vacio usa el sonido global de GameManager.Audios")]
    public AudioClip hoverClipOverride;
    public AudioClip pressClipOverride;
    public AudioClip clickClipOverride;
    [Range(0f, 1f)] public float overrideVolume = 1f;
    public bool silent = false;

    private Vector3 baseScale;
    private float targetScaleMult = 1f;
    private Selectable selectable;

    void Awake()
    {
        baseScale = transform.localScale;
        selectable = GetComponent<Selectable>();
    }

    void OnDisable()
    {
        targetScaleMult = 1f;
        transform.localScale = baseScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScaleMult, speed * Time.unscaledDeltaTime);
    }

    bool IsInteractable() => selectable == null || selectable.IsInteractable();

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScaleMult = hoverScale;
        if (IsInteractable()) PlaySound(hoverClipOverride, 0);
    }

    public void OnPointerExit(PointerEventData eventData) => targetScaleMult = 1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScaleMult = pressScale;
        if (IsInteractable()) PlaySound(pressClipOverride, 1);
    }

    public void OnPointerUp(PointerEventData eventData) => targetScaleMult = 1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInteractable()) PlaySound(clickClipOverride, 2);
    }

    void PlaySound(AudioClip overrideClip, int kind)
    {
        if (silent) return;

        if (overrideClip != null)
        {
            if (GameManager.Instance != null) GameManager.Instance.PlaySfx(overrideClip, overrideVolume);
            return;
        }

        if (GameManager.Instance == null) return;
        switch (kind)
        {
            case 0: GameManager.Instance.PlayButtonHover(); break;
            case 1: GameManager.Instance.PlayButtonPress(); break;
            case 2: GameManager.Instance.PlayButtonClick(); break;
        }
    }
}