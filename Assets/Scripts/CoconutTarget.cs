using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class CoconutTarget : MonoBehaviour
{
    [Header("Aturdimiento al recibir golpe")]
    public float stunOnHit = 0.6f;

    [Header("Particulas placeholder (opcional)")]
    public GameObject hitParticles;
    public GameObject deathParticles;

    [Header("Sonido (opcional, arrastra clips cuando los tengas)")]
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;
    [Range(0f, 0.3f)] public float pitchVariation = 0.1f;

    [Header("Feedback visual")]
    public float hitFeedbackDuration = 0.18f;
    public float squashAmount = 0.2f;

    public int hp;
    public int hpMax;

    private Vector3 originalScale;
    private Vector3 typedScale; 
    private Color restColor = Color.white;
    private bool isDead = false;
    private CoconutWander wander;
    private CoconutTypeData currentType;
    private Renderer rend;
    private AudioSource audioSource;

    void Awake()
    {
        originalScale = transform.localScale;
        typedScale = originalScale;
        wander = GetComponent<CoconutWander>();
        rend = GetComponentInChildren<Renderer>();

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
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

        if (rend != null)
        {
            Color baseColor = new Color(0.55f, 0.38f, 0.22f);
            Color toughColor = new Color(0.18f, 0.1f, 0.06f);
            float t = Mathf.Clamp01((type.hpMultiplier - 1f) / 2.5f);
            restColor = Color.Lerp(baseColor, toughColor, t);
            rend.material.color = restColor;
        }
    }
    public bool TakeDamage(int amount)
    {
        if (isDead) return false;

        hp -= amount;

        if (wander != null) wander.Stun(stunOnHit);

        if (hitParticles != null)
        {
            Instantiate(hitParticles, transform.position, Quaternion.identity);
        }

        PlaySound(hitSounds);

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.5f, "-" + amount, false);
        }

        StopAllCoroutines();
        StartCoroutine(HitFeedback());

        bool killedNow = hp <= 0;
        if (killedNow) Die();

        return killedNow;
    }

    void Die()
    {
        isDead = true;

        if (deathParticles != null)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }

        if (deathSounds != null && deathSounds.Length > 0)
        {
            AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        OnSpecialAbilityTrigger();

        float loot = currentType != null ? currentType.lootMultiplier : 1f;
        int earned = GameManager.Instance != null ? GameManager.Instance.OnCoconutDestroyed(loot) : 0;

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.6f, "+$" + earned, true);
        }

        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.RemoveCoconut(this);

        Destroy(gameObject);
    }

    protected virtual void OnSpecialAbilityTrigger()
    {
        if (currentType == null || string.IsNullOrEmpty(currentType.specialAbilityId)) return;
    }

    void PlaySound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
    IEnumerator HitFeedback()
    {
        if (rend != null) rend.material.color = Color.white;
        transform.localScale = typedScale * (1f - squashAmount);

        float t = 0f;
        while (t < hitFeedbackDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / hitFeedbackDuration);
            float eased = EaseOutBack(p);

            float scaleMult = Mathf.LerpUnclamped(1f - squashAmount, 1f, eased);
            transform.localScale = typedScale * scaleMult;

            if (rend != null) rend.material.color = Color.Lerp(Color.white, restColor, p);

            yield return null;
        }

        transform.localScale = typedScale;
        if (rend != null) rend.material.color = restColor;
    }

    float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}