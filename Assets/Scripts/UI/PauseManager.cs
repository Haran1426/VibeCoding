using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC menu for both solo scenes and online matches.
/// Online matches keep running, so this acts as a leave/resume overlay instead of freezing time.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    [SerializeField] private TMPro.TextMeshProUGUI subtitleText;

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

        EnsureRuntimePauseUI();

        pausePanel?.SetActive(false);
        resumeButton?.onClick.AddListener(Resume);
        menuButton?.onClick.AddListener(GoToMenu);
    }

    private void EnsureRuntimePauseUI()
    {
        if (pausePanel != null && resumeButton != null && menuButton != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = RuntimeUIFactory.EnsureCanvas();

        if (pausePanel == null)
        {
            pausePanel = RuntimeUIFactory.CreatePanel(canvas.transform, "PausePanel_Runtime",
                new Color(0.01f, 0.015f, 0.025f, 0.86f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            titleText = RuntimeUIFactory.CreateText(pausePanel.transform, "PauseTitle_Runtime", "PAUSED", 62,
                TMPro.TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(360f, 80f), new Color(0f, 0.9f, 1f));

            subtitleText = RuntimeUIFactory.CreateText(pausePanel.transform, "PauseSubtitle_Runtime", "", 24,
                TMPro.TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 68f), new Vector2(620f, 42f), new Color(0.72f, 0.83f, 0.92f));
        }

        if (resumeButton == null)
            resumeButton = RuntimeUIFactory.CreateButton(pausePanel.transform, "ResumeButton_Runtime", "RESUME",
                new Vector2(0f, 20f), new Vector2(260f, 58f));

        if (menuButton == null)
            menuButton = RuntimeUIFactory.CreateButton(pausePanel.transform, "MenuButton_Runtime", "LEAVE MATCH",
                new Vector2(0f, -58f), new Vector2(260f, 58f));
    }

    void OnEnable()  => EventBus.OnMatchStateChanged += OnMatchState;
    void OnDisable()
    {
        EventBus.OnMatchStateChanged -= OnMatchState;
        // 씬 언로드 시 TimeScale 복원
        if (_paused) Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        resumeButton?.onClick.RemoveListener(Resume);
        menuButton?.onClick.RemoveListener(GoToMenu);

        if (_paused)
            Time.timeScale = 1f;

        if (Instance == this)
            Instance = null;
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
        _paused = true;
        bool networkMatch = IsNetworkMatch();
        if (!networkMatch)
            Time.timeScale = 0f;

        if (titleText != null)
            titleText.text = networkMatch ? "MATCH MENU" : "PAUSED";
        if (subtitleText != null)
            subtitleText.text = networkMatch
                ? "Online match keeps running while this menu is open."
                : "Game is paused.";

        pausePanel?.SetActive(true);
    }

    public void Resume()
    {
        _paused = false;
        if (!IsNetworkMatch())
            Time.timeScale = 1f;
        pausePanel?.SetActive(false);
    }

    private void GoToMenu()
    {
        _paused        = false;
        Time.timeScale = 1f;
        NetworkManager.Singleton?.Shutdown();
        EventBus.Clear();
        SceneManager.LoadScene("MenuScene");
    }

    private static bool IsNetworkMatch() =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
}
