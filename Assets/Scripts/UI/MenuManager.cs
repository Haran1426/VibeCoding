using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메인 메뉴 씬 전체를 관리합니다.
/// 설정(음량)은 PlayerPrefs로 저장합니다.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Panel")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    [Header("Settings")]
    [SerializeField] private Slider  masterVolumeSlider;
    [SerializeField] private Slider  sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;

    private const string KEY_MASTER_VOL = "MasterVolume";
    private const string KEY_SFX_VOL    = "SFXVolume";
    private const string KEY_BEST_SCORE = "BestScore";

    void Start()
    {
        ShowMain();
        LoadSettings();
        UpdateBestScore();
    }

    // ── 패널 전환 ──────────────────────────────────────────
    public void ShowMain()
    {
        mainPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
    }

    public void ShowSettings()
    {
        mainPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
    }

    // ── 버튼 핸들러 ───────────────────────────────────────
    public void OnPlayClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── 설정 ──────────────────────────────────────────────
    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        float sfx    = PlayerPrefs.GetFloat(KEY_SFX_VOL,    1f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = master;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfx;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        ApplyVolume(master, sfx);
        UpdateVolumeLabels(master, sfx);
    }

    public void OnMasterVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, val);
        ApplyVolume(val, PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f));
        UpdateVolumeLabels(val, PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f));
    }

    public void OnSFXVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(KEY_SFX_VOL, val);
        UpdateVolumeLabels(PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f), val);
    }

    private void ApplyVolume(float master, float sfx)
    {
        AudioListener.volume = master;
        AudioManager.Instance?.RefreshVolume();
    }

    private void UpdateVolumeLabels(float master, float sfx)
    {
        if (masterVolumeLabel != null) masterVolumeLabel.text = Mathf.RoundToInt(master * 100) + "%";
        if (sfxVolumeLabel    != null) sfxVolumeLabel.text    = Mathf.RoundToInt(sfx    * 100) + "%";
    }

    private void UpdateBestScore()
    {
        int best = PlayerPrefs.GetInt(KEY_BEST_SCORE, 0);
        if (bestScoreText != null)
            bestScoreText.text = best > 0 ? "최고 점수  " + best.ToString("N0") : "";
    }
}
