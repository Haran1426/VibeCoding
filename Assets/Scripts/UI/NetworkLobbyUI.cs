using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 메뉴에서 호스트 시작 / 클라이언트 참가를 처리합니다.
/// Unity Transport (UTP) 기반 LAN / 직접 IP 연결을 사용합니다.
/// </summary>
public class NetworkLobbyUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("호스트")]
    [SerializeField] private Button hostButton;

    [Header("클라이언트")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button         joinButton;

    [Header("상태")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button          backButton;

    [Header("설정")]
    [SerializeField] private string arenaSceneName = "ArenaScene";
    [SerializeField] private ushort port           = 7777;

    void Awake()
    {
        hostButton?.onClick.AddListener(OnHostClicked);
        joinButton?.onClick.AddListener(OnJoinClicked);
        backButton?.onClick.AddListener(OnBackClicked);
        lobbyPanel?.SetActive(false);
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    public void ShowLobby()
    {
        mainPanel?.SetActive(false);
        lobbyPanel?.SetActive(true);
        SetStatus("IP 를 입력하고 참가하거나, 호스트를 시작하세요.");
    }

    private void OnHostClicked()
    {
        SetStatus("호스트 시작 중...");
        ConfigureTransport("0.0.0.0");

        NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;
        NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;

        if (NetworkManager.Singleton.StartHost())
        {
            SetStatus("호스트 시작됨 — 플레이어 대기 중...");
            // 혼자서도 바로 시작 가능
            Invoke(nameof(LoadArena), 1f);
        }
        else SetStatus("호스트 시작 실패.");
    }

    private void OnJoinClicked()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : "127.0.0.1";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        SetStatus($"연결 중... ({ip}:{port})");
        ConfigureTransport(ip);

        NetworkManager.Singleton.OnClientConnectedCallback     += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback    += OnClientDisconnected;

        if (!NetworkManager.Singleton.StartClient())
            SetStatus("연결 실패.");
    }

    private void OnBackClicked()
    {
        lobbyPanel?.SetActive(false);
        mainPanel?.SetActive(true);

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.Shutdown();
    }

    // ── 연결 승인 (서버 전용) ─────────────────────────────────
    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest req,
        NetworkManager.ConnectionApprovalResponse res)
    {
        // 최대 4인 제한
        res.Approved = NetworkManager.Singleton.ConnectedClientsIds.Count < 4;
        res.CreatePlayerObject = true;
    }

    // ── 콜백 ─────────────────────────────────────────────────
    private void OnClientConnected(ulong clientId)
    {
        SetStatus($"플레이어 {clientId} 연결됨");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        SetStatus("연결이 끊겼습니다.");
    }

    // ── 씬 전환 ──────────────────────────────────────────────
    private void LoadArena()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                arenaSceneName, LoadSceneMode.Single);
        }
    }

    // ── 유틸 ─────────────────────────────────────────────────
    private void ConfigureTransport(string address)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utp == null) return;
        utp.SetConnectionData(address, port);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Lobby] " + msg);
    }
}
