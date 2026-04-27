using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 멀티플레이어 로비 UI + 네트워크 연결 처리.
///
/// 흐름:
///   [접속 전] Host 버튼 / IP 입력 + Join 버튼
///       ↓ Host 성공
///   [대기 중] 플레이어 수 표시 + 게임 시작 버튼 (호스트만)
///       ↓ 클라이언트 Join 성공
///   [대기 중] "호스트 시작 대기" 메시지
///       ↓ 호스트 Start 버튼
///   ArenaScene 로드 (NetworkManager.SceneManager)
///
/// 뒤로가기: MenuManager.ShowMain() 으로 돌아감
/// </summary>
public class NetworkLobbyUI : MonoBehaviour
{
    // ── 최상위 패널 ──────────────────────────────────────────
    [Header("패널")]
    [SerializeField] private GameObject lobbyRoot;   // 로비 전체 루트

    // ── 접속 전 UI ───────────────────────────────────────────
    [Header("접속 전")]
    [SerializeField] private GameObject      connectPanel;
    [SerializeField] private Button          hostButton;
    [SerializeField] private TMP_InputField  ipInputField;
    [SerializeField] private Button          joinButton;
    [SerializeField] private Button          connectBackButton;
    [SerializeField] private TextMeshProUGUI connectStatusText;

    // ── 대기 중 UI ───────────────────────────────────────────
    [Header("대기 중")]
    [SerializeField] private GameObject      waitingPanel;
    [SerializeField] private TextMeshProUGUI playerCountText;    // "플레이어  2 / 4"
    [SerializeField] private TextMeshProUGUI waitingStatusText;  // 상태 메시지
    [SerializeField] private Button          startButton;        // 호스트만 보임
    [SerializeField] private Button          waitingCancelButton;

    // ── 설정 ─────────────────────────────────────────────────
    [Header("설정")]
    [SerializeField] private string arenaSceneName = "ArenaScene";
    [SerializeField] private ushort port           = 7777;
    [SerializeField] private int    minPlayersToStart = 2;   // 시작 가능 최소 인원
    [SerializeField] private float  joinTimeoutSec  = 10f;   // 접속 시도 타임아웃

    // ── 내부 상태 ────────────────────────────────────────────
    private System.Action _onBack;
    private bool  _joiningAsClient;
    private float _joinTimer;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        hostButton?.onClick.AddListener(OnHostClicked);
        joinButton?.onClick.AddListener(OnJoinClicked);
        connectBackButton?.onClick.AddListener(OnConnectBackClicked);
        startButton?.onClick.AddListener(OnStartClicked);
        waitingCancelButton?.onClick.AddListener(OnWaitingCancelClicked);

