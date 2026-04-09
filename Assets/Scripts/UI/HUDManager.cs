using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("HUD")]
    public Slider healthBar;
    public Slider expBar;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI killText;

    [Header("Level Up Panel")]
    public GameObject levelUpPanel;
    public Button[] abilityButtons;
    public TextMeshProUGUI[] abilityNameTexts;
    public TextMeshProUGUI[] abilityDescTexts;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalKillText;
    public Button restartButton;
    public Button menuButton;

    private System.Collections.Generic.List<AbilityData> currentChoices;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnExpChanged   += UpdateExp;
            GameManager.Instance.OnLevelUp      += _ => { };
            GameManager.Instance.OnGameOver     += ShowGameOver;
        }

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged += UpdateHealth;

        if (AbilityManager.Instance != null)
            AbilityManager.Instance.OnAbilityChoiceReady += ShowLevelUpPanel;

        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());

        InitAbilityButtons();
        UpdateHealth(PlayerStats.Instance != null ? PlayerStats.Instance.currentHealth : 100f,
                     PlayerStats.Instance != null ? PlayerStats.Instance.maxHealth : 100f);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (timerText != null) timerText.text = GameManager.Instance.GetFormattedTime();
        if (killText != null)  killText.text  = "x " + GameManager.Instance.enemiesKilled;
        if (levelText != null) levelText.text = "Lv." + GameManager.Instance.playerLevel;
    }

    void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
    }

    void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }

    void UpdateExp(int current, int max)
    {
        if (expBar != null)
        {
            expBar.maxValue = max;
            expBar.value = current;
        }
    }

    void InitAbilityButtons()
    {
        if (abilityButtons == null) return;
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            int index = i;
            if (abilityButtons[i] != null)
                abilityButtons[i].onClick.AddListener(() => ChooseAbility(index));
        }
    }

    void ShowLevelUpPanel(System.Collections.Generic.List<AbilityData> choices)
    {
        currentChoices = choices;
        if (levelUpPanel != null) levelUpPanel.SetActive(true);

        for (int i = 0; i < abilityButtons.Length; i++)
        {
            bool hasChoice = i < choices.Count;
            if (abilityButtons[i] != null) abilityButtons[i].gameObject.SetActive(hasChoice);
            if (hasChoice)
            {
                if (abilityNameTexts != null && i < abilityNameTexts.Length && abilityNameTexts[i] != null)
                    abilityNameTexts[i].text = choices[i].displayName +
                        (choices[i].currentLevel > 0 ? " Lv." + (choices[i].currentLevel + 1) : "");
                if (abilityDescTexts != null && i < abilityDescTexts.Length && abilityDescTexts[i] != null)
                    abilityDescTexts[i].text = choices[i].description;
            }
        }
    }

    void ChooseAbility(int index)
    {
        if (currentChoices == null || index >= currentChoices.Count) return;
        AbilityManager.Instance?.ApplyAbility(currentChoices[index]);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        if (GameManager.Instance == null) return;
        if (finalTimeText != null)  finalTimeText.text  = GameManager.Instance.GetFormattedTime();
        if (finalScoreText != null) finalScoreText.text = GameManager.Instance.score.ToString("N0");
        if (finalKillText != null)  finalKillText.text  = GameManager.Instance.enemiesKilled.ToString();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnExpChanged   -= UpdateExp;
            GameManager.Instance.OnGameOver     -= ShowGameOver;
        }
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged -= UpdateHealth;
        if (AbilityManager.Instance != null)
            AbilityManager.Instance.OnAbilityChoiceReady -= ShowLevelUpPanel;
    }
}
