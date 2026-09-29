using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Parpadeo de vida baja")]
    [Tooltip("Por debajo de este porcentaje de vida (0.25 = 25%), el coco empieza a parpadear en rojo.")]
    [Range(0f, 1f)] public float lowHpBlinkThreshold = 0.25f;
    public Color lowHpBlinkColor = Color.red;
    [Tooltip("Cuantos parpadeos completos (encendido+apagado) por segundo.")]
    public float lowHpBlinksPerSecond = 4f;

    [Header("Fragmentos al romperse (como los cerdos)")]
    [Tooltip("Prefabs de los pedazos rotos del coco. Cada uno necesita un Collider y (opcional) un Rigidbody, se le agrega uno automaticamente si le falta. Se instancian TODOS al morir y salen disparados como una explosion. Mientras esto este vacio (tu artista todavia no te paso los pedazos), el coco simplemente desaparece al morir, sin salir volando.")]
    public GameObject[] fragmentPrefabs;
    public float fragmentExplosionForce = 5f;
    public float fragmentTorque = 8f;
    [Tooltip("Cuanto tiempo quedan los fragmentos en el suelo antes de desaparecer.")]
    public float fragmentLifetime = 3f;
    [Header("Habilidad (de solo lectura -- se resuelve automaticamente por nombre en ApplyType)")]
    public GameManager.CoconutAbility ability = GameManager.CoconutAbility.Ninguna;

    [Header("Ajustes de habilidad: Resistente (Correoso) / Armadura (Blindado) / Fragil (Diamante)")]
    [Range(0f, 1f)] public float resistenteDamageReduction = 0.30f;
    [Range(0f, 1f)] public float armaduraDamageReduction = 0.50f;
    [Range(0f, 1f)] public float diamanteDamageReduction = 0.75f;
    public float diamanteCritMultiplier = 4f;

    [Header("Ajustes de habilidad: Fibra Dura (Fibroso)")]
    public float fibraDuraStackPerHit = 0.05f;
    public float fibraDuraMaxStack = 0.25f;
    public float fibraDuraResetTime = 1.5f;
    private float fibraDuraCurrentStack = 0f;
    private float fibraDuraLastHitTime = -999f;

    [Header("Ajustes de habilidad: Tenaz (Curtido)")]
    [Range(0f, 1f)] public float tenazHpThreshold = 0.3f;
    [Range(0f, 1f)] public float tenazDamageReduction = 0.25f;

    [Header("Ajustes de habilidad: Rebote (Hierro)")]
    public int reboteHitsPerTrigger = 5;
    public float reboteStaminaPenalty = 2f;
    private int reboteHitCounter = 0;

    [Header("Ajustes de habilidad: Fortificado (Acero)")]
    public float fortificadoNormalDuration = 4f;
    public float fortificadoBuffDuration = 3f;
    [Range(0f, 1f)] public float fortificadoDamageReduction = 0.4f;
    private float fortificadoTimer = 0f;
    private bool fortificadoActive = false;

    [Header("Ajustes de habilidad: Implacable (Titanio)")]
    public int implacableHitsToTrigger = 4;
    public float implacableWindowSeconds = 2f;
    public float implacableBuffDuration = 2f;
    [Range(0f, 1f)] public float implacableDamageReduction = 0.3f;
    private readonly Queue<float> implacableRecentHitTimes = new Queue<float>();
    private float implacableBuffUntil = -999f;

    [Header("Ajustes de habilidad: Regeneracion (Legendario, y parcial en Supremo fase 2)")]
    public float regeneracionDelayAfterHit = 2f;
    [Range(0f, 0.2f)] public float regeneracionPercentPerSecond = 0.03f;
    [Range(0f, 0.2f)] public float supremoFase2RegenPercentPerSecond = 0.015f;
    private float regeneracionLastHitTime = -999f;
    private float regeneracionAccumulator = 0f;

    [Header("Ajustes de habilidad: Camuflaje (Mitico)")]
    public float camuflajeVisibleDuration = 3f;
    public float camuflajeHiddenDuration = 2f;
    [Range(0f, 1f)] public float camuflajeHiddenAlpha = 0.2f;
    private float camuflajeTimer = 0f;
    private bool camuflajeIsHidden = false;

    [Header("Ajustes de habilidad: Maldicion (Ancestral)")]
    public float maldicionDuration = 6f;

    [Header("Ajustes de habilidad: Supremo (mini-boss, 3 fases)")]
    [Range(0f, 1f)] public float supremoFase2HpThreshold = 0.7f;
    [Range(0f, 1f)] public float supremoFase3HpThreshold = 0.3f;
    [Range(0f, 1f)] public float supremoFase1DamageReduction = 0.2f;
    [Range(0f, 1f)] public float supremoFase3DamageReduction = 0.35f;
    private int supremoCurrentPhase = 1;

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

    private Coroutine blinkCoroutine;

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
        StopAllCoroutines();
        blinkCoroutine = null;

        currentType = type;
        isDead = false;

        ability = GameManager.Instance != null
            ? GameManager.Instance.GetAbilityForCoconutName(type.typeName)
            : GameManager.CoconutAbility.Ninguna;

        ResetAbilityState();

        int baseHp = GameManager.Instance != null ? GameManager.Instance.GetCoconutHpMax() : 10;
        hpMax = Mathf.Max(1, Mathf.RoundToInt(baseHp * type.hpMultiplier));
        hp = hpMax;

        float sizeBoost = 1f + (type.hpMultiplier - 1f) * 0.25f;
        typedScale = originalScale * sizeBoost;
        transform.localScale = typedScale;

        if (rend != null)
        {
            rend.material.color = restColor;
            Color c = rend.material.color;
            c.a = 1f;
            rend.material.color = c;
        }

        if (rend != null) rend.enabled = true;
    }

    void ResetAbilityState()
    {
        fibraDuraCurrentStack = 0f;
        fibraDuraLastHitTime = -999f;
        reboteHitCounter = 0;
        fortificadoTimer = 0f;
        fortificadoActive = false;
        implacableRecentHitTimes.Clear();
        implacableBuffUntil = -999f;
        regeneracionAccumulator = 0f;
        regeneracionLastHitTime = Time.time;
        camuflajeTimer = 0f;
        camuflajeIsHidden = false;
        supremoCurrentPhase = 1;
    }

    void Update()
    {
        if (isDead) return;

        switch (ability)
        {
            case GameManager.CoconutAbility.Fortificado: UpdateFortificado(); break;
            case GameManager.CoconutAbility.Implacable: UpdateImplacable(); break;
            case GameManager.CoconutAbility.Regeneracion: UpdateRegeneracion(regeneracionPercentPerSecond); break;
            case GameManager.CoconutAbility.Camuflaje: UpdateCamuflaje(); break;
            case GameManager.CoconutAbility.Supremo: UpdateSupremo(); break;
        }
    }

    void UpdateFortificado()
    {
        fortificadoTimer += Time.deltaTime;
        float cycleLength = fortificadoNormalDuration + fortificadoBuffDuration;
        float t = fortificadoTimer % cycleLength;
        bool shouldBeActive = t >= fortificadoNormalDuration;

        if (shouldBeActive != fortificadoActive)
        {
            fortificadoActive = shouldBeActive;
            if (rend != null) rend.material.color = fortificadoActive ? Color.Lerp(restColor, Color.black, 0.45f) : restColor;
        }
    }

    void UpdateImplacable()
    {
        float cutoff = Time.time - implacableWindowSeconds;
        while (implacableRecentHitTimes.Count > 0 && implacableRecentHitTimes.Peek() < cutoff)
            implacableRecentHitTimes.Dequeue();

        if (implacableRecentHitTimes.Count >= implacableHitsToTrigger && Time.time > implacableBuffUntil)
        {
            implacableBuffUntil = Time.time + implacableBuffDuration;
            StartCoroutine(FlashColor(Color.cyan, 0.15f));
        }
    }

    void UpdateRegeneracion(float percentPerSecond)
    {
        if (Time.time - regeneracionLastHitTime < regeneracionDelayAfterHit) { regeneracionAccumulator = 0f; return; }
        if (hp >= hpMax) return;

        regeneracionAccumulator += hpMax * percentPerSecond * Time.deltaTime;
        if (regeneracionAccumulator >= 1f)
        {
            int healed = Mathf.FloorToInt(regeneracionAccumulator);
            hp = Mathf.Min(hpMax, hp + healed);
            regeneracionAccumulator -= healed;
            if (rend != null) rend.material.color = Color.Lerp(restColor, Color.green, 0.5f);
        }
    }

    void UpdateCamuflaje()
    {
        camuflajeTimer += Time.deltaTime;
        float limit = camuflajeIsHidden ? camuflajeHiddenDuration : camuflajeVisibleDuration;
        if (camuflajeTimer >= limit)
        {
            camuflajeTimer = 0f;
            camuflajeIsHidden = !camuflajeIsHidden;
            if (rend != null)
            {
                Color c = rend.material.color;
                c.a = camuflajeIsHidden ? camuflajeHiddenAlpha : 1f;
                rend.material.color = c;
            }
        }
    }

    void UpdateSupremo()
    {
        float hpPercent = hpMax > 0 ? (float)hp / hpMax : 1f;
        int newPhase = hpPercent <= supremoFase3HpThreshold ? 3 : (hpPercent <= supremoFase2HpThreshold ? 2 : 1);

        if (newPhase != supremoCurrentPhase)
        {
            supremoCurrentPhase = newPhase;
            OnSupremoPhaseChanged(newPhase);
        }

        if (supremoCurrentPhase == 2)
        {
            UpdateRegeneracion(supremoFase2RegenPercentPerSecond);
        }
    }

    void OnSupremoPhaseChanged(int phase)
    {
        if (JuiceManager.Instance != null)
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 1.2f, "FASE " + phase, true);

        Color flashColor = phase == 2 ? Color.green : Color.red;
        StartCoroutine(FlashColor(flashColor, 0.3f));
    }

    IEnumerator FlashColor(Color color, float duration)
    {
        if (rend == null) yield break;
        rend.material.color = color;
        yield return new WaitForSeconds(duration);
        if (!isDead) rend.material.color = restColor;
    }


    float ModifyIncomingDamage(int rawAmount, bool isCritical)
    {
        float amount = rawAmount;

        switch (ability)
        {
            case GameManager.CoconutAbility.Resistente:
                if (!isCritical) amount *= (1f - resistenteDamageReduction);
                break;

            case GameManager.CoconutAbility.Armadura:
                if (!isCritical) amount *= (1f - armaduraDamageReduction);
                break;

            case GameManager.CoconutAbility.FragilValioso:
                amount = isCritical ? amount * diamanteCritMultiplier : amount * (1f - diamanteDamageReduction);
                break;

            case GameManager.CoconutAbility.FibraDura:
                {
                    if (Time.time - fibraDuraLastHitTime > fibraDuraResetTime) fibraDuraCurrentStack = 0f;
                    float mult = 1f + Mathf.Min(fibraDuraCurrentStack, fibraDuraMaxStack);
                    amount *= mult;
                    fibraDuraCurrentStack += fibraDuraStackPerHit;
                    fibraDuraLastHitTime = Time.time;
                }
                break;

            case GameManager.CoconutAbility.Tenaz:
                {
                    float hpPercent = hpMax > 0 ? (float)hp / hpMax : 1f;
                    if (hpPercent <= tenazHpThreshold) amount *= (1f - tenazDamageReduction);
                }
                break;

            case GameManager.CoconutAbility.Fortificado:
                if (fortificadoActive) amount *= (1f - fortificadoDamageReduction);
                break;

            case GameManager.CoconutAbility.Implacable:
                if (Time.time <= implacableBuffUntil) amount *= (1f - implacableDamageReduction);
                break;

            case GameManager.CoconutAbility.Supremo:
                if (supremoCurrentPhase == 1) amount *= (1f - supremoFase1DamageReduction);
                else if (supremoCurrentPhase == 3) amount *= (1f - supremoFase3DamageReduction);
                break;
        }

        return amount;
    }

    public bool TakeDamage(int amount, bool isCritical = false)
    {
        if (isDead) return false;

        float modified = ModifyIncomingDamage(amount, isCritical);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(modified));

        hp -= finalDamage;

        if (ability == GameManager.CoconutAbility.Rebote)
        {
            reboteHitCounter++;
            if (reboteHitCounter >= reboteHitsPerTrigger)
            {
                reboteHitCounter = 0;
                TriggerRebote();
            }
        }

        if (ability == GameManager.CoconutAbility.Implacable)
        {
            implacableRecentHitTimes.Enqueue(Time.time);
        }

        regeneracionLastHitTime = Time.time;

        if (wander != null) wander.Stun(stunOnHit);

        if (hitParticles != null)
        {
            GameObject hitFx = Instantiate(hitParticles, transform.position, Quaternion.identity);
            AutoDestroyFx(hitFx);
        }

        PlaySound(hitSounds);

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.5f, "-" + finalDamage, false);
        }

        StopAllCoroutines();
        blinkCoroutine = null;
        StartCoroutine(HitFeedback());

        bool killedNow = hp <= 0;
        if (killedNow)
        {
            Die();
        }

        return killedNow;
    }

    void TriggerRebote()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyStaminaPenalty(reboteStaminaPenalty);
        }
        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.8f, "¡REBOTE!", true);
        }
        StartCoroutine(FlashColor(Color.yellow, 0.2f));
    }

    void UpdateLowHpBlink()
    {
        float hpPercent = hpMax > 0 ? (float)hp / hpMax : 1f;
        if (hpPercent <= lowHpBlinkThreshold)
        {
            blinkCoroutine = StartCoroutine(LowHpBlink());
        }
    }

    IEnumerator LowHpBlink()
    {
        bool showBlinkColor = false;
        float halfCycle = lowHpBlinksPerSecond > 0f ? 1f / (lowHpBlinksPerSecond * 2f) : 0.15f;

        while (!isDead)
        {
            showBlinkColor = !showBlinkColor;
            if (rend != null) rend.material.color = showBlinkColor ? lowHpBlinkColor : restColor;
            yield return new WaitForSeconds(halfCycle);
        }
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

        GameObject deathFx = null;
        if (deathParticles != null)
        {
            deathFx = Instantiate(deathParticles, transform.position, Quaternion.identity);
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

        // Las Death Particles son las que viajan al jarron 3D (el agua real ya se sumo arriba).
        // Si el jarron las toma, el se encarga de destruirlas; si no, las destruimos aca.
        bool jarTookFx = false;
        if (GameManager.Instance != null)
        {
            jarTookFx = GameManager.Instance.PlayWaterDropEffect(transform.position, waterGained, deathFx);
        }
        if (!jarTookFx && deathFx != null) AutoDestroyFx(deathFx);

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.6f, "+$" + earned, true);
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.9f, "+" + Mathf.RoundToInt(waterGained) + "mL", false);
        }

        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.RemoveCoconut(this);

        Destroy(gameObject, 0.05f);
    }

    // Destruye un efecto de particulas cuando termina su animacion.
    static void AutoDestroyFx(GameObject fx)
    {
        if (fx != null) Destroy(fx, GetFxLifetime(fx));
    }

    static float GetFxLifetime(GameObject fx)
    {
        float max = 0f;
        foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule m = ps.main;
            float life = m.duration + m.startLifetimeMultiplier;
            if (life > max) max = life;
        }
        // Sin ParticleSystem (o valores raros): tiempo por defecto; y nunca mas de 10 s.
        return max > 0f ? Mathf.Min(max + 0.5f, 10f) : 3f;
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
        if (ability == GameManager.CoconutAbility.Maldicion)
        {
            if (GameManager.Instance != null) GameManager.Instance.ApplyTemporaryCurse(maldicionDuration);
            if (JuiceManager.Instance != null)
                JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 1f, "¡MALDICION!", true);
        }
        else if (ability == GameManager.CoconutAbility.Supremo)
        {
            if (JuiceManager.Instance != null)
                JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 1.2f, "¡DERROTADO!", true);
        }

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

        UpdateLowHpBlink();
    }

    float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}