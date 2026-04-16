using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    // 싱글=0, 멀티=OwnerClientId (OnMatchEnded 시점에 결정)
    private int _localPlayerId;

    void Awake()
    {
        panel?.SetActive(false);
        // [버그2 픽스] 로컬/네트워크 모드 분기
        restartButton?.onClick.AddListener(OnRestartClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);

        var netSync = FindFirstObjectByType<PlayerNetworkSync>();
        if (netSync != null && netSync.IsOwner)
            _localPlayerId = (int)netSync.OwnerClientId;
        else
            _localPlayerId = 0;
    }

    private void OnRestartClicked()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.RestartMatch();
        }
        else
        {
            // 네트워크 모드: 연결 종료 후 씬 재로드
            NetworkManager.Singleton?.Shutdown();
            EventBus.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnMenuClicked()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.GoToMainMenu();
        }
        else
        {
            // 네트워크 모드: 연결 종료 후 메뉴로
            NetworkManager.Singleton?.Shutdown();
            EventBus.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
        }
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
        bool iWon    = winnerId == _localPlayerId;

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
                bool isMe  = pid == _localPlayerId;

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

        // 최고 점수 — 갱신 후 저장
        int myScore = scores.TryGetValue(_localPlayerId, out int ms) ? ms : 0;
        int best    = PlayerPrefs.GetInt("BestScore", 0);
        if (myScore > best)
        {
            best = myScore;
            PlayerPrefs.SetInt("BestScore", best);
            PlayerPrefs.Save();
        }
        if (bestScoreText != null)
            bestScoreText.text = $"BEST  {best}pt";
    }
}
