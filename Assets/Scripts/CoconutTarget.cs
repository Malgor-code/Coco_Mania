using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

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
    [Header("Corte de machete (como se separan los pedazos)")]
    public float fragmentSeparationForce = 0.5f;
    public float fragmentFollowThroughForce = 0.7f;
    public float fragmentUpForce = 0.25f;
    public float fragmentDrag = 4f;
    public float fragmentAngularDrag = 5f;
    public float fragmentExtraGravity = 15f;
    public float fragmentTorque = 1.5f;
    [Range(0f, 1f)] public float fragmentRandomness = 0.25f;
    private Vector3 lastHitFrom;
    private bool hitFromExplicit;
    [Tooltip("Cuanto tiempo quedan los fragmentos en el suelo antes de desaparecer.")]
    public float fragmentLifetime = 3f;
    [Header("Habilidad (de solo lectura -- se resuelve automaticamente por nombre en ApplyType)")]
    public GameManager.CoconutAbility ability = GameManager.CoconutAbility.Ninguna;
    [Header("Desvanecido de fragmentos")]
    [Tooltip("Ultimo tramo de 'Fragment Lifetime' en que se desvanecen.")]
    public float fragmentFadeDuration = 1.2f;
    [Range(0f, 1f)] public float fragmentShrinkTo = 0.6f;
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
    [Header("Animacion al recibir golpe")]
    [Tooltip("Animator que se activa al golpear. Si el objeto es hijo del prefab, arrastralo aqui. Si esta en la escena, dejalo vacio y asignalo en CoconutSpawner: el spawner lo pasa solo.")]
    public Animator hitAnimator;
    [Header("Animacion al aparecer (spawn)")]
    [Tooltip("Nombre EXACTO del estado de spawn en el Animator Controller. Vacio = no se reproduce nada.")]
    public string spawnStateName = "";
    [Tooltip("Segundos que el coco se queda quieto (sin caminar) mientras dura la animacion de aparicion. 0 = no se detiene.")]
    public float spawnStunDuration = 0f;
    private int spawnStateHash;
    private bool warnedMissingSpawnState = false;
    [Tooltip("Nombre EXACTO del estado de la animacion en el Animator Controller.")]
    public string hitStateName = "Hit";
    [Header("Ajustes de habilidad: Regeneracion (Legendario, y parcial en Supremo fase 2)")]
    public float regeneracionDelayAfterHit = 2f;
    [Range(0f, 0.2f)] public float regeneracionPercentPerSecond = 0.03f;
    [Range(0f, 0.2f)] public float supremoFase2RegenPercentPerSecond = 0.015f;
    private float regeneracionLastHitTime = -999f;
    private float regeneracionAccumulator = 0f;
    [Tooltip("Nombre del CLIP (no del estado). Vacio = mismo que hitStateName. Sirve para leer su duracion.")]
    public string hitClipName = "";
    [Tooltip("Desde que punto del clip arranca (0-1). Sube este valor si el clip tiene un 'anticipo' antes del golpe visible.")]
    [Range(0f, 0.9f)] public float hitAnimStartNormalized = 0f;
    [Tooltip("Opcional: parametro float del Animator usado como Speed Multiplier del estado Hit. Si el golpe del machete es mas rapido que el clip, se acelera para que alcance a terminar.")]
    public string hitSpeedParameter = "";
    public float maxHitAnimSpeed = 4f;
    [HideInInspector] public int lastDamageTaken = 0;
    private int hitStateHash;
    private float hitClipLength = -1f;
    private bool warnedMissingHitState = false;
    [Header("Ajustes de habilidad: Camuflaje (Mitico)")]
    public float camuflajeVisibleDuration = 3f;
    public float camuflajeHiddenDuration = 2f;
    [Range(0f, 1f)] public float camuflajeHiddenAlpha = 0.2f;
    private float camuflajeTimer = 0f;
    private bool camuflajeIsHidden = false;
    public Renderer colorRenderer;
    [Header("Ajustes de habilidad: Maldicion (Ancestral)")]
    public float maldicionDuration = 6f;
    [Header("Collider objetivo (donde se le pega)")]
    public Collider hitCollider;
    [Header("Ajustes de habilidad: Supremo (mini-boss, 3 fases)")]
    [Range(0f, 1f)] public float supremoFase2HpThreshold = 0.7f;
    [Range(0f, 1f)] public float supremoFase3HpThreshold = 0.3f;
    [Range(0f, 1f)] public float supremoFase1DamageReduction = 0.2f;
    [Range(0f, 1f)] public float supremoFase3DamageReduction = 0.35f;
    private int supremoCurrentPhase = 1;
    public int hp;
    public int hpMax;
    [Header("Moneda 3D (cuando el coco suelta moneda)")]
    public GameObject coinPrefab;
    public float coinLifetime = 300f;
    public float coinPopForce = 3f;
    public float baseScale = 1f;
    public float scaleGrowth = 0.08f;
    public float maxScale = 1.5f;
    [Header("Flash al recibir golpe")]
    public Color hitFlashColor = Color.white;
    [Range(0f, 3f)] public float hitFlashEmission = 1.5f;
    [Range(1f, 6f)] public float hitFlashTintBoost = 3f;
    private List<Material> flashMats = new List<Material>();
    private Vector3 originalScale;
    private Vector3 typedScale;
    private Color restColor = Color.white;
    private bool isDead = false;
    private CoconutWander wander;
    private CoconutTypeData currentType;
    private Renderer rend;
    private AudioSource audioSource;
    private Animator animator;
    [Header("Decals de daño (arrastra aqui, en orden: 1er golpe, 2do, 3ro)")]
    [Tooltip("Cada golpe activa el siguiente decal. Si el coco muere en 2 golpes, solo se ven los 2 primeros.")]
    public GameObject[] damageDecals;
    private int decalsShown = 0;
    private Coroutine blinkCoroutine;
    [Header("Colocacion automatica de decals")]
    public bool autoPlaceDecals = true;
    [Tooltip("Que tan despegado de la superficie (0.01 para quad/sprite, 0 para Decal Projector).")]
    public float decalSurfaceOffset = 0.01f;
    [Tooltip("Que tan dispersos salen respecto a la camara (0 = todos al centro, 0.5 = muy repartidos).")]
    [Range(0f, 1f)] public float decalSpread = 0.4f;
    [Tooltip("Marca esto si el decal se ve de espaldas (quad/sprite volteado).")]
    public bool flipDecalFacing = false;
    [Header("Decals de daño (arrastra SPRITES, en orden: 1er golpe, 2do, 3ro)")]
    public Sprite[] damageDecalSprites;
    [Tooltip("Tamaño del decal en unidades de mundo.")]
    public float decalSize = 0.5f;
    [Header("Decals de daño (arrastra MESHES/PREFABS, en orden: 1er golpe, 2do, 3ro)")]
    public GameObject[] damageDecalPrefabs;
    [Tooltip("Multiplicador del tamaño original del prefab.")]
    public float decalScale = 1f;
    [Tooltip("Corrige la orientacion si tu mesh sale de lado o al reves. Prueba (90,0,0), (-90,0,0) o (180,0,0).")]
    public Vector3 decalRotationOffset = Vector3.zero;
    public bool randomSpin = true;
    private readonly List<GameObject> spawnedDecals = new List<GameObject>();
    public Color decalColor = Color.white;
    public int decalSortingOrder = 100;
    public Renderer decalTarget;
    void Awake()
    {
        HideChildFragments();
        ResetDecals();
        originalScale = transform.localScale;
        typedScale = originalScale;
        if (hitCollider == null)
        {
            foreach (var c in GetComponentsInChildren<Collider>())
            {
                if (c.GetComponentInParent<FragmentFader>() != null) continue;
                hitCollider = c;
                break;
            }
        }
        wander = GetComponent<CoconutWander>();
        rend = colorRenderer;
        if (rend == null)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<FragmentFader>() != null) continue;
                rend = r;
                break;
            }
        }
        animator = GetComponentInChildren<Animator>();
        if (hitAnimator == null) hitAnimator = animator;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        if (rend != null)
        {
            foreach (var m in rend.materials)
            {
                m.EnableKeyword("_EMISSION");
                flashMats.Add(m);
            }
        }
        if (rend != null) { restColor = rend.material.color; restColor.a = 1f; }
        SetFlash(0f);
        DesyncAnimation();
    }

    void DesyncAnimation()
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        animator.Play(state.fullPathHash, 0, Random.Range(0f, 1f));
        if (animator != hitAnimator) animator.speed = Random.Range(0.9f, 1.1f);
    }
    void SetFlash(float t)
    {
        Color e = hitFlashColor * (hitFlashEmission * t);
        foreach (var m in flashMats)
            if (m != null && m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", e);
    }
    public void ApplyType(CoconutTypeData type, float hpScale = 1f)
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
        hpMax = Mathf.Max(1, Mathf.RoundToInt(baseHp * type.hpMultiplier * hpScale));
        hp = hpMax;

        float globalSize = CoconutSpawner.Instance != null ? CoconutSpawner.Instance.globalSizeMultiplier : 1f;
        float sizeBoost = Mathf.Min(baseScale + Mathf.Sqrt(Mathf.Max(0f, type.hpMultiplier - 1f)) * scaleGrowth, maxScale) * globalSize;
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
        PlaySpawnAnimation();
    }

    void ResetAbilityState()
    {
        ResetDecals();
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
    void PlayHitAnimation()
    {
        if (hitAnimator == null || string.IsNullOrEmpty(hitStateName)) return;

        if (hitStateHash == 0) hitStateHash = Animator.StringToHash(hitStateName);

        if (!hitAnimator.HasState(0, hitStateHash))
        {
            if (!warnedMissingHitState)
            {
                warnedMissingHitState = true;
                Debug.LogWarning("[CoconutTarget] El Animator no tiene el estado '" + hitStateName + "'. Revisa 'Hit State Name'.", this);
            }
            return;
        }

        if (!string.IsNullOrEmpty(hitSpeedParameter))
        {
            float speed = 1f;
            if (MacheteController.Instance != null)
            {
                if (hitClipLength < 0f) hitClipLength = ResolveHitClipLength();
                float window = Mathf.Max(0.1f, MacheteController.Instance.swingInterval * 0.9f);
                if (hitClipLength > window) speed = Mathf.Min(hitClipLength / window, maxHitAnimSpeed);
            }
            hitAnimator.SetFloat(hitSpeedParameter, speed);
        }

        hitAnimator.Play(hitStateHash, 0, hitAnimStartNormalized);
        hitAnimator.Update(0f); 
    }
    void PlaySpawnAnimation()
    {
        if (hitAnimator == null || string.IsNullOrEmpty(spawnStateName)) return;

        if (spawnStateHash == 0) spawnStateHash = Animator.StringToHash(spawnStateName);

        if (!hitAnimator.HasState(0, spawnStateHash))
        {
            if (!warnedMissingSpawnState)
            {
                warnedMissingSpawnState = true;
                Debug.LogWarning("[CoconutTarget] El Animator no tiene el estado '" + spawnStateName + "'. Revisa 'Spawn State Name'.", this);
            }
            return;
        }

        hitAnimator.Play(spawnStateHash, 0, 0f);
        hitAnimator.Update(0f);

        if (spawnStunDuration > 0f && wander != null) wander.Stun(spawnStunDuration);
    }
    float ResolveHitClipLength()
    {
        if (hitAnimator == null || hitAnimator.runtimeAnimatorController == null) return 0f;
        string clipName = string.IsNullOrEmpty(hitClipName) ? hitStateName : hitClipName;
        foreach (var clip in hitAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == clipName) return clip.length;
        }
        return 0f;
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
                    float mult = 1f - Mathf.Min(fibraDuraCurrentStack, fibraDuraMaxStack);
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
        if (!hitFromExplicit)
            lastHitFrom = MacheteController.Instance != null
                ? MacheteController.Instance.transform.position
                : transform.position - transform.forward;
        float hpPercentNow = hpMax > 0 ? (float)hp / hpMax : 1f;
        float executeMult = GameManager.Instance != null
            ? GameManager.Instance.GetExecuteDamageMultiplier(hpPercentNow)
            : 1f;
        float modified = ModifyIncomingDamage(amount, isCritical) * executeMult;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(modified));
        lastDamageTaken = finalDamage;

        hp -= finalDamage;
        if (executeMult > 1f && JuiceManager.Instance != null)
    JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * 0.9f, "¡REMATE!", true);
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
        PlayHitAnimation();
        ShowNextDecal();
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
    public bool TakeDamage(int amount, bool isCritical, Vector3 hitFrom)
    {
        lastHitFrom = hitFrom;
        hitFromExplicit = true;
        bool killed = TakeDamage(amount, isCritical);
        hitFromExplicit = false;
        return killed;
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
    void OnDestroy()
    {
        foreach (var m in flashMats) if (m != null) Destroy(m);
    }
    void Die()
    {
        isDead = true;
        if (hitCollider != null) hitCollider.enabled = false;
        if (wander != null) wander.enabled = false;

        FragmentFader[] pieces = GetComponentsInChildren<FragmentFader>(true);
        bool hasFragments = pieces.Length > 0 || (fragmentPrefabs != null && fragmentPrefabs.Length > 0);

        if (hasFragments)
        {
            if (pieces.Length > 0) SpawnChildFragments(pieces);
            else SpawnFragments();
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
            GameManager.Instance.ApplyChainSplash(transform.position, lastDamageTaken, gameObject);
        }
        if (GameManager.Instance != null && GameManager.Instance.lastKillFoundCoin) SpawnCoin();
        bool jarTookFx = false;
        if (GameManager.Instance != null)
        {
            jarTookFx = GameManager.Instance.PlayWaterDropEffect(transform.position, waterGained, deathFx);
        }
        if (!jarTookFx && deathFx != null) AutoDestroyFx(deathFx);

        if (JuiceManager.Instance != null && GameManager.Instance != null)
        {
            JuiceManager.Instance.SpawnKillRewards(
                transform.position + Vector3.up * 0.5f,
                waterGained,
                GameManager.Instance.lastKillFoundCoin);
        }

        if (CoconutSpawner.Instance != null) CoconutSpawner.Instance.RemoveCoconut(this);

        Destroy(gameObject, 0.05f);
    }
    void SpawnCoin()
    {
        if (coinPrefab == null) return;
        GameObject coin = Instantiate(coinPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        if (coin.GetComponentInChildren<Collider>() == null) coin.AddComponent<SphereCollider>();
        Rigidbody rb = coin.GetComponent<Rigidbody>();
        if (rb == null) rb = coin.AddComponent<Rigidbody>();

        Vector3 dir = (Random.insideUnitSphere * 0.5f + Vector3.up).normalized;
        rb.AddForce(dir * coinPopForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        Destroy(coin, coinLifetime); 
    }
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
        return max > 0f ? Mathf.Min(max + 0.5f, 10f) : 3f;
    }
    void HideChildFragments()
    {
        foreach (var p in GetComponentsInChildren<FragmentFader>(true))
        {
            foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer)) r.enabled = false;
            foreach (var c in p.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }
    void GetSlashBasis(out Vector3 slashDir, out Vector3 side)
    {
        slashDir = transform.position - lastHitFrom;
        slashDir.y = 0f;
        if (slashDir.sqrMagnitude < 0.0001f)
        {
            Vector2 r = Random.insideUnitCircle.normalized;
            slashDir = new Vector3(r.x, 0f, r.y);
        }
        slashDir.Normalize();
        side = Vector3.Cross(Vector3.up, slashDir);
    }

    void LaunchFragment(GameObject frag, Vector3 worldPos, Vector3 slashDir, Vector3 side)
    {
        Rigidbody rb = frag.GetComponent<Rigidbody>();
        if (rb == null) rb = frag.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearDamping = fragmentDrag;
        rb.angularDamping = fragmentAngularDrag;
        rb.maxDepenetrationVelocity = 1f;
        var cf = frag.GetComponent<ConstantForce>();
        if (cf == null) cf = frag.AddComponent<ConstantForce>();
        cf.force = Vector3.down * rb.mass * fragmentExtraGravity;
        Vector3 off = worldPos - transform.position;
        off.y = 0f;
        float s = Vector3.Dot(off, side);
        float sign = Mathf.Abs(s) > 0.01f ? Mathf.Sign(s) : (Random.value < 0.5f ? -1f : 1f);

        float k = 1f + Random.Range(-fragmentRandomness, fragmentRandomness);
        Vector3 velocity = side * sign * fragmentSeparationForce
                         + slashDir * fragmentFollowThroughForce
                         + Vector3.up * fragmentUpForce;
        rb.AddForce(velocity * k, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * fragmentTorque, ForceMode.VelocityChange);

        FragmentFader.Attach(frag, fragmentLifetime, fragmentFadeDuration, fragmentShrinkTo);
    }

    void IgnoreFragmentCollisions(List<GameObject> frags)
    {
        var cols = new List<Collider>();
        foreach (var f in frags) cols.AddRange(f.GetComponentsInChildren<Collider>());
        for (int i = 0; i < cols.Count; i++)
            for (int j = i + 1; j < cols.Count; j++)
                Physics.IgnoreCollision(cols[i], cols[j]);
    }

    void SpawnFragments()
    {
        GetSlashBasis(out Vector3 slashDir, out Vector3 side);
        float sizeRatio = originalScale.x > 0.0001f ? typedScale.x / originalScale.x : 1f;
        var spawned = new List<GameObject>();

        foreach (var prefab in fragmentPrefabs)
        {
            if (prefab == null) continue;
            Vector3 worldPos = transform.TransformPoint(prefab.transform.localPosition);
            Quaternion worldRot = transform.rotation * prefab.transform.localRotation;

            GameObject frag = Instantiate(prefab, worldPos, worldRot);
            frag.SetActive(true);
            foreach (var rr in frag.GetComponentsInChildren<Renderer>(true))
                if (!(rr is ParticleSystemRenderer)) rr.enabled = true;
            frag.transform.localScale *= sizeRatio;
            spawned.Add(frag);
        }

        IgnoreFragmentCollisions(spawned);
        foreach (var frag in spawned)
            LaunchFragment(frag, frag.transform.position, slashDir, side);
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
        SetFlash(1f);
        Color flashTint = hitFlashColor * hitFlashTintBoost;
        flashTint.a = 1f;
        if (rend != null) rend.material.color = flashTint;
        transform.localScale = typedScale * (1f - squashAmount);

        float t = 0f;
        while (t < hitFeedbackDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / hitFeedbackDuration);
            float eased = EaseOutBack(p);
            SetFlash(1f - p);
            float scaleMult = Mathf.LerpUnclamped(1f - squashAmount, 1f, eased);
            transform.localScale = typedScale * scaleMult;

            if (rend != null) rend.material.color = Color.Lerp(flashTint, restColor, p);

            yield return null;
        }
        Color RestTint()
        {
            Color c = restColor;
            if (ability == GameManager.CoconutAbility.Fortificado && fortificadoActive)
                c = Color.Lerp(restColor, Color.black, 0.45f);
            if (ability == GameManager.CoconutAbility.Camuflaje && camuflajeIsHidden)
                c.a = camuflajeHiddenAlpha;
            return c;
        }
        transform.localScale = typedScale;
        SetFlash(0f);
        if (rend != null) rend.material.color = restColor;
        UpdateLowHpBlink();
    }

    float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
    public Vector3 HitPoint
    {
        get { return hitCollider != null ? hitCollider.bounds.center : transform.position; }
    }
    public Vector3 GetClosestHitPoint(Vector3 from)
    {
        if (hitCollider == null) return transform.position;
        Vector3 probe = new Vector3(from.x, hitCollider.bounds.center.y, from.z);
        return hitCollider.ClosestPoint(probe);
    }
    void SpawnChildFragments(FragmentFader[] pieces)
    {
        Vector3 slashDir = transform.position - lastHitFrom;
        slashDir.y = 0f;
        if (slashDir.sqrMagnitude < 0.0001f)
        {
            Vector2 r = Random.insideUnitCircle.normalized;
            slashDir = new Vector3(r.x, 0f, r.y);
        }
        slashDir.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, slashDir);

        foreach (var piece in pieces)
        {
            GameObject frag = piece.gameObject;
            Vector3 worldPos = frag.transform.position;
            frag.transform.SetParent(null, true);
            frag.SetActive(true);
            foreach (var rr in frag.GetComponentsInChildren<Renderer>(true))
                if (!(rr is ParticleSystemRenderer)) rr.enabled = true;

            var cols = frag.GetComponentsInChildren<Collider>(true);
            if (cols.Length == 0) frag.AddComponent<BoxCollider>();
            else foreach (var c in cols) c.enabled = true;

            Rigidbody fragRb = frag.GetComponent<Rigidbody>();
            if (fragRb == null) fragRb = frag.AddComponent<Rigidbody>();
            fragRb.linearDamping = fragmentDrag;
            fragRb.angularDamping = fragmentAngularDrag;

            Vector3 off = worldPos - transform.position;
            off.y = 0f;
            Vector3 outDir = off.sqrMagnitude > 0.0004f
                ? off.normalized
                : side * (Random.value < 0.5f ? -1f : 1f);

            float k = 1f + Random.Range(-fragmentRandomness, fragmentRandomness);
            Vector3 velocity = outDir * fragmentSeparationForce
                             + slashDir * fragmentFollowThroughForce
                             + Vector3.up * fragmentUpForce;
            fragRb.AddForce(velocity * k, ForceMode.VelocityChange);
            fragRb.AddTorque(Random.insideUnitSphere * fragmentTorque, ForceMode.VelocityChange);

            FragmentFader.Attach(frag, fragmentLifetime, fragmentFadeDuration, fragmentShrinkTo);
        }
    }
    void ResetDecals()
    {
        decalsShown = 0;
        foreach (var d in spawnedDecals)
            if (d != null) Destroy(d);
        spawnedDecals.Clear();
    }

    void ShowNextDecal()
    {
        if (damageDecalSprites == null || decalsShown >= damageDecalSprites.Length) return;
        Sprite sprite = damageDecalSprites[decalsShown];
        if (sprite == null) { decalsShown++; return; }

        Renderer target = decalTarget != null ? decalTarget : rend;
        if (target == null) return;

        Bounds b = target.bounds;
        Vector3 center = b.center;
        float far = b.extents.magnitude * 3f;

        Camera cam = Camera.main;
        Vector3 baseDir = cam != null ? (cam.transform.position - center).normalized : Vector3.up;
        Vector3 dir = (baseDir + Random.insideUnitSphere * decalSpread).normalized;

        Vector3 point, normal;
        bool found = RaycastRendererMesh(target, new Ray(center + dir * far, -dir), out point, out normal);
        if (!found) found = RaycastRendererMesh(target, new Ray(center + baseDir * far, -baseDir), out point, out normal);

        if (!found)
        {
            Vector3 ext = Vector3.Max(b.extents, Vector3.one * 0.0001f);
            float k = Mathf.Sqrt(dir.x * dir.x / (ext.x * ext.x) + dir.y * dir.y / (ext.y * ext.y) + dir.z * dir.z / (ext.z * ext.z));
            point = center + dir / Mathf.Max(0.0001f, k);
            Vector3 q = point - center;
            normal = new Vector3(q.x / (ext.x * ext.x), q.y / (ext.y * ext.y), q.z / (ext.z * ext.z)).normalized;
        }

        GameObject go = new GameObject("DamageDecal_" + (decalsShown + 1));
        go.transform.SetParent(target.transform, true);
        go.transform.position = point + normal * decalSurfaceOffset;
        go.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Vector3 ps = target.transform.lossyScale;
        float avg = Mathf.Max(0.0001f, (Mathf.Abs(ps.x) + Mathf.Abs(ps.y) + Mathf.Abs(ps.z)) / 3f);
        float spriteSize = Mathf.Max(0.0001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
        go.transform.localScale = Vector3.one * (decalSize / spriteSize / avg);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = decalColor;
        sr.sortingOrder = decalSortingOrder + decalsShown;

        spawnedDecals.Add(go);
        decalsShown++;
    }

    bool warnedUnreadableMesh = false;

    bool RaycastRendererMesh(Renderer target, Ray ray, out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.up;

        Mesh mesh;
        Matrix4x4 l2w;
        bool temp = false;

        if (target is SkinnedMeshRenderer smr)
        {
            mesh = new Mesh();
            smr.BakeMesh(mesh);
            temp = true;
            Transform t = smr.transform;
            l2w = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
        }
        else
        {
            MeshFilter mf = target.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;
            mesh = mf.sharedMesh;
            if (!mesh.isReadable)
            {
                if (!warnedUnreadableMesh)
                {
                    warnedUnreadableMesh = true;
                    Debug.LogWarning("[CoconutTarget] El mesh '" + mesh.name + "' no es legible. Activa 'Read/Write' en el import del modelo para que los decals queden pegados.", this);
                }
                return false;
            }
            l2w = target.transform.localToWorldMatrix;
        }

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        if (temp) Destroy(mesh);

        float bestT = float.MaxValue;
        bool hit = false;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = l2w.MultiplyPoint3x4(verts[tris[i]]);
            Vector3 v1 = l2w.MultiplyPoint3x4(verts[tris[i + 1]]);
            Vector3 v2 = l2w.MultiplyPoint3x4(verts[tris[i + 2]]);

            // Moller-Trumbore
            Vector3 e1 = v1 - v0, e2 = v2 - v0;
            Vector3 p = Vector3.Cross(ray.direction, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-8f) continue;
            float inv = 1f / det;
            Vector3 s = ray.origin - v0;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0f || u > 1f) continue;
            Vector3 qv = Vector3.Cross(s, e1);
            float v = Vector3.Dot(ray.direction, qv) * inv;
            if (v < 0f || u + v > 1f) continue;
            float tt = Vector3.Dot(e2, qv) * inv;
            if (tt > 0f && tt < bestT)
            {
                bestT = tt;
                hit = true;
                Vector3 n = Vector3.Cross(e1, e2).normalized;
                if (Vector3.Dot(n, ray.direction) > 0f) n = -n;
                normal = n;
            }
        }

        if (hit) point = ray.origin + ray.direction * bestT;
        return hit;
    }

    void PlaceDecal(Transform decal)
    {
        if (hitCollider == null) return;

        Vector3 center = hitCollider.bounds.center;
        float radius = hitCollider.bounds.extents.magnitude;
        Camera cam = Camera.main;
        Vector3 dir = cam != null ? (cam.transform.position - center).normalized : Vector3.up;
        dir = (dir + Random.insideUnitSphere * decalSpread).normalized;
        Vector3 origin = center + dir * (radius * 2f);
        Ray ray = new Ray(origin, -dir);
        if (!hitCollider.Raycast(ray, out RaycastHit hit, radius * 4f)) return;
        Vector3 normal = flipDecalFacing ? -hit.normal : hit.normal;
        decal.position = hit.point + hit.normal * decalSurfaceOffset;
        decal.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }
}