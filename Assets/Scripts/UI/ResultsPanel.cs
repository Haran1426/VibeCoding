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
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("버튼")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private int _localPlayerId;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        EnsureRuntimeResultsUI();

        panel?.SetActive(false);
        restartButton?.onClick.AddListener(OnRestartClicked);
        menuButton?.onClick.AddListener(OnMenuClicked);
    }

    private void EnsureRuntimeResultsUI()
    {
        if (panel != null && titleText != null && rankTexts != null && rankTexts.Length > 0)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = RuntimeUIFactory.EnsureCanvas();

        if (panel == null)
            panel = RuntimeUIFactory.CreatePanel(canvas.transform, "ResultsPanel_Runtime",
                new Color(0.01f, 0.015f, 0.025f, 0.9f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (titleText == null)
            titleText = RuntimeUIFactory.CreateText(panel.transform, "ResultsTitle_Runtime", "MATCH END", 64,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 180f), new Vector2(540f, 80f), new Color(0f, 0.9f, 1f));

        if (rankTexts == null || rankTexts.Length == 0)
        {
            rankTexts = new TextMeshProUGUI[4];
            for (int i = 0; i < rankTexts.Length; i++)
            {
                rankTexts[i] = RuntimeUIFactory.CreateText(panel.transform, $"RankText_Runtime_{i + 1}", "", 34,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 92f - i * 44f), new Vector2(520f, 42f), Color.white);
            }
        }

        if (bestScoreText == null)
            bestScoreText = RuntimeUIFactory.CreateText(panel.transform, "BestScore_Runtime", "", 26,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -108f), new Vector2(360f, 40f), new Color(1f, 0.85f, 0.2f));

        if (hintText == null)
            hintText = RuntimeUIFactory.CreateText(panel.transform, "ResultHint_Runtime", "", 20,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -142f), new Vector2(560f, 32f), new Color(0.72f, 0.83f, 0.92f));

        if (restartButton == null)
            restartButton = RuntimeUIFactory.CreateButton(panel.transform, "RestartButton_Runtime", "REMATCH",
                new Vector2(-145f, -176f), new Vector2(240f, 58f));

        if (menuButton == null)
            menuButton = RuntimeUIFactory.CreateButton(panel.transform, "ResultMenuButton_Runtime", "TITLE",
                new Vector2(145f, -176f), new Vector2(240f, 58f));
    }

    void Start()
    {
        ResolveLocalPlayerId();
    }

    void OnEnable()  => EventBus.OnMatchEnded += OnMatchEnded;
    void OnDisable() => EventBus.OnMatchEnded -= OnMatchEnded;

    void OnDestroy()
    {
        restartButton?.onClick.RemoveListener(OnRestartClicked);
        menuButton?.onClick.RemoveListener(OnMenuClicked);
    }

    // ════════════════════════════════════════════════════════
    private void OnMatchEnded(Dictionary<int, int> scores)
    {
        panel?.SetActive(true);
        ResolveLocalPlayerId();

        var ranked = scores
            .OrderByDescending(kv => kv.Value)
            .ToList();

        int  winnerId = ranked.Count > 0 ? ranked[0].Key : -1;
        bool iWon     = winnerId == _localPlayerId;

        if (titleText != null)
            titleText.text = iWon ? "YOU WIN!" : "MATCH END";

        if (restartButton != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            restartButton.interactable = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;

        if (hintText != null)
        {
            bool online = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool host = online && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer);
            hintText.text = online
                ? (host ? "Host can start the rematch." : "Waiting for host to rematch or return to title.")
                : "Rematch restarts this arena.";
        }

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
                string marker = isMe ? "  YOU" : "";

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (restartButton != null && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
                restartButton.interactable = false;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                EventBus.Clear();
                Time.timeScale = 1f;
                NetworkManager.Singleton.SceneManager.LoadScene(
                    SceneManager.GetActiveScene().name,
                    LoadSceneMode.Single);
            }
            return;
        }

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            EventBus.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
            return;
        }

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

    private void ResolveLocalPlayerId()
    {
        _localPlayerId = 0;

        var syncs = FindObjectsByType<PlayerNetworkSync>(FindObjectsSortMode.None);
        foreach (var netSync in syncs)
        {
            if (netSync == null || !netSync.IsSpawned || !netSync.IsOwner) continue;
            _localPlayerId = (int)netSync.OwnerClientId;
            return;
        }
    }
}
