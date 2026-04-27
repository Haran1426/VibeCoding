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
    [SerializeField] private TextMeshProUGUI[] rankTexts;

    [Header("최고 점수")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    [Header("버튼")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private int _localPlayerId;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        panel?.SetActive(false);
        restartButton?.onClick.AddListener(OnRestartClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);
    }

    void Start()
    {
        // NetworkSpawn 이 완료된 Start 시점에 localPlayerId 결정
        // Awake 에서 체크하면 IsOwner 가 항상 false 를 반환함
        var netSync = FindFirstObjectByType<PlayerNetworkSync>();
        if (netSync != null && netSync.IsSpawned && netSync.IsOwner)
            _localPlayerId = (int)netSync.OwnerClientId;
        else
            _localPlayerId = 0;
    }

    void OnEnable()  => EventBus.OnMatchEnded += OnMatchEnded;
    void OnDisable() => EventBus.OnMatchEnded -= OnMatchEnded;

    // ════════════════════════════════════════════════════════
    private void OnMatchEnded(Dictionary<int, int> scores)
    {
        panel?.SetActive(true);

        var ranked = scores
            .OrderByDescending(kv => kv.Value)
            .ToList();

        int  winnerId = ranked.Count > 0 ? ranked[0].Key : -1;
        bool iWon     = winnerId == _localPlayerId;

        if (titleText != null)
            titleText.text = iWon ? "YOU WIN!" : "MATCH END";

        string[] medals = { "1st", "2nd", "3rd", "4th" };
        for (int i = 0; i < (rankTexts?.Length ?? 0); i++)
        {
            if (rankTexts[i] == null) continue;

            if (i < ranked.Count)
            {
                int    pid    = ranked[i].Key;
                int    score  = ranked[i].Value;
                bool   isMe   = pid == _localPlayerId;
                string label  = isMe ? "You" : $"P{pid + 1}";
                string medal  = i < medals.Length ? medals[i] : $"{i + 1}th";
                string marker = isMe ? " ◀" : "";

                rankTexts[i].text  = $"{medal}  {label,-6}  {score,3}pt{marker}";
                rankTexts[i].color = isMe
                    ? new Color(0f, 1f, 0.8f)
                    : Color.white;
            }
            else
            {
                rankTexts[i].text = "";
            }
        }

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

    // ════════════════════════════════════════════════════════
    private void OnRestartClicked()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.RestartMatch();
        }
        else
        {
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
            NetworkManager.Singleton?.Shutdown();
            EventBus.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
        }
    }
}
