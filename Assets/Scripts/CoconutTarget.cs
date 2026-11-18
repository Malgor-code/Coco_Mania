using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CoconutVisuals))]
[RequireComponent(typeof(CoconutJuice))]
[RequireComponent(typeof(CoconutFragments))]
[RequireComponent(typeof(CoconutDecals))]
[RequireComponent(typeof(CoconutAnimation))]
public class CoconutTarget : MonoBehaviour, ICoconutHost
{
    [Header("Golpe")]
    public float stunOnHit = 0.6f;

    [Header("Referencias (opcional, se autodetectan)")]
    public Renderer colorRenderer;
    public Collider hitCollider;

    [Header("Tamaño por tipo")]
    public float baseScale = 1f;
    public float scaleGrowth = 0.08f;
    public float maxScale = 1.5f;

    [Header("FX opcionales")]
    public GameObject hitParticles;
    public GameObject deathParticles;
    [Range(0f, 1f)] public float hitShake = 0.25f;
    [Range(0f, 1f)] public float killShake = 0.5f;
    [Header("Sonido")]
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;
    [Range(0f, 0.3f)] public float pitchVariation = 0.1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Tooltip("0 = se escucha igual sin importar la distancia (recomendado). 1 = 3D total.")]
    [Range(0f, 1f)] public float spatialBlend = 0f;

    bool warnedNoHitSounds;

    [Header("Moneda 3D")]
    public GameObject coinPrefab;
    public float coinLifetime = 300f;
    public float coinPopForce = 3f;

    [Header("Estado")]
    public int hp;
    public int hpMax;
    [HideInInspector] public int lastDamageTaken;
    public CoconutTypeSO Type => currentType;

    const int DefaultBaseHp = 10;
    const float DestroyDelay = 0.05f;
    const float CoinSpawnLift = 0.5f;
    const float CoinTorque = 2f;
    const float CoinDirSpread = 0.5f;
    const float ExecuteTextOffset = 0.9f;

    CoconutVisuals visuals;
    CoconutJuice juice;
    CoconutFragments fragments;
    CoconutDecals decals;
    CoconutAnimation anim;
    CoconutWander wander;
    AudioSource audioSource;
    Renderer rend;
    CoconutTypeSO currentType;
    float lootMultiplier = 1f;
    ICoconutPassive passive = NullPassive.Instance;

    Vector3 originalScale, typedScale;
    Vector3 lastHitFrom;
    bool hitFromExplicit;
    bool isDead;

    float HpPercent => hpMax > 0 ? (float)hp / hpMax : 1f;
    float SizeRatio => originalScale.x > 0.0001f ? typedScale.x / originalScale.x : 1f;

    int ICoconutHost.Hp => hp;
    int ICoconutHost.HpMax => hpMax;
    Vector3 ICoconutHost.Position => transform.position;
    public void Heal(int amount) => hp = Mathf.Min(hpMax, hp + amount);
    public void Flash(Color color, float duration) => visuals.Flash(color, duration);
    public void SetDarken(float t) => visuals.SetDarken(t);
    public void SetAlpha(float a) => visuals.SetAlpha(a);
    public void FloatText(string text, float yOffset, bool emphasized = true)
    {
        if (JuiceManager.Instance != null)
            JuiceManager.Instance.SpawnDamageNumber(transform.position + Vector3.up * yOffset, text, emphasized);
    }

    T Require<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    void Awake()
    {
        visuals = Require<CoconutVisuals>();
        juice = Require<CoconutJuice>();
        fragments = Require<CoconutFragments>();
        decals = Require<CoconutDecals>();
        anim = Require<CoconutAnimation>();
        wander = GetComponent<CoconutWander>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;

        fragments.Init();
        originalScale = typedScale = transform.localScale;
        hitCollider = hitCollider != null ? hitCollider : FindHitCollider();
        rend = colorRenderer != null ? colorRenderer : FindBodyRenderer();

        visuals.Init(rend);
        juice.Init(rend);
        decals.Init(rend);
        anim.Init();
        decals.ResetDecals();
    }

    Collider FindHitCollider()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
            if (c.GetComponentInParent<FragmentFader>() == null) return c;
        return null;
    }

    Renderer FindBodyRenderer()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (r.GetComponentInParent<FragmentFader>() != null) continue;
            return r;
        }
        return null;
    }

    void Update()
    {
        if (!isDead) passive.Tick(GameTime.DeltaTime);
    }

    public void ApplyType(CoconutTypeSO type, float hpMultiplier, float lootMult, float hpScale = 1f)
    {
        currentType = type;
        lootMultiplier = lootMult;
        isDead = false;
        lastDamageTaken = 0;
        hitFromExplicit = false;

        passive = (type != null && type.passive != null) ? type.passive.Create(this) : NullPassive.Instance;

        int baseHp = CombatManager.Instance != null ? CombatManager.Instance.coconutHpMax : DefaultBaseHp;
        hpMax = Mathf.Max(1, Mathf.RoundToInt(baseHp * hpMultiplier * hpScale));
        hp = hpMax;

        float globalSize = CoconutSpawner.Instance != null ? CoconutSpawner.Instance.globalSizeMultiplier : 1f;
        float growth = Mathf.Sqrt(Mathf.Max(0f, hpMultiplier - 1f)) * scaleGrowth;
        typedScale = originalScale * (Mathf.Min(baseScale + growth, maxScale) * globalSize);
        if (hitCollider != null) hitCollider.enabled = true;
        if (wander != null) wander.enabled = true;

        decals.ResetDecals();
        visuals.SetBaseScale(typedScale);
        visuals.ResetState();

        float stun = anim.PlaySpawn();
        if (stun > 0f && wander != null) wander.Stun(stun);
    }

    public bool TakeDamage(int amount, bool isCritical, Vector3 hitFrom)
    {
        lastHitFrom = hitFrom;
        hitFromExplicit = true;
        bool killed = TakeDamage(amount, isCritical);
        hitFromExplicit = false;
        return killed;
    }

    public bool TakeDamage(int amount, bool isCritical = false)
    {
        if (isDead) return false;
        if (!hitFromExplicit) lastHitFrom = DefaultHitOrigin();

        float executeMult = CombatManager.Instance.GetExecuteDamageMultiplier(HpPercent);
        float modified = passive.ModifyDamage(amount, isCritical) * executeMult;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(modified));
        lastDamageTaken = finalDamage;
        hp -= finalDamage;

        if (executeMult > 1f) FloatText("¡REMATE!", ExecuteTextOffset);
        passive.OnHit();
        if (wander != null) wander.Stun(stunOnHit);

        bool killed = hp <= 0;
        PlayHitEffects(finalDamage, killed);
        if (killed) Die();
        return killed;
    }

    Vector3 DefaultHitOrigin() =>
        MacheteController.Instance != null ? MacheteController.Instance.transform.position
                                           : transform.position - transform.forward;

    void PlayHitEffects(int damage, bool killed)
    {
        if (hitParticles != null) SpawnFx(hitParticles, transform.position);
        PlaySound(hitSounds, killed);
        juice.BurstHit();
        anim.PlayHit();
        decals.ShowNext();
        FloatText("-" + damage, 0.5f, false);
        if (!killed) visuals.PlayHit(HpPercent);
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(killed ? killShake : hitShake);
    }

    void Die()
    {
        isDead = true;
        visuals.SetDead();
        if (hitCollider != null) hitCollider.enabled = false;
        if (wander != null) wander.enabled = false;

        if (fragments.Break(lastHitFrom, SizeRatio)) visuals.HideRenderer();

        GameObject deathFx = deathParticles != null ? Instantiate(deathParticles, transform.position, Quaternion.identity) : null;
        PlayDeathSound();
        passive.OnDeath();
        juice.BurstDeath();

        GrantRewards(deathFx);

        CoconutSpawner spawner = CoconutSpawner.Instance;
        if (spawner != null)
        {
            spawner.RemoveCoconut(this);
            spawner.ReleaseDelayed(this, DestroyDelay); 
        }
        else
        {
            Destroy(gameObject, DestroyDelay);
        }
    }

    void GrantRewards(GameObject deathFx)
    {
        CombatManager combat = CombatManager.Instance;
        combat.OnCoconutDestroyed(lootMultiplier, out float waterGained);
        combat.ApplyChainSplash(transform.position, lastDamageTaken, gameObject);

        bool foundCoin = combat.lastKillFoundCoin;
        if (foundCoin) SpawnCoin();

        bool jarTookFx = WaterJarGroup.Instance != null
            && WaterJarGroup.Instance.HandleCoconutWater(transform.position, waterGained, deathFx);

        if (JuiceManager.Instance != null)
            JuiceManager.Instance.SpawnKillRewards(transform.position + Vector3.up * CoinSpawnLift, waterGained, foundCoin);

        if (!jarTookFx && deathFx != null) AutoDestroyFx(deathFx);
    }

    void SpawnCoin()
    {
        if (coinPrefab == null) return;
        GameObject coin = Instantiate(coinPrefab, transform.position + Vector3.up * CoinSpawnLift, Quaternion.identity);

        if (coin.GetComponentInChildren<Collider>() == null) coin.AddComponent<SphereCollider>();
        Rigidbody rb = coin.GetComponent<Rigidbody>();
        if (rb == null) rb = coin.AddComponent<Rigidbody>();

        Vector3 dir = (Random.insideUnitSphere * CoinDirSpread + Vector3.up).normalized;
        rb.AddForce(dir * coinPopForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * CoinTorque, ForceMode.Impulse);
        Destroy(coin, coinLifetime);
    }

    void PlaySound(AudioClip[] clips, bool detached = false)
    {
        if (clips == null || clips.Length == 0)
        {
            if (!warnedNoHitSounds)
            {
                warnedNoHitSounds = true;
                Debug.LogWarning("[CoconutTarget] 'Hit Sounds' está vacío en " + name + ". Arrastra clips en el Inspector.", this);
            }
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        if (detached || audioSource == null) { PlayDetached(clip, pitch); return; }

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    void PlayDetached(AudioClip clip, float pitch)
    {
        var go = new GameObject("CocoSfx");
        go.transform.position = transform.position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = sfxVolume;
        src.pitch = pitch;
        src.spatialBlend = spatialBlend;
        src.playOnAwake = false;
        if (audioSource != null) src.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        src.Play();

        Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
    }

    void PlayDeathSound() => PlaySound(deathSounds, true);

    static void SpawnFx(GameObject prefab, Vector3 pos) =>
        AutoDestroyFx(Instantiate(prefab, pos, Quaternion.identity));

    static void AutoDestroyFx(GameObject fx)
    {
        if (fx == null) return;
        float max = 0f;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
            max = Mathf.Max(max, ps.main.duration + ps.main.startLifetimeMultiplier);
        Destroy(fx, max > 0f ? Mathf.Min(max + 0.5f, 10f) : 3f);
    }

    public Vector3 HitPoint => hitCollider != null ? hitCollider.bounds.center : transform.position;

    public Vector3 GetClosestHitPoint(Vector3 from)
    {
        if (hitCollider == null) return transform.position;
        var probe = new Vector3(from.x, hitCollider.bounds.center.y, from.z);
        return hitCollider.ClosestPoint(probe);
    }
}