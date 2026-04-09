using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// SRP: 매치 타이머와 상태 전환만 담당합니다.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("설정")]
    [SerializeField] private float matchDuration    = 120f;
    [SerializeField] private float countdownSeconds = 3f;

    public MatchState CurrentState { get; private set; } = MatchState.WaitingToStart;
    public float      TimeRemaining { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => StartCoroutine(RunMatch());

    void Update()
    {
        if (CurrentState != MatchState.Playing) return;
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            EndMatch();
        }
    }

    private IEnumerator RunMatch()
    {
        // 카운트다운
        SetState(MatchState.Countdown);
        TimeRemaining = matchDuration;
        yield return new WaitForSeconds(countdownSeconds);

        // 매치 시작
        SetState(MatchState.Playing);
        EventBus.RaiseMatchStarted();
    }

    private void EndMatch()
    {
        SetState(MatchState.Ended);
        var scores = ScoreSystem.Instance?.GetAllScores()
                     ?? new System.Collections.Generic.Dictionary<int, int>();
        EventBus.RaiseMatchEnded(scores);
        AudioManager.Instance?.PlayGameOver();
    }

    private void SetState(MatchState s)
    {
        CurrentState = s;
        EventBus.RaiseMatchStateChanged(s);
    }

    public void RestartMatch()
    {
        EventBus.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        EventBus.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene");
    }

    public string GetFormattedTime()
    {
        int m = Mathf.FloorToInt(TimeRemaining / 60f);
        int s = Mathf.FloorToInt(TimeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", m, s);
    }
}
