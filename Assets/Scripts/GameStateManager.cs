using UnityEngine;

public enum GameState
{
    MainMenu,
    OnGame,
    OnPause,
    OnGameOver,  
    OnMinigame
}

[DefaultExecutionOrder(-200)]
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] bool logTransitions = false;

    public GameState Current { get; private set; } = GameState.MainMenu;
    public GameState Previous { get; private set; } = GameState.MainMenu;
    GameState stateBeforePause = GameState.MainMenu;

    public static bool IsPaused => Instance != null && Instance.Current == GameState.OnPause;
    public static bool IsOnGame => Instance != null && Instance.Current == GameState.OnGame;
    public bool CanPause => Current != GameState.MainMenu && Current != GameState.OnPause;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Time.timeScale = 1f; 
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    public static void Request(GameState next) { if (Instance != null) Instance.SetState(next); }
    public static void Force(GameState next) { if (Instance != null) Instance.ForceState(next); }

    public bool SetState(GameState next)
    {
        if (next == GameState.OnPause) { Pause(); return true; }
        if (Current == GameState.OnPause && next != GameState.MainMenu)
        {
            if (next == stateBeforePause) return false;
            if (!IsValidTransition(stateBeforePause, next)) { WarnInvalid(stateBeforePause, next); return false; }
            stateBeforePause = next;
            return true;
        }

        if (next == Current) return false;
        if (!IsValidTransition(Current, next)) { WarnInvalid(Current, next); return false; }
        Apply(next);
        return true;
    }

    public void ForceState(GameState next)
    {
        if (next == Current) return;
        Apply(next);
    }

    public void Pause()
    {
        if (!CanPause) return;
        stateBeforePause = Current;
        Apply(GameState.OnPause);
    }

    public void Resume()
    {
        if (Current != GameState.OnPause) return;
        Apply(stateBeforePause);
    }
    void Apply(GameState next)
    {
        Previous = Current;
        Current = next;
        if (logTransitions) Debug.Log($"[GameState] {Previous} -> {Current}");
        EventBus.Publish(new GameStateChanged(Previous, Current));
    }

    static bool IsValidTransition(GameState from, GameState to)
    {
        switch (from)
        {
            case GameState.MainMenu: return to == GameState.OnGame || to == GameState.OnGameOver;
            case GameState.OnGame: return to == GameState.OnMinigame || to == GameState.OnGameOver || to == GameState.MainMenu;
            case GameState.OnMinigame: return to == GameState.OnGame || to == GameState.MainMenu;
            case GameState.OnGameOver: return to == GameState.OnGame || to == GameState.MainMenu;
            default: return false;
        }
    }

    static void WarnInvalid(GameState from, GameState to)
    {
        Debug.LogWarning($"[GameState] Transicion no permitida: {from} -> {to}");
    }
}