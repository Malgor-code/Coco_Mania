using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [Header("Frecuencia")]
    [Tooltip("Cada cuantos dias jugados sale un evento.")]
    public int everyNDays = 5;
    public bool eventsEnabled = true;
    bool UseStory => StoryManager.Instance != null && StoryManager.Instance.dialoguePanel != null;
    [Header("Recompensa (% del pedido actual)")]
    [Range(0f, 1f)] public float minRewardPercent = 0.05f;
    [Range(0f, 1f)] public float maxRewardPercent = 0.30f;
    [Tooltip("Monedas extra al sacar 3 estrellas.")]
    public int coinsOnThreeStars = 1;

    [Header("Dificultad infinita")]
    public float difficultyPerEvent = 0.12f;
    public float difficultyPerRound = 0.35f;
    public float difficultyCap = 3.5f;

    enum State { None, Intro, Playing, Result }
    State state = State.None;

    readonly List<MinigameBase> pool = new List<MinigameBase>();
    readonly List<int> bag = new List<int>();
    int eventCount = 0;
    int round = 0;
    int lastEventDay = -1;
    Action onFinished;
    MinigameBase current;
    float currentDifficulty = 1f;
    float rewardMl;

    // UI
    static readonly Color DimColor = new Color(0f, 0f, 0f, 0.8f);
    static readonly Color AreaColor = new Color(0.12f, 0.2f, 0.13f, 1f);
    Canvas canvas;
    GameObject dim, card, gameArea, hudRoot;
    Image dimImg, areaImg;
    RectTransform gameRoot;
    TMP_Text cardTitle, cardBody, cardExtra, hudTitle, hudInfo;
    Button cardButton;
    TMP_Text cardButtonLabel;

    static readonly string[] Titles =
    {
        "¡INSPECCIÓN SORPRESA DEL PUERTO!",
        "¡LA TÍA ORGANIZÓ UN CONCURSO!",
        "¡EL SINDICATO DE COCOS EXIGE UN DESCANSO!",
        "¡FERIA DEL PUEBLO!",
        "¡APUESTA DEL TÍO ROBERTO!",
        "¡EVENTO PATROCINADO POR NADIE!",
        "¡DÍA DE LA COCONUT-MANÍA!",
    };

    static readonly string[] ZeroStars =
    {
        "Eso fue... valiente. Los cocos se rieron de ti.",
        "Hasta la tía lo habría hecho mejor, y tiene los ojos cerrados.",
    };
    static readonly string[] OneStar =
    {
        "Meh. Algo es algo, ¿no?",
        "Los cocos te perdonan la vida. Por ahora.",
    };
    static readonly string[] TwoStars =
    {
        "¡Nada mal, machetero! El puerto está impresionado.",
        "Eso estuvo bastante bien. Los cocos ya tienen miedo.",
    };
    static readonly string[] ThreeStars =
    {
        "¡PERFECTO! Los cocos piden tu autógrafo.",
        "¡LEYENDA! El puerto entero está aplaudiendo.",
    };

    void Awake()
    {
        Instance = this;
        BuildPool();
    }

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnRunReset += ResetRun;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnRunReset -= ResetRun;
    }

    void BuildPool()
    {
        pool.Clear();
        pool.Add(new Minigame_Topo());
        pool.Add(new Minigame_Bolos());
        pool.Add(new Minigame_Atrapa());
        pool.Add(new Minigame_Machetazo());
    }

    void ResetRun()
    {
        eventCount = 0;
        round = 0;
        lastEventDay = -1;
        bag.Clear();
    }
    public bool ShouldTrigger(int daysPlayed)
    {
        if (!eventsEnabled || everyNDays <= 0 || state != State.None) return false;
        return daysPlayed > 0 && daysPlayed % everyNDays == 0 && daysPlayed != lastEventDay;
    }

    public void StartEvent(Action finishedCallback)
    {
        if (canvas == null) BuildUI();
        onFinished = finishedCallback;
        lastEventDay = GameManager.Instance != null ? GameManager.Instance.runDayCount : -1;
        eventCount++;

        current = PickNext();
        currentDifficulty = Mathf.Min(difficultyCap,
            1f + difficultyPerEvent * (eventCount - 1) + difficultyPerRound * round);

        canvas.gameObject.SetActive(true);
        ShowIntro();
    }

    MinigameBase PickNext()
    {
        if (bag.Count == 0)
        {
            if (eventCount > 1) round++;
            for (int i = 0; i < pool.Count; i++) bag.Add(i);
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }
        int idx = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        return pool[idx];
    }
    void SetBackdrop(bool transparent)
    {
        if (dimImg != null) dimImg.color = transparent ? Color.clear : DimColor;
        if (areaImg != null) areaImg.color = transparent ? Color.clear : AreaColor;
    }

    void ShowIntro()
    {
        state = State.Intro;
        SetBackdrop(false);
        gameArea.SetActive(false);
        hudRoot.SetActive(false);

        string title = Titles[UnityEngine.Random.Range(0, Titles.Length)];
        string intro = current.Intros[UnityEngine.Random.Range(0, current.Intros.Length)];

        if (UseStory)
        {
            dim.SetActive(false);
            StoryManager.Instance.PlayLines(new[]
            {
                "¡Evento " + eventCount + "! " + title,
                intro,
                "Se juega así: " + current.Controls + " (Dificultad " + currentDifficulty.ToString("0.0") + ")"
            }, true, StartPlaying);
            return;
        }

        dim.SetActive(true);
        card.SetActive(true);
        cardTitle.text = title + "\n<size=60%><color=#FFD84A>" + current.Name + "</color>  (evento #" + eventCount + ")</size>";
        cardBody.text = intro;
        cardExtra.text = "<color=#9CE37D>Cómo se juega:</color> " + current.Controls
                         + "\n<size=70%>Dificultad: " + currentDifficulty.ToString("0.0") + "</size>";
        cardButtonLabel.text = "¡A jugar!";
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(StartPlaying);
    }

    void StartPlaying()
    {
        state = State.Playing;
        dim.SetActive(true);
        SetBackdrop(current.Is3D);
        card.SetActive(false);
        gameArea.SetActive(true);
        hudRoot.SetActive(true);
        hudTitle.text = current.Name;
        current.Begin(gameRoot, currentDifficulty);
    }

    void ShowResult()
    {
        state = State.Result;
        float score = Mathf.Clamp01(current.Score01);
        current.Cleanup();
        SetBackdrop(false);

        int stars = score >= 0.85f ? 3 : score >= 0.55f ? 2 : score >= 0.25f ? 1 : 0;
        string[] jokes = stars == 3 ? ThreeStars : stars == 2 ? TwoStars : stars == 1 ? OneStar : ZeroStars;
        string joke = jokes[UnityEngine.Random.Range(0, jokes.Length)];

        float order = GameManager.Instance != null ? GameManager.Instance.waterTargetML : 250f;
        rewardMl = order * Mathf.Lerp(minRewardPercent, maxRewardPercent, score);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddWater(rewardMl);
            if (stars == 3) GameManager.Instance.coconutCoins += coinsOnThreeStars;
        }

        string reward = "Recompensa: <color=#7DD3FF><b>+" + GameManager.FormatWater(rewardMl) + "</b></color>"
                        + (stars == 3 && coinsOnThreeStars > 0 ? " y <color=#FFD84A><b>+" + coinsOnThreeStars + " moneda</b></color>" : "");

        gameArea.SetActive(false);
        hudRoot.SetActive(false);

        if (UseStory)
        {
            dim.SetActive(false);
            StoryManager.Instance.PlayLines(new[]
            {
                joke + " (" + stars + "/3 estrellas, " + Mathf.RoundToInt(score * 100f) + "%)",
                reward
            }, true, Close);
            return;
        }

        card.SetActive(true);
        cardTitle.text = "<color=#FFD84A>" + stars + "/3</color>\n<size=60%>" + Mathf.RoundToInt(score * 100f) + "%</size>";
        cardBody.text = joke;
        cardExtra.text = reward;
        cardButtonLabel.text = "Continuar";
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (state != State.Playing || current == null) return;

        current.Update(Time.deltaTime);

        string timer = current.Duration > 0f ? "Tiempo: " + Mathf.CeilToInt(current.TimeLeft) + "s     " : "";
        hudInfo.text = timer + current.Hud;

        if (current.Finished) ShowResult();
    }

    void Close()
    {
        state = State.None;
        canvas.gameObject.SetActive(false);
        var cb = onFinished;
        onFinished = null;
        cb?.Invoke();
    }

    void BuildUI()
    {
        var go = new GameObject("EventCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        dimImg = MiniUI.Fill("Dim", go.transform, DimColor);
        dim = dimImg.gameObject;
        dimImg.raycastTarget = true;
        Image cardImg = MiniUI.Img("Card", dim.transform, new Color(0.13f, 0.1f, 0.07f, 0.97f), Vector2.zero, new Vector2(1200, 700));
        card = cardImg.gameObject;
        cardTitle = MiniUI.Label(card.transform, "", 56, new Vector2(0, 230), new Vector2(1100, 200));
        cardTitle.fontStyle = FontStyles.Bold;
        cardBody = MiniUI.Label(card.transform, "", 40, new Vector2(0, 40), new Vector2(1050, 240));
        cardExtra = MiniUI.Label(card.transform, "", 34, new Vector2(0, -150), new Vector2(1050, 130), new Color(0.9f, 0.9f, 0.85f));
        cardButton = MiniUI.Btn(card.transform, "¡A jugar!", new Vector2(0, -280), new Vector2(380, 90), new Color(0.2f, 0.6f, 0.25f), null);
        cardButtonLabel = cardButton.GetComponentInChildren<TMP_Text>();
        areaImg = MiniUI.Img("GameArea", dim.transform, AreaColor, Vector2.zero, new Vector2(1400, 720));
        gameArea = areaImg.gameObject;
        gameRoot = areaImg.rectTransform;
        var mask = gameArea.AddComponent<RectMask2D>();
        hudRoot = MiniUI.Img("Hud", dim.transform, Color.clear, new Vector2(0, 430), new Vector2(1400, 120)).gameObject;
        hudTitle = MiniUI.Label(hudRoot.transform, "", 52, new Vector2(0, 25), new Vector2(1400, 70));
        hudTitle.fontStyle = FontStyles.Bold;
        hudInfo = MiniUI.Label(hudRoot.transform, "", 36, new Vector2(0, -30), new Vector2(1400, 60), new Color(1f, 0.95f, 0.6f));

        canvas.gameObject.SetActive(false);
    }
}