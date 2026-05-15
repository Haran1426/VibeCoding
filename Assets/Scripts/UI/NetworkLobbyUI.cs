using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkLobbyUI : MonoBehaviour
{
    [Header("Runtime lobby")]
    [SerializeField] private bool rebuildLobbyUIAtRuntime = false;

    [Header("Panels")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private GameObject waitingPanel;

    [Header("Connect")]
    [SerializeField] private Button hostButton;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button connectBackButton;
    [SerializeField] private TextMeshProUGUI connectStatusText;

    [Header("Waiting")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI waitingStatusText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button waitingCancelButton;

    [Header("Network")]
    [SerializeField] private string arenaSceneName = "ArenaScene";
    [SerializeField] private ushort port = 7777;
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float joinTimeoutSec = 10f;

    private System.Action _onBack;
    private bool _joiningAsClient;
    private float _joinTimer;
    private bool _matchStarted;
    private bool _networkCallbacksRegistered;

    private void Awake()
    {
        if (rebuildLobbyUIAtRuntime)
            EnsureRuntimeLobbyUI();

        BindButtons();
        lobbyRoot?.SetActive(false);
    }

    private void OnDestroy() => UnsubscribeNetworkCallbacks();

    public void ShowLobby(System.Action onBack)
    {
        if (rebuildLobbyUIAtRuntime)
            EnsureRuntimeLobbyUI();

        BindButtons();
        _onBack = onBack;
        lobbyRoot?.SetActive(true);
        ShowConnectPanel();
        SetConnectStatus("Start a host or join by IP.");
    }

    public void HideLobby()
    {
        lobbyRoot?.SetActive(false);
        Cleanup();
    }

    private void ShowConnectPanel()
    {
        connectPanel?.SetActive(true);
        waitingPanel?.SetActive(false);
        SetButtonsInteractable(true);
        _joiningAsClient = false;
        _joinTimer = 0f;
        _matchStarted = false;
    }

    private void ShowWaitingPanel(bool isHost)
    {
        connectPanel?.SetActive(false);
        waitingPanel?.SetActive(true);

        if (startButton != null)
            startButton.gameObject.SetActive(isHost);

        SetWaitingStatus(isHost
            ? "Waiting for one more player..."
            : "Connected. Waiting for host to start the match.");
        RefreshPlayerCount();
    }

    private void OnHostClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            SetConnectStatus("Network manager is not ready.");
            return;
        }

        SetConnectStatus("Starting host...");
        SetButtonsInteractable(false);

        ConfigureTransport("0.0.0.0");
        RegisterNetworkCallbacks(includeApproval: true);

        if (NetworkManager.Singleton.StartHost())
        {
            ShowWaitingPanel(isHost: true);
            SetWaitingStatus("Waiting for players...");
        }
        else
        {
            UnsubscribeNetworkCallbacks();
            SetConnectStatus("Host failed. Try again.");
            SetButtonsInteractable(true);
        }
    }

    private void OnJoinClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            SetConnectStatus("Network manager is not ready.");
            return;
        }

        string ip = ipInputField != null ? ipInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        if (!IsValidAddress(ip))
        {
            SetConnectStatus("Enter a valid IP address. Example: 192.168.0.1");
            return;
        }

        SetConnectStatus($"Connecting... ({ip}:{port})");
        SetButtonsInteractable(false);

        ConfigureTransport(ip);
        RegisterNetworkCallbacks(includeApproval: false);

        if (NetworkManager.Singleton.StartClient())
        {
            _joiningAsClient = true;
            _joinTimer = 0f;
        }
        else
        {
            UnsubscribeNetworkCallbacks();
            SetConnectStatus("Connection failed. Check the IP and port.");
            SetButtonsInteractable(true);
        }
    }

    private void OnStartClicked()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (count < minPlayersToStart)
        {
            SetWaitingStatus($"Need at least {minPlayersToStart} players. Current: {count}.");
            return;
        }

        startButton.interactable = false;
        _matchStarted = true;
        SetWaitingStatus("Loading arena...");
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
        SetConnectStatus("Connection cancelled.");
    }

    private void OnApproveConnection(
        NetworkManager.ConnectionApprovalRequest req,
        NetworkManager.ConnectionApprovalResponse res)
    {
        if (NetworkManager.Singleton == null)
        {
            res.Approved = false;
            res.CreatePlayerObject = false;
            return;
        }

        int currentCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
        res.Approved = !_matchStarted && currentCount < 4;
        res.CreatePlayerObject = res.Approved;

        if (!res.Approved && !_matchStarted)
            SetWaitingStatus("Server is full (4/4).");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        _joiningAsClient = false;

        if (NetworkManager.Singleton.IsHost)
        {
            RefreshPlayerCount();
        }
        else if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            ShowWaitingPanel(isHost: false);
            SetWaitingStatus("Connected. Waiting for host to start the match.");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsHost)
        {
            RefreshPlayerCount();
        }
        else if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Cleanup();
            ShowConnectPanel();
            SetConnectStatus("Disconnected from server.");
        }
    }

    private void Update()
    {
        if (_joiningAsClient)
        {
            _joinTimer += Time.deltaTime;
            if (_joinTimer >= joinTimeoutSec)
            {
                _joiningAsClient = false;
                Cleanup();
                ShowConnectPanel();
                SetConnectStatus("Connection timed out. Check the IP address.");
            }
        }

        if (waitingPanel != null && waitingPanel.activeSelf &&
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            RefreshPlayerCount();
        }
    }

    private void RefreshPlayerCount()
    {
        if (NetworkManager.Singleton == null) return;

        int count = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (playerCountText != null)
            playerCountText.text = $"PLAYERS  {count} / 4";

        if (startButton != null)
            startButton.interactable = count >= minPlayersToStart;

        if (NetworkManager.Singleton.IsHost)
        {
            SetWaitingStatus(count >= minPlayersToStart
                ? $"{count} players connected. Ready to start."
                : $"Waiting for players... ({count}/{minPlayersToStart})");
        }
        else
        {
            SetWaitingStatus("Connected. Waiting for host to start the match.");
        }
    }

    private void Cleanup()
    {
        UnsubscribeNetworkCallbacks();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        _joiningAsClient = false;
        _joinTimer = 0f;
        _matchStarted = false;
    }

    private void UnsubscribeNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.ConnectionApprovalCallback = null;
        if (!_networkCallbacksRegistered) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        _networkCallbacksRegistered = false;
    }

    private void RegisterNetworkCallbacks(bool includeApproval)
    {
        if (NetworkManager.Singleton == null) return;

        UnsubscribeNetworkCallbacks();
        if (includeApproval)
            NetworkManager.Singleton.ConnectionApprovalCallback = OnApproveConnection;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        _networkCallbacksRegistered = true;
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

    private void EnsureRuntimeLobbyUI()
    {
        if (lobbyRoot != null && lobbyRoot.name == "Title_Lobby_Runtime") return;

        Canvas canvas = RuntimeUIFactory.EnsureCanvas("Title Canvas");
        lobbyRoot = RuntimeUIFactory.CreatePanel(canvas.transform, "Title_Lobby_Runtime",
            new Color(0.005f, 0.007f, 0.012f, 0.98f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RuntimeUIFactory.CreatePanel(lobbyRoot.transform, "Lobby_CyanRail", new Color(0f, 0.72f, 1f, 0.22f),
            new Vector2(0.04f, 0.12f), new Vector2(0.047f, 0.88f), Vector2.zero, Vector2.zero);
        RuntimeUIFactory.CreatePanel(lobbyRoot.transform, "Lobby_MagentaRail", new Color(1f, 0.16f, 0.58f, 0.20f),
            new Vector2(0.945f, 0.12f), new Vector2(0.952f, 0.88f), Vector2.zero, Vector2.zero);

        RuntimeUIFactory.CreateText(lobbyRoot.transform, "Lobby_Title", "ONLINE LOBBY", 64,
            TextAlignmentOptions.Left, new Vector2(0.08f, 0.77f), new Vector2(0.56f, 0.86f),
            Vector2.zero, Vector2.zero, Color.white);
        RuntimeUIFactory.CreatePixelText(lobbyRoot.transform, "Lobby_Title_Pixel", "ONLINE LOBBY", TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.735f), new Vector2(0.50f, 0.795f),
            Vector2.zero, Vector2.zero, Color.white);
        RuntimeUIFactory.CreateText(lobbyRoot.transform, "Lobby_Subtitle", "Host a room, or join a host by IP. Online only.",
            24, TextAlignmentOptions.Left, new Vector2(0.08f, 0.68f), new Vector2(0.58f, 0.73f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.83f, 0.92f));

        connectPanel = RuntimeUIFactory.CreatePanel(lobbyRoot.transform, "Lobby_ConnectPanel",
            new Color(0.025f, 0.035f, 0.05f, 0.9f),
            new Vector2(0.08f, 0.18f), new Vector2(0.56f, 0.64f), Vector2.zero, Vector2.zero);
        CreateConnectPanelUI(connectPanel.transform);

        waitingPanel = RuntimeUIFactory.CreatePanel(lobbyRoot.transform, "Lobby_WaitingPanel",
            new Color(0.025f, 0.035f, 0.05f, 0.9f),
            new Vector2(0.08f, 0.18f), new Vector2(0.56f, 0.64f), Vector2.zero, Vector2.zero);
        CreateWaitingPanelUI(waitingPanel.transform);

        CreateLobbyShowcase(lobbyRoot.transform);

        lobbyRoot.SetActive(false);
    }

    private void CreateConnectPanelUI(Transform root)
    {
        RuntimeUIFactory.CreateText(root, "Connect_Title", "CONNECT", 38,
            TextAlignmentOptions.Left, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f),
            Vector2.zero, Vector2.zero, Color.white);
        RuntimeUIFactory.CreatePixelText(root, "Connect_Title_Pixel", "CONNECT", TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.80f), new Vector2(0.50f, 0.90f), Vector2.zero, Vector2.zero, Color.white);

        hostButton = RuntimeUIFactory.CreateButton(root, "Connect_Host", "HOST ROOM",
            Vector2.zero, new Vector2(250f, 62f));
        SetRect(hostButton.transform as RectTransform, new Vector2(0.08f, 0.59f), new Vector2(0.08f, 0.59f),
            new Vector2(125f, 0f), new Vector2(250f, 62f), new Vector2(0.5f, 0.5f));

        var inputRoot = RuntimeUIFactory.CreatePanel(root, "Connect_IpInput", new Color(0.06f, 0.08f, 0.11f, 0.96f),
            new Vector2(0.08f, 0.38f), new Vector2(0.60f, 0.52f), Vector2.zero, Vector2.zero);
        ipInputField = inputRoot.AddComponent<TMP_InputField>();
        ipInputField.textComponent = RuntimeUIFactory.CreateText(inputRoot.transform, "Ip_Text", "127.0.0.1", 28,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        ipInputField.placeholder = RuntimeUIFactory.CreateText(inputRoot.transform, "Ip_Placeholder", "127.0.0.1", 28,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.6f, 0.68f, 0.76f, 0.8f));
        ipInputField.text = "127.0.0.1";
        ipInputField.characterLimit = 32;

        joinButton = RuntimeUIFactory.CreateButton(root, "Connect_Join", "JOIN",
            Vector2.zero, new Vector2(150f, 62f));
        SetRect(joinButton.transform as RectTransform, new Vector2(0.76f, 0.45f), new Vector2(0.76f, 0.45f),
            new Vector2(75f, 0f), new Vector2(150f, 62f), new Vector2(0.5f, 0.5f));

        connectStatusText = RuntimeUIFactory.CreateText(root, "Connect_Status", "Start a host or join by IP.", 24,
            TextAlignmentOptions.Left, new Vector2(0.08f, 0.22f), new Vector2(0.88f, 0.32f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.83f, 0.92f));

        connectBackButton = RuntimeUIFactory.CreateButton(root, "Connect_Back", "BACK",
            Vector2.zero, new Vector2(160f, 54f));
        SetRect(connectBackButton.transform as RectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.08f, 0.10f),
            new Vector2(80f, 0f), new Vector2(160f, 54f), new Vector2(0.5f, 0.5f));
    }

    private void CreateWaitingPanelUI(Transform root)
    {
        RuntimeUIFactory.CreateText(root, "Waiting_Title", "WAITING ROOM", 38,
            TextAlignmentOptions.Left, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f),
            Vector2.zero, Vector2.zero, Color.white);
        RuntimeUIFactory.CreatePixelText(root, "Waiting_Title_Pixel", "WAITING ROOM", TextAnchor.MiddleLeft,
            new Vector2(0.08f, 0.80f), new Vector2(0.70f, 0.90f), Vector2.zero, Vector2.zero, Color.white);

        playerCountText = RuntimeUIFactory.CreateText(root, "Waiting_Count", "PLAYERS  1 / 4", 42,
            TextAlignmentOptions.Left, new Vector2(0.08f, 0.58f), new Vector2(0.88f, 0.70f),
            Vector2.zero, Vector2.zero, new Color(0.1f, 0.95f, 1f));

        waitingStatusText = RuntimeUIFactory.CreateText(root, "Waiting_Status", "Waiting for players...", 24,
            TextAlignmentOptions.Left, new Vector2(0.08f, 0.42f), new Vector2(0.88f, 0.52f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.83f, 0.92f));

        startButton = RuntimeUIFactory.CreateButton(root, "Waiting_Start", "START MATCH",
            Vector2.zero, new Vector2(280f, 62f));
        SetRect(startButton.transform as RectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.08f, 0.22f),
            new Vector2(140f, 0f), new Vector2(280f, 62f), new Vector2(0.5f, 0.5f));

        waitingCancelButton = RuntimeUIFactory.CreateButton(root, "Waiting_Cancel", "CANCEL",
            Vector2.zero, new Vector2(180f, 54f));
        SetRect(waitingCancelButton.transform as RectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.08f, 0.10f),
            new Vector2(90f, 0f), new Vector2(180f, 54f), new Vector2(0.5f, 0.5f));
    }

    private static void CreateLobbyShowcase(Transform root)
    {
        var side = RuntimeUIFactory.CreateRoundedPanel(root, "Lobby_SidePreview",
            new Color(0.035f, 0.045f, 0.065f, 0.96f),
            new Vector2(0.64f, 0.18f), new Vector2(0.91f, 0.66f), Vector2.zero, Vector2.zero,
            44f, new Color(1f, 1f, 1f, 0.9f));

        RuntimeUIFactory.CreateText(side.transform, "Lobby_MapLabel", "NEXT ARENA", 30,
            TextAlignmentOptions.Center, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.90f),
            Vector2.zero, Vector2.zero, Color.white);
        RuntimeUIFactory.CreatePixelText(side.transform, "Lobby_MapLabel_Pixel", "UP TO 4", TextAnchor.MiddleCenter,
            new Vector2(0.22f, 0.70f), new Vector2(0.78f, 0.78f),
            Vector2.zero, Vector2.zero, new Color(1f, 0.82f, 0.18f));

        var mini = RuntimeUIFactory.CreateRoundedPanel(side.transform, "Lobby_MiniArena",
            new Color(0.005f, 0.012f, 0.025f, 0.90f),
            new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.66f), Vector2.zero, Vector2.zero,
            34f, new Color(0.1f, 0.95f, 1f, 0.55f));

        RuntimeUIFactory.CreateRoundedPanel(mini.transform, "Lobby_PlatformMain", new Color(0.03f, 0.56f, 0.95f, 0.96f),
            new Vector2(0.16f, 0.42f), new Vector2(0.84f, 0.54f), Vector2.zero, Vector2.zero, 18f);
        RuntimeUIFactory.CreateRoundedPanel(mini.transform, "Lobby_PlatformTop", new Color(1f, 0.30f, 0.08f, 0.96f),
            new Vector2(0.34f, 0.63f), new Vector2(0.66f, 0.73f), Vector2.zero, Vector2.zero, 16f);
        RuntimeUIFactory.CreateRoundedPanel(mini.transform, "Lobby_PlayerA", new Color(0f, 0.78f, 1f, 0.96f),
            new Vector2(0.24f, 0.60f), new Vector2(0.34f, 0.78f), Vector2.zero, Vector2.zero, 16f);
        RuntimeUIFactory.CreateRoundedPanel(mini.transform, "Lobby_PlayerB", new Color(1f, 0.18f, 0.58f, 0.96f),
            new Vector2(0.68f, 0.22f), new Vector2(0.78f, 0.40f), Vector2.zero, Vector2.zero, 16f);

        RuntimeUIFactory.CreateText(side.transform, "Lobby_RuleText", "Host starts the match after players join.", 20,
            TextAlignmentOptions.Center, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.18f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.83f, 0.92f));
    }

    private void BindButtons()
    {
        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(OnHostClicked);
            hostButton.onClick.AddListener(OnHostClicked);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(OnJoinClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        if (connectBackButton != null)
        {
            connectBackButton.onClick.RemoveListener(OnConnectBackClicked);
            connectBackButton.onClick.AddListener(OnConnectBackClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (waitingCancelButton != null)
        {
            waitingCancelButton.onClick.RemoveListener(OnWaitingCancelClicked);
            waitingCancelButton.onClick.AddListener(OnWaitingCancelClicked);
        }
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        if (rect == null) return;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        rect.pivot = pivot;
    }

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
