using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC 키로 일시정지를 토글합니다.
/// 싱글플레이(GameScene)에서만 동작 — 멀티플레이 중에는 비활성화됩니다.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject pausePanel;

    [Header("버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;

    private bool       _paused;
    private MatchState _matchState = MatchState.WaitingToStart;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pausePanel?.SetActive(false);
        resumeButton?.onClick.AddListener(Resume);
        menuButton?.onClick.AddListener(GoToMenu);
    }

    void OnEnable()  => EventBus.OnMatchStateChanged += OnMatchState;
    void OnDisable()
    {
        EventBus.OnMatchStateChanged -= OnMatchState;
        // 씬 언로드 시 TimeScale 복원
        if (_paused) Time.timeScale = 1f;
    }

    private void OnMatchState(MatchState s)
    {
        _matchState = s;
        // 매치 종료 시 자동 unpause (ResultsPanel이 올라와야 하므로)
        if (s == MatchState.Ended && _paused)
            Resume();
    }

    // ════════════════════════════════════════════════════════
    void Update()
    {
        // 멀티플레이 중에는 pause 불가
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;

        // 매치가 끝났으면 새로 pause 불가 (unpause는 OnMatchState에서 처리)
        if (_matchState == MatchState.Ended && !_paused)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    // ════════════════════════════════════════════════════════
    public void Toggle() { if (_paused) Resume(); else Pause(); }

    public void Pause()
    {
        _paused        = true;
        Time.timeScale = 0f;
        pausePanel?.SetActive(true);
    }

    public void Resume()
    {
        _paused        = false;
        Time.timeScale = 1f;
        pausePanel?.SetActive(false);
    }

    private void GoToMenu()
    {
        _paused        = false;
        Time.timeScale = 1f;
        EventBus.Clear();
        SceneManager.LoadScene("MenuScene");
    }
}
