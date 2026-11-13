using UnityEngine;

public class CoconutAnimation : MonoBehaviour
{
    [Tooltip("Animator que se usa al golpear/aparecer. Vacío = el primer Animator hijo.")]
    public Animator hitAnimator;

    [Header("Golpe")]
    public string hitStateName = "Hit";
    [Tooltip("Nombre del CLIP (no del estado). Vacío = igual que hitStateName.")]
    public string hitClipName = "";
    [Range(0f, 0.9f)] public float hitStartNormalized = 0f;
    [Tooltip("Parámetro float usado como Speed Multiplier del estado Hit.")]
    public string hitSpeedParameter = "";
    public float maxHitAnimSpeed = 4f;

    [Header("Aparición")]
    public string spawnStateName = "";
    [Tooltip("Segundos que el coco queda quieto mientras dura la animación de aparición.")]
    public float spawnStunDuration = 0f;

    const float MinSwingWindow = 0.1f;
    const float SwingWindowFactor = 0.9f;
    const float DesyncSpeedMin = 0.9f, DesyncSpeedMax = 1.1f;

    Animator ambient;
    int hitHash, spawnHash;
    bool warnedHit, warnedSpawn;
    float hitClipLength = -1f;

    public void Init()
    {
        ambient = GetComponentInChildren<Animator>();
        if (hitAnimator == null) hitAnimator = ambient;
        Desync();
    }

    void Desync()
    {
        if (ambient == null) return;
        var state = ambient.GetCurrentAnimatorStateInfo(0);
        ambient.Play(state.fullPathHash, 0, Random.value);
        if (ambient != hitAnimator) ambient.speed = Random.Range(DesyncSpeedMin, DesyncSpeedMax);
    }

    /// Devuelve cuánto debe quedar quieto el coco (0 si nada).
    public float PlaySpawn()
    {
        if (!TryPlay(spawnStateName, ref spawnHash, ref warnedSpawn, 0f)) return 0f;
        return spawnStunDuration;
    }

    public void PlayHit()
    {
        if (hitAnimator == null || string.IsNullOrEmpty(hitStateName)) return;
        ApplyHitSpeed();
        TryPlay(hitStateName, ref hitHash, ref warnedHit, hitStartNormalized);
    }

    bool TryPlay(string stateName, ref int hash, ref bool warned, float normalizedTime)
    {
        if (hitAnimator == null || string.IsNullOrEmpty(stateName)) return false;
        if (hash == 0) hash = Animator.StringToHash(stateName);

        if (!hitAnimator.HasState(0, hash))
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[CoconutAnimation] El Animator no tiene el estado '" + stateName + "'.", this);
            }
            return false;
        }

        hitAnimator.Play(hash, 0, normalizedTime);
        hitAnimator.Update(0f);
        return true;
    }

    void ApplyHitSpeed()
    {
        if (string.IsNullOrEmpty(hitSpeedParameter)) return;

        float speed = 1f;
        if (MacheteController.Instance != null)
        {
            if (hitClipLength < 0f) hitClipLength = ResolveClipLength();
            float window = Mathf.Max(MinSwingWindow, MacheteController.Instance.swingInterval * SwingWindowFactor);
            if (hitClipLength > window) speed = Mathf.Min(hitClipLength / window, maxHitAnimSpeed);
        }
        hitAnimator.SetFloat(hitSpeedParameter, speed);
    }

    float ResolveClipLength()
    {
        if (hitAnimator.runtimeAnimatorController == null) return 0f;
        string clipName = string.IsNullOrEmpty(hitClipName) ? hitStateName : hitClipName;
        foreach (var clip in hitAnimator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == clipName) return clip.length;
        return 0f;
    }
}