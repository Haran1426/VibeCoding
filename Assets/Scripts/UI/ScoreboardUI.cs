using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 접속 인원에 따라 동적으로 조절되는 점수 HUD.
///
/// 싱글:  행 1개
/// 멀티:  접속한 플레이어 수만큼 행 표시 (최대 4)
///
/// 행 구성: [색상 닷] [이름(YOU / P2 / P3 / P4)] [점수]
///
/// Inspector 연결:
///   rows 배열에 ScoreRow 4개를 연결합니다.
///   각 ScoreRow.root 는 비활성 상태로 두면 됩니다 — 코드에서 자동으로 켜고 끕니다.
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    [System.Serializable]
    public class ScoreRow
    {
        public GameObject      root;
        public Image           colorDot;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI scoreText;
    }

    [Header("순위 행 (Inspector에서 4개 연결)")]
    [SerializeField] private ScoreRow[] rows = new ScoreRow[4];

    // ── 내부 상태 ────────────────────────────────────────────
    private readonly Dictionary<int, int> _scores    = new Dictionary<int, int>();
    private readonly List<int>            _playerIds = new List<int>();  // 접속 순서 유지

    private int   _localPlayerId = 0;
    private bool  _idResolved    = false;
    private float _retryTimer    = 0f;
    private const float RetryInterval = 0.5f;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        foreach (var row in rows)
            row?.root?.SetActive(false);
    }

    void Start()
    {
        TryResolveLocalId();

        if (!IsMultiplayer())
        {
            // 싱글: 플레이어 0 고정
            RegisterPlayer(0);
            RefreshDisplay();
        }
    }

    void OnEnable()
    {
        EventBus.OnScoreChanged += OnScoreChanged;
        SubscribeNetworkCallbacks();
    }

    void OnDisable()
    {
        EventBus.OnScoreChanged -= OnScoreChanged;
        UnsubscribeNetworkCallbacks();
    }

    void Update()
    {
        if (_idResolved) return;

        _retryTimer += Time.deltaTime;
        if (_retryTimer < RetryInterval) return;
        _retryTimer = 0f;
        TryResolveLocalId();
    }

    // ════════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════════

    private void TryResolveLocalId()
    {
        if (!IsMultiplayer())
        {
            _localPlayerId = 0;
            _idResolved    = true;
            return;
        }

        var netSync = FindFirstObjectByType<PlayerNetworkSync>();
        if (netSync == null || !netSync.IsSpawned || !netSync.IsOwner) return;

        _localPlayerId = (int)netSync.OwnerClientId;
        _idResolved    = true;

        // 현재 접속된 모든 플레이어 일괄 등록
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
            RegisterPlayer((int)id);

        RefreshDisplay();
    }

    // ════════════════════════════════════════════════════════
    //  네트워크 콜백
    // ════════════════════════════════════════════════════════

    private void SubscribeNetworkCallbacks()
    {
        if (!IsMultiplayer()) return;
        NetworkManager.Singleton.OnClientConnectedCallback  += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;
    }

    private void UnsubscribeNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback  -= OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
    }

    private void OnPlayerJoined(ulong clientId)
    {
        RegisterPlayer((int)clientId);
        RefreshDisplay();
    }

    private void OnPlayerLeft(ulong clientId)
    {
        int pid = (int)clientId;
        _playerIds.Remove(pid);
        _scores.Remove(pid);
        RefreshDisplay();
    }

    // ════════════════════════════════════════════════════════
    //  점수 이벤트
    // ════════════════════════════════════════════════════════

    private void OnScoreChanged(int playerId, int score)
    {
        _scores[playerId] = score;

        if (!_playerIds.Contains(playerId))
        {
            RegisterPlayer(playerId);
        }

        RefreshDisplay();
    }

    // ════════════════════════════════════════════════════════
    //  표시 갱신
    // ════════════════════════════════════════════════════════

    private void RefreshDisplay()
    {
        if (rows == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (row == null || row.root == null) continue;

            if (i >= _playerIds.Count)
            {
                row.root.SetActive(false);
                continue;
            }

            int   pid   = _playerIds[i];
            int   score = _scores.TryGetValue(pid, out int s) ? s : 0;
            bool  isMe  = (pid == _localPlayerId);
            Color color = PlayerVisuals.ColorOf(pid);

            row.root.SetActive(true);

            if (row.colorDot  != null) row.colorDot.color  = color;
            if (row.nameText  != null)
            {
                row.nameText.text  = isMe ? "YOU" : $"P{pid + 1}";
                row.nameText.color = isMe ? color : Color.white;
                row.nameText.fontStyle = isMe
                    ? FontStyles.Bold
                    : FontStyles.Normal;
            }
            if (row.scoreText != null)
            {
                row.scoreText.text  = score.ToString();
                row.scoreText.color = isMe ? color : Color.white;
            }
        }
    }

    // ════════════════════════════════════════════════════════

    private void RegisterPlayer(int playerId)
    {
        if (_playerIds.Contains(playerId)) return;
        _playerIds.Add(playerId);
        if (!_scores.ContainsKey(playerId))
            _scores[playerId] = 0;
    }

    private static bool IsMultiplayer() =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
}
