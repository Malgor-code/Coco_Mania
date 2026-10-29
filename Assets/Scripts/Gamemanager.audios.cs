using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameManager
{
    public enum PanelAudioId
    {
        Gameplay,
        Recaudacion,
        ArbolMejoras,
        Tienda,
        MaquinaRecompensas,
        Deuda,
        ElegirPerk,
        EntreRondas,
        Indicaciones,
        Pausa,
        MenuPrincipal
    }

    [System.Serializable]
    public class PanelAudioEntry
    {
        public PanelAudioId panel;
        public AudioClip openClip;
        public AudioClip closeClip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [System.Serializable]
    public class CountdownAudio
    {
        public AudioClip tres;
        public AudioClip dos;
        public AudioClip uno;
        public AudioClip ya;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Segundos entre cada numero del conteo")]
        public float intervalo = 1f;
    }

    [Header("Audios - Fuente")]
    [SerializeField] AudioSource sfxSource;

    [Header("Audios - Conteo 3, 2, 1, YA")]
    [SerializeField] CountdownAudio countdownAudio = new CountdownAudio();

    [Header("Audios - Botones (aplican a todos los UIButtonPunch)")]
    [SerializeField] AudioClip buttonHoverClip;
    [SerializeField] AudioClip buttonPressClip;
    [SerializeField] AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] float buttonVolume = 1f;

    [Header("Audios - Paneles (uno por panel)")]
    [SerializeField] List<PanelAudioEntry> panelAudios = new List<PanelAudioEntry>();

    Dictionary<PanelAudioId, PanelAudioEntry> panelAudioLookup;
    Coroutine countdownRoutine;


    void EnsureAudioSetup()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D
        }

        if (panelAudioLookup == null)
        {
            panelAudioLookup = new Dictionary<PanelAudioId, PanelAudioEntry>();
            foreach (var entry in panelAudios)
            {
                if (entry != null) panelAudioLookup[entry.panel] = entry;
            }
        }
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        EnsureAudioSetup();
        sfxSource.PlayOneShot(clip, volume); 
    }

    public void PlayButtonHover() => PlayButtonSfx(buttonHoverClip);
    public void PlayButtonPress() => PlayButtonSfx(buttonPressClip);
    public void PlayButtonClick() => PlayButtonSfx(buttonClickClip);

    void PlayButtonSfx(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSetup();
        sfxSource.PlayOneShot(clip, buttonVolume);
    }

    public void PlayPanelOpen(PanelAudioId id)
    {
        EnsureAudioSetup();
        if (panelAudioLookup.TryGetValue(id, out var e)) PlaySfx(e.openClip, e.volume);
    }

    public void PlayPanelClose(PanelAudioId id)
    {
        EnsureAudioSetup();
        if (panelAudioLookup.TryGetValue(id, out var e)) PlaySfx(e.closeClip, e.volume);
    }

    public void SetPanelActiveWithAudio(GameObject panel, bool active, PanelAudioId id)
    {
        if (panel == null) return;
        bool wasActive = panel.activeSelf;
        panel.SetActive(active);
        if (active && !wasActive) PlayPanelOpen(id);
        else if (!active && wasActive) PlayPanelClose(id);
    }

    public void StartCountdown(System.Action onFinished = null)
    {
        EnsureAudioSetup();
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        countdownRoutine = StartCoroutine(CountdownRoutine(onFinished));
    }

    public void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
    }

    IEnumerator CountdownRoutine(System.Action onFinished)
    {
        float v = countdownAudio.volume;
        float wait = Mathf.Max(0.05f, countdownAudio.intervalo);

        PlaySfx(countdownAudio.tres, v);
        yield return new WaitForSecondsRealtime(wait);

        PlaySfx(countdownAudio.dos, v);
        yield return new WaitForSecondsRealtime(wait);

        PlaySfx(countdownAudio.uno, v);
        yield return new WaitForSecondsRealtime(wait);

        PlaySfx(countdownAudio.ya, v);

        countdownRoutine = null;
        onFinished?.Invoke();
    }
}