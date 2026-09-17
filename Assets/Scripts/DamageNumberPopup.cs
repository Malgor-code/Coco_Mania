using System.Collections;
using UnityEngine;
using TMPro;

public class DamageNumberPopup : MonoBehaviour
{
    public TMP_Text label;
    public float riseSpeed = 1.3f;
    public float lifetime = 0.7f;
    public Color normalColor = Color.white;
    public Color bigColor = new Color(1f, 0.85f, 0.2f);

    public void Setup(string text, bool big)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();

        if (label != null)
        {
            label.text = text;
            label.color = big ? bigColor : normalColor;
            transform.localScale = Vector3.one * (big ? 1.4f : 1f);
        }

        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float t = 0f;
        Vector3 start = transform.position;
        Camera cam = Camera.main;

        while (t < lifetime)
        {
            t += Time.unscaledDeltaTime;
            float p = t / lifetime;

            transform.position = start + Vector3.up * riseSpeed * t;

            if (cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }

            if (label != null)
            {
                Color c = label.color;
                c.a = 1f - Mathf.Clamp01(p);
                label.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}