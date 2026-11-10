using UnityEngine;

public class NotificationBadges : MonoBehaviour
{
    [Header("Badges (hijos de cada boton)")]
    public GameObject treeBadge;
    public GameObject gachaBadge;
    public GameObject shopBadge;

    [Tooltip("Cada cuantos segundos se revisa.")]
    public float checkInterval = 0.25f;

    float timer;

    void OnEnable() => Refresh();

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < checkInterval) return;
        timer = 0f;
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Set(treeBadge, gm.CanAffordAnySkill());
        Set(shopBadge, gm.CanAffordAnyMachete());
        Set(gachaBadge, gm.CanUseGacha());
    }

    static void Set(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }
}