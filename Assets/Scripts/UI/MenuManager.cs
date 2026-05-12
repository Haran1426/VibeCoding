using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 타이틀 씬 전체 패널 흐름을 관리합니다.
/// 싱글플레이 / 멀티플레이 / 설정 / 종료
/// </summary>
[ExecuteAlways]
public class MenuManager : MonoBehaviour
{
    [Header("Runtime title scene")]
    [SerializeField] private bool rebuildTitleSceneAtRuntime = false;

    // ── 패널 ─────────────────────────────────────────────────
    [Header("패널")]
    [SerializeField] private GameObject    mainPanel;
    [SerializeField] private GameObject    settingsPanel;
    [SerializeField] private NetworkLobbyUI lobbyUI;    // 네트워크 멀티 로비

    // ── 메인 패널 ─────────────────────────────────────────────
    [Header("메인 버튼")]
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
        if (rebuildTitleSceneAtRuntime)
            EnsureRuntimeTitleScene();

        playMultiButton?.onClick.AddListener(OnMultiPlay);
        settingsButton?.onClick.AddListener(ShowSettings);
        settingsBackButton?.onClick.AddListener(ShowMain);
        quitButton?.onClick.AddListener(OnQuit);

        masterVolumeSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnEnable()
    {
        if (Application.isPlaying || !rebuildTitleSceneAtRuntime) return;
        EnsureRuntimeTitleScene();
    }

