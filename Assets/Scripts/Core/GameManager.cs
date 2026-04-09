using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 상태 기계, 점수, 레벨 진행 담당.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── 직렬화 필드 ───────────────────────────────────────────
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;

    [Header("Levels")]
    [SerializeField] private List<LevelData> levels;

    // ── 게임 상태 ─────────────────────────────────────────────
    public enum State { Ready, Playing, Win, Lose }
    public State CurrentState { get; private set; }

    // ── 점수 ─────────────────────────────────────────────────
    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public int CurrentLevel { get; private set; }

    private LevelData _currentData;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BestScore = PlayerPrefs.GetInt("BestScore", 0);
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        LoadLevel(CurrentLevel);
    }

    // ── 레벨 로드 ─────────────────────────────────────────────
    public void LoadLevel(int index)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("레벨 데이터가 없습니다. LevelData ScriptableObject를 할당하세요.");
            return;
        }

        index = Mathf.Clamp(index, 0, levels.Count - 1);
        CurrentLevel = index;
        _currentData = levels[index];

        Score = 0;
        CurrentState = State.Playing;

        uiManager.SetLevelTitle($"LEVEL {index + 1}");
        uiManager.UpdateScore(0);
        uiManager.UpdateStars(0);
        uiManager.HideResultPanel();

        boardManager.OnGroupPopped    -= OnGroupPopped;
        boardManager.OnBoardCleared   -= OnBoardCleared;
        boardManager.OnNoMovesLeft    -= OnNoMovesLeft;

        boardManager.OnGroupPopped    += OnGroupPopped;
        boardManager.OnBoardCleared   += OnBoardCleared;
        boardManager.OnNoMovesLeft    += OnNoMovesLeft;

        boardManager.BuildBoard(_currentData);
    }

    // ── 점수 계산 (SameGame 공식: n² × 10) ───────────────────
    private void OnGroupPopped(int groupSize)
    {
        int gained = groupSize * groupSize * 10;
        Score += gained;

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt("BestScore", BestScore);
        }

        uiManager.UpdateScore(Score);
        uiManager.ShowPopScore(gained);
        uiManager.UpdateStars(CalcStars());
        audioManager?.PlayPop(groupSize);
    }

    // ── 클리어 ────────────────────────────────────────────────
    private void OnBoardCleared()
    {
        // 보너스: 전부 지웠을 때 1000점
        Score += 1000;
        uiManager.UpdateScore(Score);

        FinishLevel(true);
    }

    // ── 더 이상 이동 없음 ─────────────────────────────────────
    private void OnNoMovesLeft()
    {
        bool cleared = boardManager.IsCleared();
        FinishLevel(cleared);
    }

    private void FinishLevel(bool win)
    {
        CurrentState = win ? State.Win : State.Lose;

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt("BestScore", BestScore);
        }

        int stars = CalcStars();
        SaveLevelProgress(CurrentLevel, stars);

        audioManager?.PlayResult(win);
        uiManager.ShowResultPanel(win, Score, stars, HasNextLevel());
    }

    // ── 별점 계산 ─────────────────────────────────────────────
    private int CalcStars()
    {
        if (_currentData == null) return 0;
        if (Score >= _currentData.star3Score) return 3;
        if (Score >= _currentData.star2Score) return 2;
        if (Score > 0)                        return 1;
        return 0;
    }

    // ── 저장 ─────────────────────────────────────────────────
    private void SaveLevelProgress(int level, int stars)
    {
        string key = $"Level_{level}_Stars";
        int prev = PlayerPrefs.GetInt(key, 0);
        if (stars > prev) PlayerPrefs.SetInt(key, stars);

        // 다음 레벨 잠금 해제
        int unlocked = PlayerPrefs.GetInt("UnlockedLevels", 1);
        if (level + 1 >= unlocked)
            PlayerPrefs.SetInt("UnlockedLevels", level + 2);

        PlayerPrefs.Save();
    }

    public int GetLevelStars(int level)
        => PlayerPrefs.GetInt($"Level_{level}_Stars", 0);

    public int GetUnlockedLevels()
        => PlayerPrefs.GetInt("UnlockedLevels", 1);

    // ── 외부 버튼 콜백 ────────────────────────────────────────
    public void OnRetryClicked()
    {
        LoadLevel(CurrentLevel);
    }

    public void OnNextLevelClicked()
    {
        if (HasNextLevel())
            LoadLevel(CurrentLevel + 1);
        else
            uiManager.ShowAllClearScreen(BestScore);
    }

    public void OnMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private bool HasNextLevel()
        => levels != null && CurrentLevel + 1 < levels.Count;

    public int TotalLevels
        => levels != null ? levels.Count : 0;
}
