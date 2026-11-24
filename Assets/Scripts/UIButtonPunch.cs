using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.06f;
    public float pressScale = 0.92f;
    public float speed = 12f;

    [Header("Sonido (ids de la biblioteca de audio; vacio = sin sonido)")]
    public string hoverSfxId = SfxIds.UiHover;
    public string clickSfxId = SfxIds.UiClick;

    private Vector3 baseScale;
    private float targetScaleMult = 1f;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScaleMult, speed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScaleMult = hoverScale;
        PlaySfx(hoverSfxId);
    }

    public void OnPointerExit(PointerEventData eventData) => targetScaleMult = 1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScaleMult = pressScale;
        PlaySfx(clickSfxId);
    }

    public void OnPointerUp(PointerEventData eventData) => targetScaleMult = 1f;

    static void PlaySfx(string id)
    {
        if (string.IsNullOrEmpty(id) || AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySfx(id);
    }
}