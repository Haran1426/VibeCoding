using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 타이틀 씬 전체 패널 흐름을 관리합니다.
/// 싱글플레이 / 멀티플레이 / 설정 / 종료
/// </summary>
public class MenuManager : MonoBehaviour
{
    // ── 패널 ─────────────────────────────────────────────────
    [Header("패널")]
    [SerializeField] private GameObject    mainPanel;
    [SerializeField] private GameObject    settingsPanel;
    [SerializeField] private NetworkLobbyUI lobbyUI;    // 네트워크 멀티 로비
    [SerializeField] private LocalLobbyUI   localLobbyUI; // 로컬 멀티 인원 선택

    // ── 메인 패널 ─────────────────────────────────────────────
    [Header("메인 버튼")]
    [SerializeField] private Button playSingleButton;  // 로컬 플레이 (인원 선택)
    [SerializeField] private Button playMultiButton;   // 네트워크 멀티 로비
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("메인 정보")]
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI versionText;

    // ── 설정 패널 ─────────────────────────────────────────────
    [Header("설정")]
    [SerializeField] private Slider          masterVolumeSlider;
    [SerializeField] private Slider          sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
    [SerializeField] private Button          settingsBackButton;

    private const string KeyMaster    = "MasterVolume";
    private const string KeySFX       = "SFXVolume";
    private const string KeyBestScore = "BestScore";

    // ════════════════════════════════════════════════════════
    void Awake()
    {
        playSingleButton?.onClick.AddListener(OnSinglePlay);
        playMultiButton?.onClick.AddListener(OnMultiPlay);
        settingsButton?.onClick.AddListener(ShowSettings);
        settingsBackButton?.onClick.AddListener(ShowMain);
        quitButton?.onClick.AddListener(OnQuit);

        masterVolumeSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXChanged);
    }

    void Start()
    {
        ShowMain();
        LoadSettings();
        RefreshBestScore();

        if (versionText != null) versionText.text = "v0.1.0";
    }

    // ── 패널 전환 ─────────────────────────────────────────────

    public void ShowMain()
    {
        mainPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        lobbyUI?.HideLobby();
        RefreshBestScore();
    }

    private void ShowSettings()
    {
        mainPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    private void OnSinglePlay()
    {
        mainPanel?.SetActive(false);
        localLobbyUI?.ShowPanel(ShowMain);
    }

    private void OnMultiPlay()
    {
        mainPanel?.SetActive(false);
        lobbyUI?.ShowLobby(ShowMain);   // 뒤로가기 콜백 전달
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── 설정 ─────────────────────────────────────────────────

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(KeyMaster, 0.8f);
        float sfx    = PlayerPrefs.GetFloat(KeySFX,    0.8f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = sfx;

        ApplyVolume(master);
        UpdateVolumeLabels(master, sfx);
    }

    private void OnMasterChanged(float val)
    {
        PlayerPrefs.SetFloat(KeyMaster, val);
        PlayerPrefs.Save();
        ApplyVolume(val);
        UpdateVolumeLabels(val, PlayerPrefs.GetFloat(KeySFX, 0.8f));
    }

    private void OnSFXChanged(float val)
    {
        PlayerPrefs.SetFloat(KeySFX, val);
        PlayerPrefs.Save();
        UpdateVolumeLabels(PlayerPrefs.GetFloat(KeyMaster, 0.8f), val);
    }

    private static void ApplyVolume(float master)
    {
        AudioListener.volume = master;
        AudioManager.Instance?.RefreshVolume();
    }

    private void UpdateVolumeLabels(float master, float sfx)
    {
        if (masterVolumeLabel != null)
            masterVolumeLabel.text = Mathf.RoundToInt(master * 100) + "%";
        if (sfxVolumeLabel != null)
            sfxVolumeLabel.text = Mathf.RoundToInt(sfx * 100) + "%";
    }

    private void RefreshBestScore()
    {
        if (bestScoreText == null) return;
        int best = PlayerPrefs.GetInt(KeyBestScore, 0);
        bestScoreText.text = best > 0 ? $"BEST  {best}pt" : "";
    }
}
