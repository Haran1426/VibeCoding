using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로컬 멀티플레이어 인원 선택 패널.
/// MenuManager 에서 "로컬 플레이" 버튼 클릭 시 ShowPanel() 호출.
/// </summary>
public class LocalLobbyUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject panel;

    [Header("인원 선택")]
    [SerializeField] private Button          minusButton;
    [SerializeField] private Button          plusButton;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("장치 정보")]
    [SerializeField] private TextMeshProUGUI deviceInfoText;

    [Header("버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("씬 이름")]
    [SerializeField] private string arenaSceneName = "ArenaScene";

    private const int Min = 2;
    private const int Max = 10;

    private int           _count = 2;
    private System.Action _onBack;

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        minusButton?.onClick.AddListener(() => SetCount(_count - 1));
        plusButton?.onClick.AddListener(() =>  SetCount(_count + 1));
        startButton?.onClick.AddListener(OnStart);
        backButton?.onClick.AddListener(OnBack);

        panel?.SetActive(false);
    }

    // ── 외부 진입점 ──────────────────────────────────────────

    public void ShowPanel(System.Action onBack)
    {
        _onBack = onBack;
        panel?.SetActive(true);
        SetCount(_count);
    }

    public void HidePanel()
    {
        panel?.SetActive(false);
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    private void SetCount(int n)
    {
        _count = Mathf.Clamp(n, Min, Max);

        if (countText != null)
            countText.text = _count.ToString();

        if (minusButton != null) minusButton.interactable = (_count > Min);
        if (plusButton  != null) plusButton.interactable  = (_count < Max);

        RefreshDeviceInfo();
    }

    private void OnStart()
    {
        LocalMultiplayerConfig.PlayerCount = _count;
        LocalMultiplayerConfig.IsLocalMode = true;
        SceneManager.LoadScene(arenaSceneName);
    }

    private void OnBack()
    {
        HidePanel();
        _onBack?.Invoke();
    }

    // ── 장치 정보 표시 ───────────────────────────────────────

    private void RefreshDeviceInfo()
    {
        if (deviceInfoText == null) return;

        var sb = new System.Text.StringBuilder();
        int padCount = Gamepad.all.Count;

        for (int i = 0; i < _count; i++)
        {
            if (i == 0)
            {
                sb.AppendLine($"P1  :  키보드 + 마우스");
            }
            else
            {
                int padIdx = i - 1;
                bool connected = padIdx < padCount;
                string padName = connected
                    ? Gamepad.all[padIdx].displayName
                    : "연결 안 됨 ⚠";
                sb.AppendLine($"P{i + 1}  :  게임패드 {padIdx + 1}  ({padName})");
            }
        }

        deviceInfoText.text = sb.ToString();
    }
}