    void Start()
    {
        if (Screen.width < Screen.height)
            Screen.SetResolution(1280, 720, false);

        ShowMain();
        LoadSettings();
        RefreshBestScore();

        if (versionText != null) versionText.text = "ALPHA 0.1.0  |  16:9 ONLINE BUILD";
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

    private void EnsureRuntimeTitleScene()
    {
        Canvas canvas = RuntimeUIFactory.EnsureCanvas("Title Canvas");
        HideOtherTitleCanvases(canvas, this);
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        RemoveGeneratedTitlePanels(canvas.transform);
        HideExistingScenePanels(canvas.transform);

        mainPanel = RuntimeUIFactory.CreatePanel(canvas.transform, "Title_Main_Runtime",
            new Color(0.005f, 0.007f, 0.012f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CreateTitleBackdrop(mainPanel.transform);

        RuntimeUIFactory.CreateText(mainPanel.transform, "Title_Kicker", "ONLINE MULTIPLAYER BRAWLER", 24,
            TextAlignmentOptions.Left, new Vector2(0.06f, 0.72f), new Vector2(0.56f, 0.72f),
            Vector2.zero, new Vector2(0f, 40f), new Color(0.1f, 0.95f, 1f));

        RuntimeUIFactory.CreateText(mainPanel.transform, "Title_Logo", "NEON\nREWIND", 118,
            TextAlignmentOptions.Left, new Vector2(0.06f, 0.47f), new Vector2(0.62f, 0.71f),
            Vector2.zero, Vector2.zero, Color.white);

        RuntimeUIFactory.CreateText(mainPanel.transform, "Title_Subtitle",
            "Every knockout leaves a replay clone behind. Survive your own past and outscore the arena.",
            28, TextAlignmentOptions.Left, new Vector2(0.06f, 0.38f), new Vector2(0.57f, 0.46f),
            Vector2.zero, Vector2.zero, new Color(0.72f, 0.83f, 0.92f));

        playMultiButton = RuntimeUIFactory.CreateButton(mainPanel.transform, "Title_PlayOnline", "HOST / JOIN ONLINE",
            new Vector2(0f, 0f), new Vector2(390f, 64f));
        SetRect(playMultiButton.transform as RectTransform, new Vector2(0.06f, 0.26f), new Vector2(0.06f, 0.26f),
            new Vector2(195f, 0f), new Vector2(390f, 64f), new Vector2(0.5f, 0.5f));

        settingsButton = RuntimeUIFactory.CreateButton(mainPanel.transform, "Title_Settings", "SETTINGS",
            Vector2.zero, new Vector2(190f, 56f));
        SetRect(settingsButton.transform as RectTransform, new Vector2(0.06f, 0.17f), new Vector2(0.06f, 0.17f),
            new Vector2(95f, 0f), new Vector2(190f, 56f), new Vector2(0.5f, 0.5f));

        quitButton = RuntimeUIFactory.CreateButton(mainPanel.transform, "Title_Quit", "QUIT",
            Vector2.zero, new Vector2(150f, 56f));
        SetRect(quitButton.transform as RectTransform, new Vector2(0.18f, 0.17f), new Vector2(0.18f, 0.17f),
            new Vector2(75f, 0f), new Vector2(150f, 56f), new Vector2(0.5f, 0.5f));

        bestScoreText = RuntimeUIFactory.CreateText(mainPanel.transform, "Title_BestScore", "", 24,
            TextAlignmentOptions.Right, new Vector2(0.72f, 0.06f), new Vector2(0.94f, 0.11f),
            Vector2.zero, Vector2.zero, new Color(1f, 0.82f, 0.18f));

        versionText = RuntimeUIFactory.CreateText(mainPanel.transform, "Title_Version", "ALPHA 0.1.0  |  16:9 ONLINE BUILD", 18,
            TextAlignmentOptions.Left, new Vector2(0.06f, 0.06f), new Vector2(0.45f, 0.1f),
            Vector2.zero, Vector2.zero, new Color(0.45f, 0.55f, 0.66f));

        settingsPanel = RuntimeUIFactory.CreatePanel(canvas.transform, "Title_Settings_Runtime",
            new Color(0.005f, 0.007f, 0.012f, 0.98f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateSettingsRuntimeUI(settingsPanel.transform);
        settingsPanel.SetActive(false);
    }

    private void CreateTitleBackdrop(Transform root)
    {
        RuntimeUIFactory.CreatePanel(root, "Title_CyanRail", new Color(0f, 0.7f, 1f, 0.34f),
            new Vector2(0.62f, 0.16f), new Vector2(0.625f, 0.86f), Vector2.zero, Vector2.zero);
        RuntimeUIFactory.CreatePanel(root, "Title_MagentaRail", new Color(1f, 0.12f, 0.55f, 0.30f),
            new Vector2(0.70f, 0.08f), new Vector2(0.706f, 0.78f), Vector2.zero, Vector2.zero);
        RuntimeUIFactory.CreatePanel(root, "Title_GoldRail", new Color(1f, 0.78f, 0.1f, 0.24f),
            new Vector2(0.78f, 0.24f), new Vector2(0.785f, 0.94f), Vector2.zero, Vector2.zero);

        var arena = RuntimeUIFactory.CreatePanel(root, "Title_ArenaPreview", new Color(0.03f, 0.04f, 0.06f, 0.92f),
            new Vector2(0.62f, 0.24f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);

        RuntimeUIFactory.CreatePanel(arena.transform, "Preview_Platform", new Color(0.0f, 0.62f, 0.95f, 0.7f),
            new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.48f), Vector2.zero, Vector2.zero);
        RuntimeUIFactory.CreatePanel(arena.transform, "Preview_Spinner", new Color(1f, 0.32f, 0.08f, 0.8f),
            new Vector2(0.32f, 0.52f), new Vector2(0.68f, 0.56f), Vector2.zero, Vector2.zero);
        RuntimeUIFactory.CreatePanel(arena.transform, "Preview_PlayerA", new Color(0f, 0.78f, 1f, 0.9f),
            new Vector2(0.28f, 0.58f), new Vector2(0.34f, 0.72f), Vector2.zero, Vector2.zero);
        RuntimeUIFactory.CreatePanel(arena.transform, "Preview_PlayerB", new Color(1f, 0.18f, 0.58f, 0.9f),
            new Vector2(0.66f, 0.32f), new Vector2(0.72f, 0.46f), Vector2.zero, Vector2.zero);

        RuntimeUIFactory.CreateText(arena.transform, "Preview_Label", "4 PLAYER ONLINE ARENA", 22,
            TextAlignmentOptions.Center, new Vector2(0f, 0.08f), new Vector2(1f, 0.16f),
            Vector2.zero, Vector2.zero, new Color(0.8f, 0.92f, 1f));
    }

    private void CreateSettingsRuntimeUI(Transform root)
    {
        RuntimeUIFactory.CreateText(root, "Settings_Title", "SETTINGS", 74,
            TextAlignmentOptions.Left, new Vector2(0.12f, 0.72f), new Vector2(0.5f, 0.82f),
            Vector2.zero, Vector2.zero, Color.white);

        masterVolumeLabel = RuntimeUIFactory.CreateText(root, "Settings_MasterValue", "80%", 28,
            TextAlignmentOptions.Right, new Vector2(0.56f, 0.58f), new Vector2(0.68f, 0.63f),
            Vector2.zero, Vector2.zero, new Color(0.1f, 0.95f, 1f));
        sfxVolumeLabel = RuntimeUIFactory.CreateText(root, "Settings_SfxValue", "80%", 28,
            TextAlignmentOptions.Right, new Vector2(0.56f, 0.46f), new Vector2(0.68f, 0.51f),
            Vector2.zero, Vector2.zero, new Color(0.1f, 0.95f, 1f));

        RuntimeUIFactory.CreateText(root, "Settings_MasterLabel", "MASTER VOLUME", 26,
            TextAlignmentOptions.Left, new Vector2(0.18f, 0.58f), new Vector2(0.44f, 0.63f),
            Vector2.zero, Vector2.zero, Color.white);
        RuntimeUIFactory.CreateText(root, "Settings_SfxLabel", "SFX VOLUME", 26,
            TextAlignmentOptions.Left, new Vector2(0.18f, 0.46f), new Vector2(0.44f, 0.51f),
            Vector2.zero, Vector2.zero, Color.white);

        masterVolumeSlider = CreateSlider(root, "Settings_MasterSlider", new Vector2(0.18f, 0.54f), new Vector2(0.68f, 0.57f));
        sfxVolumeSlider = CreateSlider(root, "Settings_SfxSlider", new Vector2(0.18f, 0.42f), new Vector2(0.68f, 0.45f));

        settingsBackButton = RuntimeUIFactory.CreateButton(root, "Settings_Back", "BACK",
            Vector2.zero, new Vector2(220f, 58f));
        SetRect(settingsBackButton.transform as RectTransform, new Vector2(0.18f, 0.25f), new Vector2(0.18f, 0.25f),
            new Vector2(110f, 0f), new Vector2(220f, 58f), new Vector2(0.5f, 0.5f));
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var track = RuntimeUIFactory.CreatePanel(parent, name, new Color(0.07f, 0.1f, 0.14f, 0.95f),
            anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        var fill = RuntimeUIFactory.CreatePanel(track.transform, "Fill", new Color(0f, 0.75f, 1f, 0.95f),
            Vector2.zero, new Vector2(0.8f, 1f), Vector2.zero, Vector2.zero);
        var handle = RuntimeUIFactory.CreatePanel(track.transform, "Handle", Color.white,
            new Vector2(0.8f, 0.5f), new Vector2(0.8f, 0.5f), Vector2.zero, new Vector2(18f, 34f));

        var slider = track.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        slider.fillRect = fill.transform as RectTransform;
        slider.handleRect = handle.transform as RectTransform;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
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

    private static void HideExistingScenePanels(Transform canvasRoot)
    {
        foreach (Transform child in canvasRoot)
        {
            if (child.name.StartsWith("Title_")) continue;
            if (child.name.StartsWith("EventSystem")) continue;
            child.gameObject.SetActive(false);
        }
    }

    private static void RemoveGeneratedTitlePanels(Transform canvasRoot)
    {
        for (int i = canvasRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasRoot.GetChild(i);
            if (!child.name.StartsWith("Title_")) continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private static void HideOtherTitleCanvases(Canvas activeCanvas, MenuManager owner)
    {
        if (activeCanvas == null) return;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas == null || canvas == activeCanvas) continue;
            if (owner != null && canvas.GetComponentInChildren<MenuManager>(true) == owner) continue;
            if (!canvas.name.Contains("Title") && !canvas.name.Contains("Menu")) continue;
            canvas.gameObject.SetActive(false);
        }
    }
}
