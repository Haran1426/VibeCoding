using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 매치 종료 시 전체 플레이어 점수 순위를 표시합니다.
/// </summary>
public class ResultsPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("순위 행 (최대 4개 — Inspector에서 연결)")]
    [SerializeField] private TextMeshProUGUI[] rankTexts;   // "1st  You    12점"

    [Header("최고 점수")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    [Header("버튼")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    // 내 플레이어 ID (싱글/로컬: 0)
    private const int LocalPlayerId = 0;

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

        // 점수 내림차순 정렬
        var ranked = scores
            .OrderByDescending(kv => kv.Value)
            .ToList();

        // 승자 판단
        int winnerId = ranked.Count > 0 ? ranked[0].Key : -1;
        bool iWon    = winnerId == LocalPlayerId;

        if (titleText != null)
            titleText.text = iWon ? "YOU WIN!" : "MATCH END";

        // 순위 행 채우기
        string[] medals = { "1st", "2nd", "3rd", "4th" };
        for (int i = 0; i < (rankTexts?.Length ?? 0); i++)
        {
            if (rankTexts[i] == null) continue;

            if (i < ranked.Count)
            {
                int  pid   = ranked[i].Key;
                int  score = ranked[i].Value;
                bool isMe  = pid == LocalPlayerId;

                string label = isMe ? "You" : $"P{pid + 1}";
                string medal = i < medals.Length ? medals[i] : $"{i + 1}th";
                string marker = isMe ? " ◀" : "";

                rankTexts[i].text = $"{medal}  {label,-6}  {score,3}pt{marker}";
                rankTexts[i].color = isMe
                    ? new Color(0f, 1f, 0.8f)   // 내 항목: 네온 시안
                    : Color.white;
            }
            else
            {
                rankTexts[i].text = "";
            }
        }

        // 최고 점수
        int myScore = scores.TryGetValue(LocalPlayerId, out int ms) ? ms : 0;
        int best    = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScoreText != null)
            bestScoreText.text = $"BEST  {best}pt";
    }
}
