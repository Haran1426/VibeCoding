using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 매치 종료 시 결과를 표시합니다.
/// </summary>
public class ResultsPanel : MonoBehaviour
{
    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private Button          restartButton;
    [SerializeField] private Button          menuButton;

    void Awake()
    {
        panel?.SetActive(false);
        restartButton?.onClick.AddListener(() => MatchManager.Instance?.RestartMatch());
        menuButton?.onClick.AddListener(()    => MatchManager.Instance?.GoToMainMenu());
    }

    void OnEnable()  => EventBus.OnMatchEnded += OnMatchEnded;
    void OnDisable() => EventBus.OnMatchEnded -= OnMatchEnded;

    private void OnMatchEnded(Dictionary<int, int> scores)
    {
        panel?.SetActive(true);
        if (titleText != null) titleText.text = "MATCH END";

        int playerScore = scores.TryGetValue(0, out int s) ? s : 0;
        int best        = PlayerPrefs.GetInt("BestScore", 0);

        if (scoreText    != null) scoreText.text    = "SCORE  " + playerScore.ToString("N0");
        if (bestScoreText != null) bestScoreText.text = "BEST   " + best.ToString("N0");
    }
}
