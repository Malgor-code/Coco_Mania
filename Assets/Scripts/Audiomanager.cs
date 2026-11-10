using System.Collections.Generic;
using UnityEngine;

public enum AudioContext
{
    None,
    MainMenu,
    Hitting,
    Recaudacion,
    UpgradeTree,
    Tienda,
    RewardMachine,
    Deuda,
    PerkChoice,
    BetweenRuns,
    Pause
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    const string MasterKey = "master_volume";

    [System.Serializable]
    public class ContextAudio
    {
        public AudioContext context;

        [Header("Música (loop)")]
        [Tooltip("Música/ambiente en loop de este panel. Vacío = no cambia la música.")]
        public AudioClip music;
        [Tooltip("Sonido corto al entrar a este panel (opcional).")]
        public AudioClip enterSfx;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Segundos que tarda en entrar esta música.")]
        public float fadeIn = 0.8f;
        [Tooltip("Segundos que tarda en salir la música anterior al venir a este panel.")]
        public float fadeOut = 0.6f;

        [Header("Efectos ambientales aleatorios (encima de la música)")]
        [Tooltip("Se reproducen de forma aleatoria sin cortar la música principal.")]
        public List<AudioClip> ambientClips = new List<AudioClip>();
        [Tooltip("Tiempo mínimo entre efectos (segundos).")]
        public float ambientMinInterval = 6f;
        [Tooltip("Tiempo máximo entre efectos (segundos).")]
        public float ambientMaxInterval = 18f;
        [Range(0f, 1f)] public float ambientVolume = 0.7f;
        [Tooltip("Variación aleatoria de tono (0 = sin variación).")]
        [Range(0f, 0.3f)] public float ambientPitchVariation = 0.08f;
        [Tooltip("Posición estéreo aleatoria (0 = siempre centrado, 1 = izquierda/derecha completo).")]
        [Range(0f, 1f)] public float ambientPanSpread = 0.6f;
    }

    [Header("Audio por panel")]
    public List<ContextAudio> contexts = new List<ContextAudio>();

    [Header("Volúmenes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Pausa")]
    [Tooltip("Si el contexto Pause no tiene música, la actual baja a este volumen.")]
    [Range(0f, 1f)] public float pauseDuck = 0.3f;
    public float duckSpeed = 3f;

    AudioSource[] music = new AudioSource[2];
    AudioSource sfx;
    AudioSource ambient;
    int active = 0;

    float[] target = new float[2];  
    float[] speed = new float[2];
    float duck = 1f, duckTarget = 1f;

    AudioContext current = AudioContext.None;
    Dictionary<AudioContext, ContextAudio> map = new Dictionary<AudioContext, ContextAudio>();
    ContextAudio ambientEntry;
    float ambientTimer;
    int lastAmbientIndex = -1;

    public float MasterVolume => masterVolume;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < 2; i++)
        {
            music[i] = gameObject.AddComponent<AudioSource>();
            music[i].loop = true;
            music[i].playOnAwake = false;
            music[i].volume = 0f;
        }
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        ambient = gameObject.AddComponent<AudioSource>();
        ambient.playOnAwake = false;

        masterVolume = PlayerPrefs.GetFloat(MasterKey, masterVolume);

        RebuildMap();
    }

    void RebuildMap()
    {
        map.Clear();
        foreach (var c in contexts)
            if (c != null) map[c.context] = c;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        duck = Mathf.MoveTowards(duck, duckTarget, duckSpeed * dt);

        for (int i = 0; i < 2; i++)
        {
            if (music[i].clip == null) continue;
            float goal = target[i] * musicVolume * masterVolume * duck;
            music[i].volume = Mathf.MoveTowards(music[i].volume, goal, speed[i] * dt);

            if (target[i] <= 0f && music[i].volume <= 0.0001f && music[i].isPlaying)
                music[i].Stop();
        }

        UpdateAmbient(dt);
    }

    void UpdateAmbient(float dt)
    {
        if (ambientEntry == null || ambientEntry.ambientClips == null || ambientEntry.ambientClips.Count == 0) return;
        if (current == AudioContext.Pause) return;

        ambientTimer -= dt;
        if (ambientTimer > 0f) return;

        PlayRandomAmbient(ambientEntry);
        ResetAmbientTimer(ambientEntry);
    }

    void ResetAmbientTimer(ContextAudio e)
    {
        float min = Mathf.Max(0.1f, e.ambientMinInterval);
        float max = Mathf.Max(min, e.ambientMaxInterval);
        ambientTimer = Random.Range(min, max);
    }

    void PlayRandomAmbient(ContextAudio e)
    {
        var clips = e.ambientClips;
        int idx = Random.Range(0, clips.Count);
        if (clips.Count > 1 && idx == lastAmbientIndex)
            idx = (idx + 1 + Random.Range(0, clips.Count - 1)) % clips.Count;
        lastAmbientIndex = idx;

        AudioClip clip = clips[idx];
        if (clip == null) return;

        ambient.pitch = 1f + Random.Range(-e.ambientPitchVariation, e.ambientPitchVariation);
        ambient.panStereo = Random.Range(-e.ambientPanSpread, e.ambientPanSpread);
        ambient.PlayOneShot(clip, e.ambientVolume * sfxVolume * masterVolume);
    }

    public void SetContext(AudioContext ctx)
    {
        if (ctx == current) return;
        current = ctx;

        map.TryGetValue(ctx, out ContextAudio entry);
        if (ctx != AudioContext.Pause)
        {
            ambientEntry = entry;
            if (entry != null && entry.ambientClips != null && entry.ambientClips.Count > 0)
                ResetAmbientTimer(entry);
        }
        if (ctx == AudioContext.Pause && (entry == null || entry.music == null))
        {
            duckTarget = pauseDuck;
            PlayEnterSfx(entry);
            return;
        }
        duckTarget = 1f;

        PlayEnterSfx(entry);
        if (entry == null || entry.music == null) return;
        if (music[active].clip == entry.music && target[active] > 0f)
        {
            target[active] = entry.volume;
            speed[active] = 1f / Mathf.Max(0.01f, entry.fadeIn);
            return;
        }
        int old = active;
        int next = 1 - active;

        target[old] = 0f;
        speed[old] = 1f / Mathf.Max(0.01f, entry.fadeOut);

        music[next].clip = entry.music;
        music[next].volume = 0f;
        music[next].Play();
        target[next] = entry.volume;
        speed[next] = 1f / Mathf.Max(0.01f, entry.fadeIn);

        active = next;
    }

    void PlayEnterSfx(ContextAudio entry)
    {
        if (entry != null && entry.enterSfx != null)
            sfx.PlayOneShot(entry.enterSfx, sfxVolume * masterVolume);
    }
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip != null) sfx.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
    }

    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); }
    public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); }
}