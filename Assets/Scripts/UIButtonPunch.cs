using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.06f;
    public float pressScale = 0.92f;
    public float speed = 12f;

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

    public void OnPointerEnter(PointerEventData eventData) => targetScaleMult = hoverScale;
    public void OnPointerExit(PointerEventData eventData) => targetScaleMult = 1f;
    public void OnPointerDown(PointerEventData eventData) => targetScaleMult = pressScale;
    public void OnPointerUp(PointerEventData eventData) => targetScaleMult = 1f;
}