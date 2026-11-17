using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] GameObject pauseMenuPanel;

    void OnEnable() => EventBus.Subscribe<GameStateChanged>(OnStateChanged);
    void OnDisable() => EventBus.Unsubscribe<GameStateChanged>(OnStateChanged);

    void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (!GameInput.PauseDown) return;
        if (CoconutUnlockPanel.BlocksPause || StoryManager.BlocksPause) return;

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null) return;

        if (gsm.Current == GameState.OnPause) gsm.Resume();
        else gsm.Pause();
    }

    void OnStateChanged(GameStateChanged e)
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(e.Current == GameState.OnPause);
    }
}