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

    [Header("Fragmentos al romperse (como los cerdos)")]
    [Tooltip("Prefabs de los pedazos rotos del coco. Cada uno necesita un Collider y (opcional) un Rigidbody, se le agrega uno automaticamente si le falta. Se instancian TODOS al morir y salen disparados como una explosion. Mientras esto este vacio (tu artista todavia no te paso los pedazos), el coco simplemente desaparece al morir, sin salir volando.")]
    public GameObject[] fragmentPrefabs;
    public float fragmentExplosionForce = 5f;
    public float fragmentTorque = 8f;
    [Tooltip("Cuanto tiempo quedan los fragmentos en el suelo antes de desaparecer.")]
    public float fragmentLifetime = 3f;

    [Header("Modelo visual por tier (opcional)")]
    [Tooltip("Donde se instancia el modelo del tier si CoconutTypeData.visualPrefab tiene algo. Si lo dejas vacio, se usa el propio transform del coco.")]
    public Transform modelSlot;
    private GameObject currentModelInstance;

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
    private Animator animator;

    void Awake()
    {
        originalScale = transform.localScale;
        typedScale = originalScale;
        wander = GetComponent<CoconutWander>();
        rend = GetComponentInChildren<Renderer>();
        animator = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        DesyncAnimation();
    }

    void DesyncAnimation()
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        animator.Play(state.fullPathHash, 0, Random.Range(0f, 1f));
        animator.speed = Random.Range(0.9f, 1.1f);
    }

    public void ApplyType(CoconutTypeData type)
    {
        currentType = type;
        isDead = false;

        int baseHp = GameManager.Instance != null ? GameManager.Instance.GetCoconutHpMax() : 10;
        hpMax = Mathf.Max(1, Mathf.RoundToInt(baseHp * type.hpMultiplier));
        hp = hpMax;

        float sizeBoost = 1f + (type.hpMultiplier - 1f) * 0.25f;
        typedScale = originalScale * sizeBoost;
        transform.localScale = typedScale;

        // Si este tier trae su propio modelo 3D, lo instancia y le pasa la
        // referencia de renderer/animator (para que el flash/squash y la
        // desincronizacion de animacion sigan funcionando con el modelo
        // nuevo). Si no trae, se queda con el placeholder de tinte/escala.
        if (type.visualPrefab != null)
        {
            if (currentModelInstance != null) Destroy(currentModelInstance);

            Transform slot = modelSlot != null ? modelSlot : transform;
            currentModelInstance = Instantiate(type.visualPrefab, slot);
            currentModelInstance.transform.localPosition = Vector3.zero;
            currentModelInstance.transform.localRotation = Quaternion.identity;
            currentModelInstance.transform.localScale = Vector3.one;

            Renderer modelRend = currentModelInstance.GetComponentInChildren<Renderer>();
            if (modelRend != null) rend = modelRend;

            Animator modelAnimator = currentModelInstance.GetComponentInChildren<Animator>();
            if (modelAnimator != null)
            {
                animator = modelAnimator;
                DesyncAnimation();
            }
        }

        if (rend != null)
        {
            float t = Mathf.Clamp01((type.hpMultiplier - 1f) / 2.5f);
            rend.material.color = restColor;
        }

        if (rend != null) rend.enabled = true;
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

        if (wander != null) wander.enabled = false;

        bool hasFragments = fragmentPrefabs != null && fragmentPrefabs.Length > 0;

        if (hasFragments)
        {
            SpawnFragments();
            if (rend != null) rend.enabled = false;
        }

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
        int earned = 0;
        float waterGained = 0f;
        if (GameManager.Instance != null)
        {
            earned = GameManager.Instance.OnCoconutDestroyed(loot, out waterGained);
        }

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.6f, "+$" + earned, true);
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.9f, "+" + Mathf.RoundToInt(waterGained) + "mL", false);
        }

        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.RemoveCoconut(this);

        Destroy(gameObject, 0.05f);
    }

    void SpawnFragments()
    {
        foreach (var prefab in fragmentPrefabs)
        {
            if (prefab == null) continue;
            Vector3 worldPos = transform.TransformPoint(prefab.transform.localPosition);
            Quaternion worldRot = transform.rotation * prefab.transform.localRotation;

            GameObject frag = Instantiate(prefab, worldPos, worldRot);

            float sizeRatio = originalScale.x > 0.0001f ? typedScale.x / originalScale.x : 1f;
            frag.transform.localScale *= sizeRatio;

            Rigidbody fragRb = frag.GetComponent<Rigidbody>();
            if (fragRb == null) fragRb = frag.AddComponent<Rigidbody>();

            Vector3 outDir = worldPos - transform.position;
            if (outDir.sqrMagnitude < 0.0001f) outDir = Random.onUnitSphere;
            outDir = (outDir.normalized + Vector3.up * 0.6f).normalized;

            fragRb.AddForce(outDir * fragmentExplosionForce, ForceMode.Impulse);
            fragRb.AddTorque(Random.insideUnitSphere * fragmentTorque, ForceMode.Impulse);

            Destroy(frag, fragmentLifetime);
        }
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