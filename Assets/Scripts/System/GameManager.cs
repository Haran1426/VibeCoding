using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, LevelUp, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("Game Stats")]
    public float survivalTime = 0f;
    public int score = 0;
    public int enemiesKilled = 0;
    public int playerLevel = 1;
    public int playerExp = 0;
    public int expToNextLevel = 10;

    public System.Action<int> OnScoreChanged;
    public System.Action<int, int> OnExpChanged;
    public System.Action<int> OnLevelUp;
    public System.Action OnGameOver;
    public System.Action OnGameStart;

    private bool isGameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        OnGameStart?.Invoke();
    }

    void Update()
    {
        if (CurrentState == GameState.Playing)
            survivalTime += Time.deltaTime;
    }

    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    public void AddExp(int amount)
    {
        playerExp += amount;
        while (playerExp >= expToNextLevel)
        {
            playerExp -= expToNextLevel;
            playerLevel++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.3f);
            CurrentState = GameState.LevelUp;
            Time.timeScale = 0f;
            OnLevelUp?.Invoke(playerLevel);
        }
        OnExpChanged?.Invoke(playerExp, expToNextLevel);
    }

    public void ResumeFromLevelUp()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene");
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}

