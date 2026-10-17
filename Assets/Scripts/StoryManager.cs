using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryManager : MonoBehaviour
{
    [System.Serializable]
    public class Keyword
    {
        [Tooltip("Expresion regular (sin distinguir mayusculas). Ej: machetes?|gacha")]
        public string pattern;
        public Color color = Color.yellow;
        public Keyword() { }
        public Keyword(string p, Color c) { pattern = p; color = c; }
    }

    public static StoryManager Instance;
    public static bool BlocksPause => Instance != null && Instance.isShowing;

    [Header("UI del dialogo (crear en el Canvas)")]
    [Tooltip("Raiz del dialogo. Debe empezar DESACTIVADO. El script va en OTRO objeto siempre activo.")]
    public GameObject dialoguePanel;
    [Tooltip("Imagen de fondo a pantalla completa (negro). Se oscurece segun el paso. Debe tener Raycast Target.")]
    public Image backdrop;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public Button nextButton;
    public Button skipButton;
    [Tooltip("(Opcional) La cajita del dialogo. Entra con un rebote al aparecer.")]
    public RectTransform dialogueBox;

    [Header("Ajustes")]
    public string speakerName = "El Emprendedor";
    public float charsPerSecond = 45f;
    [Range(0f, 1f)] public float dimStrong = 0.6f;
    [Tooltip("Fondo mas claro para cuando el dialogo explica un panel que debe verse.")]
    [Range(0f, 1f)] public float dimLight = 0.12f;
    [Tooltip("Ultimo pedido de la historia. Al entregarlo sale el 'Gracias por jugar'.")]
    public int finalCycle = 16;
    [Tooltip("false = cada capitulo solo se muestra la primera vez que lo ves (se recuerda entre partidas).")]
    public bool repeatStoryEachRun = false;
    public bool showIntroEveryRun = false;

    [Header("Gamefeel del texto")]
    public bool animateText = true;
    [Tooltip("Ondulacion suave de todas las letras (fraccion del tamano de fuente).")]
    public float waveAmplitude = 0.05f;
    public float waveSpeed = 4f;
    [Tooltip("Las palabras clave ademas tiemblan un poco y laten de color.")]
    public float keywordShake = 0.07f;
    public float keywordShakeSpeed = 22f;
    [Tooltip("Cada letra aparece con un 'pop' (crece y se asienta).")]
    public float popTime = 0.18f;
    public float popOvershoot = 0.6f;
    public float punctuationPause = 0.14f;
    public Color numberColor = new Color(1f, 0.84f, 0.29f);
    public Keyword[] keywords =
    {
        new Keyword("agua de coco|cocos?",                         new Color(0.31f, 0.84f, 0.91f)),
        new Keyword("machetes?|gacha|monedas?",                    new Color(1f, 0.60f, 0.20f)),
        new Keyword("pedidos?|entregar|entrega",                   new Color(1f, 0.84f, 0.29f)),
        new Keyword("árbol de mejoras|mejoras?|scroll|tienda",     new Color(0.55f, 0.92f, 0.45f)),
    };

    [Header("Sonido (opcional)")]
    public AudioSource blipSource;
    public AudioClip blipClip;

    const string IntroKey = "story_intro_done";
    const string TutorialKey = "story_tutorial_done";
    const string ChapterKey = "story_cap_";

    GameManager gm;
    bool isShowing, running, nextPressed, skipRequested, lineDone;
    int lastChapterShown = 0;
    Regex kwRegex;
    float[] charRevealTime = new float[256];

    void Awake()
    {
        Instance = this;
        BuildKeywordRegex();
    }

    void Start()
    {
        gm = GameManager.Instance;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(() => nextPressed = true);
        if (skipButton != null) skipButton.onClick.AddListener(() => skipRequested = true);

        if (gm != null)
        {
            gm.OnRunStarted += HandleRunStarted;
            gm.OnRecaudacionShown += HandleRecaudacion;
        }
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnRunStarted -= HandleRunStarted;
            gm.OnRecaudacionShown -= HandleRecaudacion;
        }
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!isShowing) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            nextPressed = true;
        if (nextButton != null)
        {
            float s = lineDone ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            nextButton.transform.localScale = Vector3.one * s;
        }
    }

    void LateUpdate()
    {
        if (!isShowing || !animateText || bodyText == null || !bodyText.gameObject.activeInHierarchy) return;

        bodyText.ForceMeshUpdate();
        TMP_TextInfo ti = bodyText.textInfo;
        int visible = Mathf.Min(ti.characterCount, bodyText.maxVisibleCharacters);
        float t = Time.unscaledTime;
        float fs = bodyText.fontSize;
        Color32 baseCol = bodyText.color;

        for (int i = 0; i < visible; i++)
        {
            TMP_CharacterInfo ch = ti.characterInfo[i];
            if (!ch.isVisible) continue;

            int mi = ch.materialReferenceIndex;
            int vi = ch.vertexIndex;
            Vector3[] v = ti.meshInfo[mi].vertices;
            Color32[] cols = ti.meshInfo[mi].colors32;
            bool key = Mathf.Abs(ch.color.r - baseCol.r) > 6
                    || Mathf.Abs(ch.color.g - baseCol.g) > 6
                    || Mathf.Abs(ch.color.b - baseCol.b) > 6;

            Vector3 off = new Vector3(0f, Mathf.Sin(t * waveSpeed + i * 0.35f) * waveAmplitude * fs, 0f);
            if (key)
            {
                off += new Vector3(Mathf.Sin(t * keywordShakeSpeed + i * 1.7f),
                                   Mathf.Cos(t * keywordShakeSpeed * 1.3f + i * 2.3f), 0f) * (keywordShake * fs);
            }

            float scale = 1f;
            if (i < charRevealTime.Length)
            {
                float age = t - charRevealTime[i];
                if (age >= 0f && age < popTime) scale = 1f + popOvershoot * (1f - age / popTime);
            }

            Vector3 center = (v[vi] + v[vi + 2]) * 0.5f;
            for (int j = 0; j < 4; j++)
                v[vi + j] = center + (v[vi + j] - center) * scale + off;

            if (key)
            {
                float p = 0.5f + 0.5f * Mathf.Sin(t * 5f + i * 0.4f);
                for (int j = 0; j < 4; j++)
                    cols[vi + j] = Color32.Lerp(cols[vi + j], Color.white, 0.4f * p);
            }
        }

        bodyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
    void HandleRunStarted()
    {
        lastChapterShown = 0;
        if (running) return;
        if (PlayerPrefs.GetInt(IntroKey, 0) == 1 && !showIntroEveryRun) return;
        StartCoroutine(Guard(IntroRoutine()));
    }

    void HandleRecaudacion()
    {
        if (running || gm == null) return;

        int c = gm.billCycle;
        if (c <= lastChapterShown) return;
        if (c > finalCycle + 1) return;

        lastChapterShown = c;

        if (c == 1 && PlayerPrefs.GetInt(TutorialKey, 0) == 0)
        {
            StartCoroutine(Guard(TutorialRoutine()));
            return;
        }

        string key = ChapterKey + c;
        if (!repeatStoryEachRun && PlayerPrefs.GetInt(key, 0) == 1) return;
        StartCoroutine(Guard(ChapterRoutine(c, key)));
    }

    IEnumerator Guard(IEnumerator routine)
    {
        running = true;
        yield return StartCoroutine(routine);
        running = false;
    }
    IEnumerator IntroRoutine()
    {
        yield return null;
        yield return Say(IntroLines, dimStrong, true);
        Cursor.visible = false;
        PlayerPrefs.SetInt(IntroKey, 1);
        PlayerPrefs.Save();
    }
    public void PlayLines(string[] lines, bool strongDim, System.Action onDone)
    {
        StartCoroutine(PlayLinesRoutine(lines, strongDim, onDone));
    }
    IEnumerator PlayLinesRoutine(string[] lines, bool strongDim, System.Action onDone)
    {
        while (running) yield return null;
        running = true;
        yield return Say(lines, strongDim ? dimStrong : dimLight, false);
        running = false;
        onDone?.Invoke();
    }
    IEnumerator TutorialRoutine()
    {
        yield return WaitForFree();
        yield return Say(TutorialOrderLines, dimStrong, false);
        if (gm.deudaPanel != null && !gm.deudaPanel.activeSelf) gm.ToggleDeudaPanel();
        yield return new WaitForSecondsRealtime(0.35f);
        yield return Say(TutorialDeudaLines, dimLight, false);
        while (gm.deudaPanel != null && gm.deudaPanel.activeSelf) yield return null;
        yield return new WaitForSecondsRealtime(0.25f);
        yield return Say(TutorialUpgradeIntroLines, dimStrong, false);
        if (gm.upgradeTreePanel != null && !gm.upgradeTreePanel.activeSelf) gm.ToggleUpgradeTreePanel();
        yield return new WaitForSecondsRealtime(0.4f);
        yield return Say(TutorialTreeLines, dimLight, false);

        PlayerPrefs.SetInt(TutorialKey, 1);
        PlayerPrefs.SetInt(ChapterKey + 1, 1);
        PlayerPrefs.Save();
    }

    IEnumerator ChapterRoutine(int cycle, string key)
    {
        yield return WaitForFree();
        string[] lines = (cycle >= 0 && cycle < Chapters.Length) ? Chapters[cycle] : null;
        if (lines != null) yield return Say(lines, dimStrong, false);
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    IEnumerator WaitForFree()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(0.35f);
        while (CoconutUnlockPanel.BlocksPause || (gm != null && gm.isPaused)) yield return null;
    }
    IEnumerator Say(string[] lines, float dim, bool freezeGame)
    {
        if (lines == null || lines.Length == 0 || dialoguePanel == null || bodyText == null) yield break;

        isShowing = true;
        skipRequested = false;
        nextPressed = false;
        lineDone = false;
        if (freezeGame) Time.timeScale = 0f;
        Cursor.visible = true;

        dialoguePanel.SetActive(true);
        bodyText.text = "";
        if (backdrop != null)
        {
            Color c = backdrop.color;
            c.a = dim;
            backdrop.color = c;
        }
        if (speakerText != null) speakerText.text = speakerName;
        if (dialogueBox != null)
        {
            const float dur = 0.25f;
            float t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < dur)
            {
                dialogueBox.localScale = Vector3.one * EaseOutBack((Time.unscaledTime - t0) / dur);
                yield return null;
            }
            dialogueBox.localScale = Vector3.one;
        }

        foreach (string raw in lines)
        {
            if (skipRequested) break;

            bodyText.text = Fill(raw);
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            TMP_TextInfo ti = bodyText.textInfo;
            int total = ti.characterCount;

            if (charRevealTime.Length < total) charRevealTime = new float[total + 64];
            for (int k = 0; k < charRevealTime.Length; k++) charRevealTime[k] = -999f;

            int shown = 0;
            float nextTime = Time.unscaledTime + 0.05f;
            lineDone = false;
            nextPressed = false;
            while (shown < total && !nextPressed && !skipRequested)
            {
                while (shown < total && Time.unscaledTime >= nextTime)
                {
                    char ch = ti.characterInfo[shown].character;
                    charRevealTime[shown] = Time.unscaledTime;
                    shown++;
                    bodyText.maxVisibleCharacters = shown;
                    PlayBlip(ch, shown);

                    nextTime += 1f / Mathf.Max(1f, charsPerSecond);
                    if (ch == ',' || ch == '.' || ch == '!' || ch == '?' || ch == ':' || ch == ';')
                        nextTime += punctuationPause;
                }
                yield return null;
            }
            bodyText.maxVisibleCharacters = 99999;
            lineDone = true;
            nextPressed = false;
            while (!nextPressed && !skipRequested) yield return null;
        }

        dialoguePanel.SetActive(false);
        isShowing = false;
        lineDone = false;
        if (nextButton != null) nextButton.transform.localScale = Vector3.one;
        if (freezeGame) Time.timeScale = (gm != null && gm.isPaused) ? 0f : 1f;
    }

    void PlayBlip(char c, int count)
    {
        if (blipSource == null || blipClip == null || char.IsWhiteSpace(c) || count % 2 != 0) return;
        blipSource.pitch = Random.Range(0.9f, 1.15f);
        blipSource.PlayOneShot(blipClip, 0.5f);
    }

    static float EaseOutBack(float k)
    {
        k = Mathf.Clamp01(k);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }
    void BuildKeywordRegex()
    {
        kwRegex = null;
        if (keywords == null || keywords.Length == 0) return;

        var sb = new StringBuilder(@"\b(?:");
        for (int i = 0; i < keywords.Length; i++)
        {
            if (keywords[i] == null || string.IsNullOrEmpty(keywords[i].pattern)) continue;
            if (i > 0) sb.Append('|');
            sb.Append("(?<k").Append(i).Append('>').Append(keywords[i].pattern).Append(')');
        }
        sb.Append(@")\b");

        try { kwRegex = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); }
        catch (System.ArgumentException e) { Debug.LogWarning("StoryManager: patron de palabra clave invalido. " + e.Message); }
    }

    string Highlight(string s)
    {
        if (kwRegex == null) return s;
        return kwRegex.Replace(s, m =>
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (m.Groups["k" + i].Success)
                    return "<color=#" + ColorUtility.ToHtmlStringRGB(keywords[i].color) + "><b>" + m.Value + "</b></color>";
            }
            return m.Value;
        });
    }

    string Num(string value)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(numberColor) + "><b>" + value + "</b></color>";
    }

    string Fill(string s)
    {
        s = Highlight(s);
        if (gm == null) return s;

        int days = gm.DaysToPlay;
        return s.Replace("{days} días", Num(days + (days == 1 ? " día" : " días")))
                .Replace("{days}", Num(days.ToString()))
                .Replace("{ml}", Num(GameManager.FormatWater(gm.waterTargetML)))
                .Replace("{cycle}", Num(gm.billCycle.ToString()));
    }

    [ContextMenu("Borrar progreso de historia (para probar)")]
    void ResetStoryProgress()
    {
        PlayerPrefs.DeleteKey(IntroKey);
        PlayerPrefs.DeleteKey(TutorialKey);
        for (int i = 0; i <= 20; i++) PlayerPrefs.DeleteKey(ChapterKey + i);
        PlayerPrefs.Save();
    }

    static readonly string[] IntroLines =
    {
        "Soy un emprendedor del puerto, y si hay algo que disfruto de verdad es destruir cocos.",
        "Cada coco que parto suelta agua fresca, y esa agua es mi negocio.",
        "Tengo un machete oxidado, mucha energía y ningún plan mejor. ¡Ayúdame a romper todos los cocos que pueda antes de quedarme sin aliento!"
    };

    static readonly string[] TutorialOrderLines =
    {
        "¡Primera ronda terminada! Ya hay agua de coco en las jarras.",
        "Pero ojo: tienes que entregar tu pedido.",
        "Mi tía Rosa me pidió {ml} de agua de coco para su cumpleaños, ¡y la fiesta es dentro de {days} días!",
        "Vamos a ver cómo va ese pedido."
    };

    static readonly string[] TutorialDeudaLines =
    {
        "Aquí ves cuánta agua te falta y cuántos días te quedan para juntarla.",
        "Cuando tengas suficiente, el botón de entregar se activa. Cierra este panel cuando estés listo."
    };

    static readonly string[] TutorialUpgradeIntroLines =
    {
        "Con la misma agua también puedes mejorarte: ser más fuerte, más rápido y más listo.",
        "Echemos un vistazo a las mejoras."
    };

    static readonly string[] TutorialTreeLines =
    {
        "Este es el árbol de mejoras. Muévete con el scroll: la rueda del mouse acerca y aleja, y arrastrando con el botón central te desplazas.",
        "Desde aquí también puedes comprar machetes en la tienda y probar suerte en la gacha con las monedas que te dan los cocos.",
        "Pero ojo: todo se paga con la misma agua del pedido, así que elige con cabeza. ¡Ahora sigue tu camino, emprendedor!"
    };

    static readonly string[][] Chapters =
    {
        null,
        new[] { "Pedido {cycle}: mi tía Rosa quiere {ml} para su cumpleaños. ¡Tengo {days} días!" },
        new[] { "¡Tía Rosa quedó feliz! La noticia corrió por toda la familia.",
                "Mi primo Mateo cumple 6 años y quiere una piñata... de coco. Pidió {ml}. Tengo {days} días." },
        new[] { "La fiesta de Mateo fue un éxito: los niños pedían \"otro coco, otro coco\".",
                "Ahora la escuela del barrio organiza una kermés y necesita {ml}. ¡{days} días!" },
        new[] { "Los niños de la kermés salieron saltando de felicidad.",
                "Mi prima Luna se casa en la playa y quiere brindar con agua de coco fresca: {ml} para dentro de {days} días." },
        new[] { "Una boda preciosa, y varios invitados preguntaron quién surte el agua.",
                "Mis abuelos cumplen 50 años de casados y toda la familia llegará al puerto. Piden {ml}. Tengo {days} días." },
        new[] { "Los abuelos lloraron de emoción (y se tomaron como tres litros).",
                "Ahora el parque central hará la Gran Fiesta Infantil, con payasos y 200 niños. Piden {ml} en {days} días." },
        new[] { "200 niños y ni una gota sobrante. Soy un profesional.",
                "El pueblo entero celebra la Fiesta del Puerto y el comité me pide {ml}. ¡Tengo {days} días!" },
        new[] { "Todo el pueblo habla de mi agua de coco.",
                "El alcalde de Villa Marea, el pueblo vecino, quiere {ml} para su feria anual. Plazo: {days} días." },
        new[] { "Villa Marea me nombró \"Cortador Honorario\".",
                "La ciudad de San Cocotal organizará su Festival de Faros y necesita {ml}. Plazo: {days} días." },
        new[] { "Las luces del festival se reflejaban en mi machete.",
                "Ahora la capital quiere {ml} para el concierto del estadio. Ya no es una fiesta: es una multitud. {days} días." },
        new[] { "Cincuenta mil personas y ni una sola con sed. Anotado.",
                "El estado de Costa Dorada va a declarar el Día del Coco y pide {ml}. Tengo {days} días." },
        new[] { "El gobernador me regaló una medalla de latón con forma de coco.",
                "Cinco estados se juntan en el Carnaval Unido y piden {ml} entre todos. ¡{days} días!" },
        new[] { "Los carnavales nunca vieron tanta agua de coco.",
                "Llega un mensajero real desde el Reino de Palmeralia: la Reina quiere {ml} para su banquete. Plazo: {days} días." },
        new[] { "La Reina Palmira probó el agua y dijo: \"¡Exquisita!\".",
                "Pero la República de Cocolandia, su eterna rival, exige {ml} para su Gran Desfile. ¡{days} días!" },
        new[] { "Las dos naciones firmaron la paz... brindando con coco.",
                "Las Mil Islas convocan la Cumbre de las Naciones y piden {ml}. Es el pedido más grande hasta ahora: {days} días." },
        new[] { "Si hasta reyes y presidentes me escriben...",
                "Último pedido: el Festival Mundial de las Fiestas, el cumpleaños de todos los niños de todos los reinos. Piden {ml} en {days} días.",
                "Es el pedido de mi vida. ¡Vamos con todo!" },
        new[] { "¡Lo logré! Entregué el último pedido y el mundo entero brindó con agua de coco.",
                "Desde el puerto, un emprendedor, un machete y muchísimos cocos hicieron una fiesta para todos.",
                "¡GRACIAS POR JUGAR!",
                "El juego sigue en modo infinito, por si quieres romper unos cocos más." }
    };
}