        lobbyRoot?.SetActive(false);
    }

    void OnDestroy() => UnsubscribeNetworkCallbacks();

    // ════════════════════════════════════════════════════════
    //  외부 진입점 (MenuManager 에서 호출)
    // ════════════════════════════════════════════════════════

    public void ShowLobby(System.Action onBack)
    {
        _onBack = onBack;
        lobbyRoot?.SetActive(true);
        ShowConnectPanel();
        SetConnectStatus("호스트를 시작하거나 IP 를 입력해 참가하세요.");
    }

    public void HideLobby()
    {
        lobbyRoot?.SetActive(false);
        Cleanup();
    }

    // ════════════════════════════════════════════════════════
    //  패널 전환
    // ════════════════════════════════════════════════════════

    private void ShowConnectPanel()
    {
        connectPanel?.SetActive(true);
        waitingPanel?.SetActive(false);
        SetButtonsInteractable(true);
        _joiningAsClient = false;
        _joinTimer       = 0f;
    }

    private void ShowWaitingPanel(bool isHost)
    {
        connectPanel?.SetActive(false);
        waitingPanel?.SetActive(true);

        if (startButton != null)
            startButton.gameObject.SetActive(isHost);

        RefreshPlayerCount();
    }

    // ════════════════════════════════════════════════════════
    //  버튼 핸들러
    // ════════════════════════════════════════════════════════

    private void OnHostClicked()
    {
        SetConnectStatus("호스트 시작 중...");
        SetButtonsInteractable(false);

        ConfigureTransport("0.0.0.0");
        NetworkManager.Singleton.ConnectionApprovalCallback  = OnApproveConnection;
        NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (NetworkManager.Singleton.StartHost())
        {
            ShowWaitingPanel(isHost: true);
            SetWaitingStatus("플레이어를 기다리는 중...");
        }
        else
        {
            SetConnectStatus("호스트 시작 실패. 다시 시도하세요.");
            SetButtonsInteractable(true);
        }
    }

    private void OnJoinClicked()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        if (!IsValidAddress(ip))
        {
            SetConnectStatus("올바른 IP 주소를 입력하세요. (예: 192.168.0.1)");
            return;
        }

        SetConnectStatus($"연결 중... ({ip}:{port})");
        SetButtonsInteractable(false);

        ConfigureTransport(ip);
        NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (NetworkManager.Singleton.StartClient())
        {
            _joiningAsClient = true;
            _joinTimer       = 0f;
        }
        else
        {
            SetConnectStatus("연결 실패. IP 와 포트를 확인하세요.");
            SetButtonsInteractable(true);
        }
    }

    private void OnStartClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (count < minPlayersToStart)
        {
            SetWaitingStatus($"최소 {minPlayersToStart}명이 필요합니다. (현재 {count}명)");
            return;
        }

        startButton.interactable = false;
        SetWaitingStatus("게임 시작 중...");
        NetworkManager.Singleton.SceneManager.LoadScene(arenaSceneName, LoadSceneMode.Single);
    }

    private void OnConnectBackClicked()
    {
        Cleanup();
        _onBack?.Invoke();
    }

    private void OnWaitingCancelClicked()
    {
        Cleanup();
        ShowConnectPanel();
        SetConnectStatus("연결을 취소했습니다.");
    }

    // ════════════════════════════════════════════════════════
    //  네트워크 콜백
    // ════════════════════════════════════════════════════════

    private void OnApproveConnection(
        NetworkManager.ConnectionApprovalRequest  req,
        NetworkManager.ConnectionApprovalResponse res)
    {
        int currentCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        res.Approved           = currentCount < 4;
        res.CreatePlayerObject = res.Approved;

        if (!res.Approved)
            SetWaitingStatus("서버가 가득 찼습니다 (4/4).");
    }

    private void OnClientConnected(ulong clientId)
    {
        _joiningAsClient = false;

        if (NetworkManager.Singleton.IsHost)
        {
            RefreshPlayerCount();
        }
        else if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            // 내 접속 완료
            ShowWaitingPanel(isHost: false);
            SetWaitingStatus("호스트가 게임을 시작할 때까지 대기 중...");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            RefreshPlayerCount();
        }
        else if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            // 서버에서 끊김
            Cleanup();
            ShowConnectPanel();
            SetConnectStatus("서버 연결이 끊겼습니다.");
        }
    }

    // ════════════════════════════════════════════════════════
    //  Update — 타임아웃 / 플레이어 수 갱신
    // ════════════════════════════════════════════════════════

    void Update()
    {
        // 클라이언트 접속 타임아웃
        if (_joiningAsClient)
        {
            _joinTimer += Time.deltaTime;
            if (_joinTimer >= joinTimeoutSec)
            {
                _joiningAsClient = false;
                Cleanup();
                ShowConnectPanel();
                SetConnectStatus("연결 시간이 초과됐습니다. IP 를 확인하세요.");
            }
        }

        // 호스트: 대기 패널이 켜져 있을 때 플레이어 수 실시간 갱신
        if (waitingPanel != null && waitingPanel.activeSelf &&
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            RefreshPlayerCount();
        }
    }

    // ════════════════════════════════════════════════════════
    //  유틸
    // ════════════════════════════════════════════════════════

    private void RefreshPlayerCount()
    {
        if (NetworkManager.Singleton == null) return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (playerCountText != null)
            playerCountText.text = $"플레이어  {count} / 4";

        if (startButton != null)
            startButton.interactable = count >= minPlayersToStart;

        if (NetworkManager.Singleton.IsHost)
        {
            SetWaitingStatus(count >= minPlayersToStart
                ? $"{count}명 접속 — 게임을 시작할 수 있습니다!"
                : $"플레이어를 기다리는 중... ({count}/{minPlayersToStart})");
        }
    }

    private void Cleanup()
    {
        UnsubscribeNetworkCallbacks();

        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
            NetworkManager.Singleton.Shutdown();

        _joiningAsClient = false;
        _joinTimer       = 0f;
    }

    private void UnsubscribeNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.ConnectionApprovalCallback  = null;
        NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void ConfigureTransport(string address)
    {
        var utp = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        utp?.SetConnectionData(address, port);
    }

    private void SetButtonsInteractable(bool on)
    {
        if (hostButton != null) hostButton.interactable = on;
        if (joinButton != null) joinButton.interactable = on;
    }

    private void SetConnectStatus(string msg)
    {
        if (connectStatusText != null) connectStatusText.text = msg;
    }

    private void SetWaitingStatus(string msg)
    {
        if (waitingStatusText != null) waitingStatusText.text = msg;
    }

    // 기본 IP / 도메인 주소 유효성 검사
    private static bool IsValidAddress(string addr)
    {
        if (addr == "localhost") return true;

        string[] parts = addr.Split('.');
        if (parts.Length != 4) return false;

        foreach (string p in parts)
        {
            if (!int.TryParse(p, out int n) || n < 0 || n > 255) return false;
        }
        return true;
    }
}
