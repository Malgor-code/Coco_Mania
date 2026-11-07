using System.Collections.Generic;
using UnityEngine;

public enum LegendaryAbility
{
    None,
    DamageAura,   // dano continuo a los cocos cercanos 
    Shockwave,    // onda que daña y empuja a los cocos
    Lightning,    // rayo a un coco aleatorio en rango 
    WaterZone     // los cocos que mueren dentro dan mas agua 
}

public class LegendaryItem : MonoBehaviour
{
    public static readonly List<LegendaryItem> Active = new List<LegendaryItem>();

    [Header("Alcance y ritmo (ajustar por prefab)")]
    [SerializeField] float radius = 4f;
    [Tooltip("Segundos entre cada pulso/rayo/onda. Ignorado por WaterZone.")]
    [SerializeField] float interval = 1f;
    [SerializeField] LayerMask targetMask = ~0;

    [Header("Solo Shockwave")]
    [SerializeField] float knockback = 6f;

    [Header("Visual (opcional)")]
    [Tooltip("Circulo/decal que se escala al diametro del radio.")]
    [SerializeField] Transform rangeIndicator;
    [SerializeField] GameObject lightningVfxPrefab;
    [SerializeField] GameObject pulseVfxPrefab;
    [SerializeField] float bobHeight = 0.15f;
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float spinSpeed = 45f;
    [Tooltip("Hijo con el modelo; si esta vacio se anima todo el objeto.")]
    [SerializeField] Transform model;

    public RewardData Data { get; private set; }
    public LegendaryAbility Ability { get; private set; }
    public float Magnitude { get; private set; }
    public float Radius => radius;

    readonly Collider[] buffer = new Collider[64];
    readonly List<CoconutTarget> found = new List<CoconutTarget>(64);
    float timer;
    Vector3 modelBaseLocalPos;
    Transform Visual => model != null ? model : transform;

    public void Init(RewardData data)
    {
        Data = data;
        Ability = data.ability;
        Magnitude = data.value;
        if (rangeIndicator != null)
            rangeIndicator.localScale = new Vector3(radius * 2f, rangeIndicator.localScale.y, radius * 2f);
    }

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    void Start() { modelBaseLocalPos = Visual.localPosition; }

    void Update()
    {
        Visual.localPosition = modelBaseLocalPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        Visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (Ability == LegendaryAbility.None || Ability == LegendaryAbility.WaterZone) return;

        timer += Time.deltaTime;
        if (timer < interval) return;
        timer -= interval;
        Trigger();
    }

    void Trigger()
    {
        FindCoconuts();
        if (found.Count == 0) return;

        switch (Ability)
        {
            case LegendaryAbility.DamageAura:
                foreach (var c in found) Damage(c, Magnitude);
                break;

            case LegendaryAbility.Shockwave:
                if (pulseVfxPrefab != null) Destroy(Instantiate(pulseVfxPrefab, transform.position, Quaternion.identity), 2f);
                foreach (var c in found)
                {
                    var rb = c.GetComponent<Rigidbody>();
                    Damage(c, Magnitude);
                    if (rb != null) rb.AddExplosionForce(knockback, transform.position, radius, 0.3f, ForceMode.Impulse);
                }
                break;

            case LegendaryAbility.Lightning:
                var target = found[Random.Range(0, found.Count)];
                if (lightningVfxPrefab != null)
                    Destroy(Instantiate(lightningVfxPrefab, target.transform.position, Quaternion.identity), 1.5f);
                Damage(target, Magnitude);
                break;
        }
    }

    void FindCoconuts()
    {
        found.Clear();
        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, targetMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var c = buffer[i].GetComponentInParent<CoconutTarget>();
            if (c != null && !found.Contains(c)) found.Add(c);
        }
    }
    void Damage(CoconutTarget c, float amount)
    {
        c.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount)), false, transform.position);
    }

    public static float GetWaterMultiplierAt(Vector3 pos)
    {
        float bonus = 0f;
        foreach (var item in Active)
        {
            if (item.Ability != LegendaryAbility.WaterZone) continue;
            Vector3 d = item.transform.position - pos;
            d.y = 0f; 
            if (d.sqrMagnitude <= item.radius * item.radius) bonus += item.Magnitude;
        }
        return 1f + bonus;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}