using UnityEngine;
using System.Collections;

public class CoconutTarget : MonoBehaviour
{
    [Header("Aturdimiento al recibir golpe")]
    public float stunOnHit = 0.6f;

    [Header("Particulas placeholder (opcional)")]
    public GameObject hitParticles;
    public GameObject deathParticles;

    public int hp;
    public int hpMax;

    private Vector3 originalScale;
    private Vector3 typedScale; 
    private bool isDead = false;
    private CoconutWander wander;
    private CoconutTypeData currentType;

    void Awake()
    {
        originalScale = transform.localScale;
        typedScale = originalScale;
        wander = GetComponent<CoconutWander>();
    }

    public void ApplyType(CoconutTypeData type)
    {
        currentType = type;

        int baseHp = GameManager.Instance != null ? GameManager.Instance.GetCoconutHpMax() : 10;
        hpMax = Mathf.Max(1, Mathf.RoundToInt(baseHp * type.hpMultiplier));
        hp = hpMax;

        float sizeBoost = 1f + (type.hpMultiplier - 1f) * 0.25f;
        typedScale = originalScale * sizeBoost;
        transform.localScale = typedScale;

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Color baseColor = new Color(0.55f, 0.38f, 0.22f);
            Color toughColor = new Color(0.18f, 0.1f, 0.06f);
            float t = Mathf.Clamp01((type.hpMultiplier - 1f) / 2.5f);
            rend.material.color = Color.Lerp(baseColor, toughColor, t);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        hp -= amount;

        if (wander != null) wander.Stun(stunOnHit);

        if (hitParticles != null)
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }

        StopAllCoroutines();
        StartCoroutine(HitFeedback());

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (deathParticles != null)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }

        OnSpecialAbilityTrigger();

        float loot = currentType != null ? currentType.lootMultiplier : 1f;
        if (GameManager.Instance != null) GameManager.Instance.OnCoconutDestroyed(loot);
        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.RemoveCoconut(this);

        Destroy(gameObject);
    }

    protected virtual void OnSpecialAbilityTrigger()
    {
        if (currentType == null || string.IsNullOrEmpty(currentType.specialAbilityId)) return;
       
    }

    IEnumerator HitFeedback()
    {
        transform.localScale = typedScale * 0.85f;
        yield return new WaitForSeconds(0.08f);
        if (!isDead) transform.localScale = typedScale;
    }
}