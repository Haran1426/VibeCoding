using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIScenePolisher
{
    private const string MenuScenePath = "Assets/Scenes/MenuScene/MenuScene.unity";
    private const string ArenaScenePath = "Assets/Scenes/ArenaScene/ArenaScene.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene/GameScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Neon Rewind/Rebuild All Authored UI")]
    public static void RebuildAllAuthoredUI()
    {
        RebuildMenuSceneUI();
        RebuildArenaSceneUI();
        RebuildLegacySceneRedirect(GameScenePath, "Legacy GameScene");
        RebuildLegacySceneRedirect(SampleScenePath, "SampleScene");
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Neon Rewind authored UI rebuilt for MenuScene and ArenaScene.");
    }

    [MenuItem("Tools/Neon Rewind/Configure Release Build Scenes")]
    public static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ArenaScenePath, true)
        };
        Debug.Log("Build Settings configured: MenuScene, ArenaScene.");
    }

    [MenuItem("Tools/Neon Rewind/Rebuild Legacy Scene Redirects")]
    public static void RebuildLegacySceneRedirects()
    {
        RebuildLegacySceneRedirect(GameScenePath, "Legacy GameScene");
        RebuildLegacySceneRedirect(SampleScenePath, "SampleScene");
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Neon Rewind/Rebuild Menu Scene UI")]
    public static void RebuildMenuSceneUI()
    {
        var scene = EditorSceneManager.OpenScene(MenuScenePath);
        RemoveCanvasesAndEventSystems();
        RemoveOnlineReleaseExcludedObjects();
        EnsureMenuCamera();

        var canvasGo = CreateCanvas("Title Canvas", 0);
        var menu = canvasGo.AddComponent<MenuManager>();
        var lobby = canvasGo.AddComponent<NetworkLobbyUI>();

        var main = Panel(canvasGo.transform, "Title_Main", C(0.005f, 0.007f, 0.012f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BuildMenuMain(main.transform, out Button play, out Button settings, out Button quit,
            out TextMeshProUGUI best, out TextMeshProUGUI version);

        var settingsPanel = Panel(canvasGo.transform, "Title_Settings", C(0.005f, 0.007f, 0.012f, 0.98f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BuildSettings(settingsPanel.transform, out Slider master, out Slider sfx,
            out TextMeshProUGUI masterValue, out TextMeshProUGUI sfxValue, out Button settingsBack);
        settingsPanel.SetActive(false);

        var lobbyRoot = Panel(canvasGo.transform, "Title_Lobby", C(0.005f, 0.007f, 0.012f, 0.98f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BuildLobby(lobbyRoot.transform, out GameObject connectPanel, out GameObject waitingPanel,
            out Button host, out TMP_InputField ip, out Button join, out Button connectBack,
            out TextMeshProUGUI connectStatus, out TextMeshProUGUI playerCount,
            out TextMeshProUGUI waitingStatus, out Button start, out Button cancel);
        waitingPanel.SetActive(false);
        lobbyRoot.SetActive(false);

        SetObject(menu, "rebuildTitleSceneAtRuntime", false);
        SetObject(menu, "mainPanel", main);
        SetObject(menu, "settingsPanel", settingsPanel);
        SetObject(menu, "lobbyUI", lobby);
        SetObject(menu, "playMultiButton", play);
        SetObject(menu, "settingsButton", settings);
        SetObject(menu, "quitButton", quit);
        SetObject(menu, "bestScoreText", best);
        SetObject(menu, "versionText", version);
        SetObject(menu, "masterVolumeSlider", master);
        SetObject(menu, "sfxVolumeSlider", sfx);
        SetObject(menu, "masterVolumeLabel", masterValue);
        SetObject(menu, "sfxVolumeLabel", sfxValue);
        SetObject(menu, "settingsBackButton", settingsBack);

        SetObject(lobby, "rebuildLobbyUIAtRuntime", false);
        SetObject(lobby, "lobbyRoot", lobbyRoot);
        SetObject(lobby, "connectPanel", connectPanel);
        SetObject(lobby, "waitingPanel", waitingPanel);
        SetObject(lobby, "hostButton", host);
        SetObject(lobby, "ipInputField", ip);
        SetObject(lobby, "joinButton", join);
        SetObject(lobby, "connectBackButton", connectBack);
        SetObject(lobby, "connectStatusText", connectStatus);
        SetObject(lobby, "playerCountText", playerCount);
        SetObject(lobby, "waitingStatusText", waitingStatus);
        SetObject(lobby, "startButton", start);
        SetObject(lobby, "waitingCancelButton", cancel);

        EnsureEventSystem();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MenuScene authored UI rebuilt.");
    }

    [MenuItem("Tools/Neon Rewind/Rebuild Arena Scene UI")]
    public static void RebuildArenaSceneUI()
    {
        var scene = EditorSceneManager.OpenScene(ArenaScenePath);
        RemoveCanvasesAndEventSystems();
        RemoveOnlineReleaseExcludedObjects();

        var canvasGo = CreateCanvas("Arena HUD Canvas", 100);
        var hud = canvasGo.AddComponent<HUDManager>();
        var scoreboard = canvasGo.AddComponent<ScoreboardUI>();
        var killFeed = canvasGo.AddComponent<KillFeedUI>();
        var pause = canvasGo.AddComponent<PauseManager>();
        var results = canvasGo.AddComponent<ResultsPanel>();

        BuildArenaHud(canvasGo.transform, out TextMeshProUGUI timer, out TextMeshProUGUI knockback, out Image knockbackFill,
            out TextMeshProUGUI clones, out TextMeshProUGUI countdown, out TextMeshProUGUI respawn,
            out TextMeshProUGUI[] feedSlots, out ScoreboardUI.ScoreRow[] rows);
        BuildPause(canvasGo.transform, out GameObject pausePanel, out TextMeshProUGUI pauseTitle,
            out TextMeshProUGUI pauseSubtitle, out Button resumeButton, out Button leaveButton);
        BuildResults(canvasGo.transform, out GameObject resultsPanel, out TextMeshProUGUI resultTitle,
            out TextMeshProUGUI[] rankTexts, out TextMeshProUGUI bestScore, out TextMeshProUGUI hint,
            out Button rematch, out Button title);

        SetObject(hud, "timerText", timer);
        SetObject(hud, "knockbackText", knockback);
        SetObject(hud, "knockbackFill", knockbackFill);
        SetObject(hud, "cloneCountText", clones);
        SetObject(hud, "countdownText", countdown);
        SetObject(hud, "respawnText", respawn);

        SetScoreRows(scoreboard, rows);
        SetObject(killFeed, "feedSlots", feedSlots);

        SetObject(pause, "pausePanel", pausePanel);
        SetObject(pause, "titleText", pauseTitle);
        SetObject(pause, "subtitleText", pauseSubtitle);
        SetObject(pause, "resumeButton", resumeButton);
        SetObject(pause, "menuButton", leaveButton);
        pausePanel.SetActive(false);

        SetObject(results, "panel", resultsPanel);
        SetObject(results, "titleText", resultTitle);
        SetObject(results, "rankTexts", rankTexts);
        SetObject(results, "bestScoreText", bestScore);
        SetObject(results, "hintText", hint);
        SetObject(results, "restartButton", rematch);
        SetObject(results, "menuButton", title);
        resultsPanel.SetActive(false);

        EnsureEventSystem();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ArenaScene authored UI rebuilt.");
    }

    private static void RebuildLegacySceneRedirect(string scenePath, string title)
    {
        var scene = EditorSceneManager.OpenScene(scenePath);
        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(0.005f, 0.007f, 0.012f, 1f);
        camGo.AddComponent<AudioListener>();

        var redirector = new GameObject("SceneRedirector");
        redirector.AddComponent<SceneRedirector>();

        var canvasGo = CreateCanvas("Redirect Canvas", 0);
        Panel(canvasGo.transform, "Background", C(0.005f, 0.007f, 0.012f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Panel(canvasGo.transform, "Cyan_Rail", C(0f, 0.72f, 1f, 0.28f),
            V(0.18f, 0.34f), V(0.182f, 0.66f), Vector2.zero, Vector2.zero);
        Panel(canvasGo.transform, "Magenta_Rail", C(1f, 0.16f, 0.58f, 0.24f),
            V(0.82f, 0.34f), V(0.824f, 0.66f), Vector2.zero, Vector2.zero);
        Text(canvasGo.transform, "Redirect_Title", title.ToUpperInvariant(), 58, TextAlignmentOptions.Center,
            V(0.22f, 0.52f), V(0.78f, 0.62f), Vector2.zero, Vector2.zero, Color.white);
        Text(canvasGo.transform, "Redirect_Subtitle",
            "This scene is not part of the online release flow. Returning to title...",
            24, TextAlignmentOptions.Center, V(0.20f, 0.43f), V(0.80f, 0.49f),
            Vector2.zero, Vector2.zero, C(0.72f, 0.83f, 0.92f, 1f));

        EnsureEventSystem();
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"{title} redirect scene rebuilt.");
    }

    private static void RemoveOnlineReleaseExcludedObjects()
    {
        foreach (var localLobby in Object.FindObjectsByType<LocalLobbyUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(localLobby.gameObject);

        foreach (var localArena in Object.FindObjectsByType<LocalArenaBootstrapper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(localArena.gameObject);
    }

    private static void BuildMenuMain(Transform root, out Button play, out Button settings, out Button quit,
        out TextMeshProUGUI best, out TextMeshProUGUI version)
    {
        Panel(root, "Backdrop_CyanRail", C(0f, 0.72f, 1f, 0.34f), V(0.625f, 0.14f), V(0.630f, 0.88f), Vector2.zero, Vector2.zero);
        Panel(root, "Backdrop_MagentaRail", C(1f, 0.12f, 0.55f, 0.30f), V(0.705f, 0.08f), V(0.711f, 0.78f), Vector2.zero, Vector2.zero);
        Panel(root, "Backdrop_GoldRail", C(1f, 0.78f, 0.10f, 0.24f), V(0.785f, 0.24f), V(0.790f, 0.94f), Vector2.zero, Vector2.zero);

        Text(root, "Kicker", "ONLINE MULTIPLAYER BRAWLER", 24, TextAlignmentOptions.Left,
            V(0.06f, 0.735f), V(0.56f, 0.785f), Vector2.zero, Vector2.zero, C(0.10f, 0.95f, 1f, 1f));
        Text(root, "Logo", "NEON\nREWIND", 118, TextAlignmentOptions.Left,
            V(0.06f, 0.49f), V(0.62f, 0.72f), Vector2.zero, Vector2.zero, Color.white);
        Text(root, "Subtitle", "Every knockout leaves a replay clone behind. Survive your own past and outscore the arena.",
            28, TextAlignmentOptions.Left, V(0.06f, 0.38f), V(0.57f, 0.46f), Vector2.zero, Vector2.zero, C(0.72f, 0.83f, 0.92f, 1f));

        BuildRuleStrip(root);
        play = Button(root, "PlayOnline_Button", "HOST / JOIN ONLINE", V(0.06f, 0.255f), V(390f, 64f), V(195f, 0f));
        settings = Button(root, "Settings_Button", "SETTINGS", V(0.06f, 0.165f), V(190f, 56f), V(95f, 0f));
        quit = Button(root, "Quit_Button", "QUIT", V(0.18f, 0.165f), V(150f, 56f), V(75f, 0f));

        var preview = Panel(root, "Arena_Preview", C(0.03f, 0.04f, 0.06f, 0.92f), V(0.62f, 0.23f), V(0.92f, 0.72f), Vector2.zero, Vector2.zero);
        Panel(preview.transform, "Platform", C(0f, 0.62f, 0.95f, 0.70f), V(0.12f, 0.42f), V(0.88f, 0.49f), Vector2.zero, Vector2.zero);
        Panel(preview.transform, "SpinnerHazard", C(1f, 0.32f, 0.08f, 0.82f), V(0.30f, 0.53f), V(0.70f, 0.57f), Vector2.zero, Vector2.zero);
        Panel(preview.transform, "JumpPad_Left", C(1f, 0.82f, 0.10f, 0.78f), V(0.15f, 0.31f), V(0.33f, 0.36f), Vector2.zero, Vector2.zero);
        Panel(preview.transform, "JumpPad_Right", C(0.55f, 0.25f, 1f, 0.78f), V(0.67f, 0.66f), V(0.85f, 0.71f), Vector2.zero, Vector2.zero);
        Panel(preview.transform, "Player_Cyan", C(0f, 0.78f, 1f, 0.95f), V(0.28f, 0.59f), V(0.34f, 0.73f), Vector2.zero, Vector2.zero);
        Panel(preview.transform, "Player_Magenta", C(1f, 0.18f, 0.58f, 0.95f), V(0.66f, 0.31f), V(0.72f, 0.45f), Vector2.zero, Vector2.zero);
        Text(preview.transform, "Preview_Label", "4 PLAYER ONLINE ARENA", 22, TextAlignmentOptions.Center,
            V(0f, 0.08f), V(1f, 0.16f), Vector2.zero, Vector2.zero, C(0.8f, 0.92f, 1f, 1f));
        Text(preview.transform, "Preview_Rule", "KNOCKOUTS CREATE REPLAY CLONES", 17, TextAlignmentOptions.Center,
            V(0.08f, 0.01f), V(0.92f, 0.07f), Vector2.zero, Vector2.zero, C(1f, 0.82f, 0.18f, 1f));

        best = Text(root, "BestScore", "", 24, TextAlignmentOptions.Right,
            V(0.72f, 0.06f), V(0.94f, 0.11f), Vector2.zero, Vector2.zero, C(1f, 0.82f, 0.18f, 1f));
        version = Text(root, "Version", "ALPHA 0.1.0  |  16:9 ONLINE BUILD", 18, TextAlignmentOptions.Left,
            V(0.06f, 0.06f), V(0.45f, 0.10f), Vector2.zero, Vector2.zero, C(0.45f, 0.55f, 0.66f, 1f));
    }

    private static void BuildRuleStrip(Transform root)
    {
        var strip = Panel(root, "Rules_Strip", C(0.025f, 0.035f, 0.05f, 0.72f),
            V(0.06f, 0.315f), V(0.57f, 0.365f), Vector2.zero, Vector2.zero);
        Text(strip.transform, "Rules_Label", "WIN CONDITION", 17, TextAlignmentOptions.Left,
            V(0.035f, 0f), V(0.22f, 1f), Vector2.zero, Vector2.zero, C(0.1f, 0.95f, 1f, 1f));
        Text(strip.transform, "Rules_Text", "Hit rivals, ring them out, then survive the replay clones they leave behind.",
            20, TextAlignmentOptions.Left, V(0.23f, 0f), V(0.98f, 1f), Vector2.zero, Vector2.zero, Color.white);
    }

    private static void BuildSettings(Transform root, out Slider master, out Slider sfx,
        out TextMeshProUGUI masterValue, out TextMeshProUGUI sfxValue, out Button back)
    {
        Text(root, "Settings_Title", "SETTINGS", 74, TextAlignmentOptions.Left,
            V(0.12f, 0.72f), V(0.52f, 0.84f), Vector2.zero, Vector2.zero, Color.white);
        Text(root, "Settings_Master_Label", "MASTER VOLUME", 26, TextAlignmentOptions.Left,
            V(0.18f, 0.58f), V(0.44f, 0.63f), Vector2.zero, Vector2.zero, Color.white);
        Text(root, "Settings_Sfx_Label", "SFX VOLUME", 26, TextAlignmentOptions.Left,
            V(0.18f, 0.46f), V(0.44f, 0.51f), Vector2.zero, Vector2.zero, Color.white);
        masterValue = Text(root, "Settings_Master_Value", "80%", 28, TextAlignmentOptions.Right,
            V(0.56f, 0.58f), V(0.68f, 0.63f), Vector2.zero, Vector2.zero, C(0.1f, 0.95f, 1f, 1f));
        sfxValue = Text(root, "Settings_Sfx_Value", "80%", 28, TextAlignmentOptions.Right,
            V(0.56f, 0.46f), V(0.68f, 0.51f), Vector2.zero, Vector2.zero, C(0.1f, 0.95f, 1f, 1f));
        master = Slider(root, "Settings_Master_Slider", V(0.18f, 0.54f), V(0.68f, 0.57f));
        sfx = Slider(root, "Settings_Sfx_Slider", V(0.18f, 0.42f), V(0.68f, 0.45f));
        back = Button(root, "Settings_Back_Button", "BACK", V(0.18f, 0.25f), V(220f, 58f), V(110f, 0f));
    }

    private static void BuildLobby(Transform root, out GameObject connect, out GameObject waiting,
        out Button host, out TMP_InputField ip, out Button join, out Button back,
        out TextMeshProUGUI status, out TextMeshProUGUI playerCount, out TextMeshProUGUI waitingStatus,
        out Button start, out Button cancel)
    {
        Panel(root, "Lobby_CyanRail", C(0f, 0.72f, 1f, 0.25f), V(0.05f, 0.12f), V(0.055f, 0.88f), Vector2.zero, Vector2.zero);
        Panel(root, "Lobby_MagentaRail", C(1f, 0.16f, 0.58f, 0.24f), V(0.93f, 0.12f), V(0.936f, 0.88f), Vector2.zero, Vector2.zero);
        Text(root, "Lobby_Title", "ONLINE LOBBY", 72, TextAlignmentOptions.Left,
            V(0.09f, 0.76f), V(0.56f, 0.86f), Vector2.zero, Vector2.zero, Color.white);
        Text(root, "Lobby_Subtitle", "Host a room, or join a host by IP. Online only.",
            26, TextAlignmentOptions.Left, V(0.09f, 0.70f), V(0.62f, 0.75f), Vector2.zero, Vector2.zero, C(0.72f, 0.83f, 0.92f, 1f));
        Text(root, "Lobby_Info", "Host controls match start. Clients stay in the waiting room until arena load.",
            20, TextAlignmentOptions.Left, V(0.09f, 0.655f), V(0.66f, 0.69f), Vector2.zero, Vector2.zero, C(1f, 0.82f, 0.18f, 1f));

        connect = Panel(root, "Connect_Panel", C(0.025f, 0.035f, 0.05f, 0.90f), V(0.09f, 0.20f), V(0.58f, 0.64f), Vector2.zero, Vector2.zero);
        Text(connect.transform, "Connect_Title", "CONNECT", 38, TextAlignmentOptions.Left, V(0.08f, 0.78f), V(0.92f, 0.92f), Vector2.zero, Vector2.zero, Color.white);
        Text(connect.transform, "Connect_Hint", "LAN / direct IP", 18, TextAlignmentOptions.Right,
            V(0.58f, 0.79f), V(0.92f, 0.90f), Vector2.zero, Vector2.zero, C(0.1f, 0.95f, 1f, 1f));
        host = Button(connect.transform, "Host_Button", "HOST", V(0.08f, 0.57f), V(220f, 62f), V(110f, 0f));
        ip = Input(connect.transform, "Ip_Input", "127.0.0.1", V(0.08f, 0.38f), V(0.62f, 0.52f));
        join = Button(connect.transform, "Join_Button", "JOIN", V(0.68f, 0.45f), V(170f, 62f), V(85f, 0f));
        status = Text(connect.transform, "Connect_Status", "Start a host or join by IP.", 24, TextAlignmentOptions.Left,
            V(0.08f, 0.22f), V(0.88f, 0.32f), Vector2.zero, Vector2.zero, C(0.72f, 0.83f, 0.92f, 1f));
        back = Button(connect.transform, "Connect_Back_Button", "BACK", V(0.08f, 0.10f), V(160f, 54f), V(80f, 0f));

        waiting = Panel(root, "Waiting_Panel", C(0.025f, 0.035f, 0.05f, 0.90f), V(0.09f, 0.20f), V(0.58f, 0.64f), Vector2.zero, Vector2.zero);
        Text(waiting.transform, "Waiting_Title", "WAITING ROOM", 38, TextAlignmentOptions.Left, V(0.08f, 0.78f), V(0.92f, 0.92f), Vector2.zero, Vector2.zero, Color.white);
        playerCount = Text(waiting.transform, "Waiting_Count", "PLAYERS  1 / 4", 42, TextAlignmentOptions.Left,
            V(0.08f, 0.58f), V(0.88f, 0.70f), Vector2.zero, Vector2.zero, C(0.1f, 0.95f, 1f, 1f));
        waitingStatus = Text(waiting.transform, "Waiting_Status", "Waiting for players...", 24, TextAlignmentOptions.Left,
            V(0.08f, 0.42f), V(0.88f, 0.52f), Vector2.zero, Vector2.zero, C(0.72f, 0.83f, 0.92f, 1f));
        start = Button(waiting.transform, "Start_Button", "START MATCH", V(0.08f, 0.22f), V(260f, 62f), V(130f, 0f));
        cancel = Button(waiting.transform, "Cancel_Button", "CANCEL", V(0.08f, 0.10f), V(180f, 54f), V(90f, 0f));

        var side = Panel(root, "Lobby_SidePreview", C(0.03f, 0.04f, 0.06f, 0.94f), V(0.65f, 0.20f), V(0.90f, 0.64f), Vector2.zero, Vector2.zero);
        Panel(side.transform, "MapBar_A", C(0f, 0.72f, 1f, 0.68f), V(0.12f, 0.56f), V(0.88f, 0.62f), Vector2.zero, Vector2.zero);
        Panel(side.transform, "MapBar_B", C(1f, 0.18f, 0.56f, 0.70f), V(0.22f, 0.36f), V(0.78f, 0.41f), Vector2.zero, Vector2.zero);
        Text(side.transform, "Map_Label", "NEON ARENA\nUP TO 4 PLAYERS", 30, TextAlignmentOptions.Center,
            V(0.08f, 0.12f), V(0.92f, 0.28f), Vector2.zero, Vector2.zero, Color.white);
        Text(side.transform, "Map_Rules", "RINGOUT +5\nHIT +1\nCLONE +1", 22, TextAlignmentOptions.Center,
            V(0.08f, 0.72f), V(0.92f, 0.92f), Vector2.zero, Vector2.zero, C(1f, 0.82f, 0.18f, 1f));
    }

    private static void BuildArenaHud(Transform root, out TextMeshProUGUI timer, out TextMeshProUGUI knockback, out Image knockbackFill,
        out TextMeshProUGUI clones, out TextMeshProUGUI countdown, out TextMeshProUGUI respawn,
        out TextMeshProUGUI[] feedSlots, out ScoreboardUI.ScoreRow[] rows)
    {
        RoundPanel(root, "Top_Timer_Backplate", C(1f, 0.28f, 0.55f, 0.96f), V(0.5f, 1f), V(0.5f, 1f), V(0f, -48f), V(300f, 76f), V(0.5f, 1f), 34f, C(1f, 1f, 1f, 0.95f));
        Text(root, "Timer_Label", "TIME", 16, TextAlignmentOptions.Center,
            V(0.5f, 1f), V(0.5f, 1f), V(0f, -18f), V(220f, 24f), C(0.1f, 0.95f, 1f, 1f), V(0.5f, 1f));
        timer = Text(root, "Timer_Text", "02:00", 38, TextAlignmentOptions.Center,
            V(0.5f, 1f), V(0.5f, 1f), V(0f, -46f), V(220f, 52f), Color.white, V(0.5f, 1f));

        RoundPanel(root, "Player_Status_Backplate", C(0.05f, 0.66f, 1f, 0.94f), V(0f, 0f), V(0f, 0f), V(184f, 92f), V(320f, 128f), null, 38f, C(1f, 1f, 1f, 0.95f));
        Text(root, "Knockback_Label", "DANGER", 16, TextAlignmentOptions.Left,
            V(0f, 0f), V(0f, 0f), V(46f, 116f), V(160f, 22f), C(1f, 0.82f, 0.18f, 1f));
        knockback = Text(root, "Knockback_Text", "0%", 48, TextAlignmentOptions.Left,
            V(0f, 0f), V(0f, 0f), V(46f, 56f), V(180f, 56f), Color.white);
        RoundPanel(root, "Knockback_Bar_Back", C(0.15f, 0.08f, 0.28f, 0.90f),
            V(0f, 0f), V(0f, 0f), V(174f, 36f), V(244f, 12f));
        knockbackFill = Panel(root, "Knockback_Bar_Fill", C(0.1f, 0.95f, 1f, 1f),
            V(0f, 0f), V(0f, 0f), V(52f, 36f), V(244f, 12f), V(0f, 0.5f)).GetComponent<Image>();
        knockbackFill.type = Image.Type.Filled;
        knockbackFill.fillMethod = Image.FillMethod.Horizontal;
        knockbackFill.fillOrigin = 0;
        knockbackFill.fillAmount = 0f;
        clones = Text(root, "Clone_Count_Text", "CLONES  0", 24, TextAlignmentOptions.Left,
            V(0f, 0f), V(0f, 0f), V(46f, 94f), V(220f, 34f), C(0.2f, 0.9f, 1f, 1f));

        var board = RoundPanel(root, "Scoreboard_Panel", C(0.72f, 0.22f, 1f, 0.92f), V(1f, 1f), V(1f, 1f), V(-36f, -36f), V(294f, 174f), V(1f, 1f), 34f, C(1f, 1f, 1f, 0.95f));
        Text(board.transform, "Scoreboard_Header", "SCORE", 16, TextAlignmentOptions.Left,
            V(0f, 1f), V(1f, 1f), V(18f, -2f), V(-36f, 22f), C(0.1f, 0.95f, 1f, 1f), V(0.5f, 1f));
        rows = new ScoreboardUI.ScoreRow[4];
        Color[] colors = { C(0f, 0.78f, 1f, 1f), C(1f, 0.18f, 0.58f, 1f), C(0.6f, 0.25f, 1f, 1f), C(1f, 0.82f, 0.1f, 1f) };
        for (int i = 0; i < rows.Length; i++)
        {
            var rowRoot = RectObject(board.transform, $"Score_Row_{i + 1}", V(0f, 1f), V(1f, 1f), V(0f, -30f - i * 30f), V(-22f, 28f), V(0.5f, 1f));
            var dot = Panel(rowRoot.transform, "Color_Dot", colors[i], V(0f, 0.5f), V(0f, 0.5f), V(14f, 0f), V(12f, 12f)).GetComponent<Image>();
            var name = Text(rowRoot.transform, "Name_Text", i == 0 ? "YOU" : $"P{i + 1}", 20, TextAlignmentOptions.Left,
                V(0f, 0f), V(1f, 1f), V(58f, 0f), V(-86f, 0f), Color.white);
            var score = Text(rowRoot.transform, "Score_Text", "0", 22, TextAlignmentOptions.Right,
                V(1f, 0f), V(1f, 1f), V(-22f, 0f), V(58f, 0f), Color.white);
            rows[i] = new ScoreboardUI.ScoreRow { root = rowRoot, colorDot = dot, nameText = name, scoreText = score };
        }

        feedSlots = new TextMeshProUGUI[4];
        for (int i = 0; i < feedSlots.Length; i++)
            feedSlots[i] = Text(root, $"KillFeed_{i + 1}", "", 23, TextAlignmentOptions.Right,
                V(1f, 1f), V(1f, 1f), V(-36f, -220f - i * 32f), V(520f, 30f), Color.white, V(1f, 1f));

        countdown = Text(root, "Countdown_Text", "", 96, TextAlignmentOptions.Center,
            V(0.5f, 0.5f), V(0.5f, 0.5f), Vector2.zero, V(560f, 130f), C(0f, 0.9f, 1f, 1f));
        countdown.gameObject.SetActive(false);
        respawn = Text(root, "Respawn_Text", "", 34, TextAlignmentOptions.Center,
            V(0.5f, 0.18f), V(0.5f, 0.18f), Vector2.zero, V(420f, 60f), C(1f, 0.85f, 0.2f, 1f));
        respawn.gameObject.SetActive(false);
        Text(root, "Esc_Hint", "ESC", 18, TextAlignmentOptions.Right,
            V(0.88f, 0.02f), V(0.97f, 0.06f), Vector2.zero, Vector2.zero, C(0.45f, 0.55f, 0.66f, 1f));
    }

    private static void BuildPause(Transform root, out GameObject panel, out TextMeshProUGUI title,
        out TextMeshProUGUI subtitle, out Button resume, out Button leave)
    {
        panel = Panel(root, "Pause_Backdrop", C(0.03f, 0.02f, 0.08f, 0.74f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var modal = RoundPanel(panel.transform, "Pause_Panel", C(0.95f, 0.26f, 0.56f, 0.96f),
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, 26f), V(620f, 390f), V(0.5f, 0.5f), 52f, C(1f, 1f, 1f, 0.98f));
        title = Text(panel.transform, "Pause_Title", "MATCH MENU", 62, TextAlignmentOptions.Center,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, 120f), V(460f, 80f), C(0f, 0.9f, 1f, 1f));
        subtitle = Text(panel.transform, "Pause_Subtitle", "Online match keeps running while this menu is open.", 24, TextAlignmentOptions.Center,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, 68f), V(720f, 42f), C(0.72f, 0.83f, 0.92f, 1f));
        resume = Button(panel.transform, "Resume_Button", "RESUME", V(0.5f, 0.5f), V(260f, 58f), V(0f, 20f));
        leave = Button(panel.transform, "Leave_Button", "LEAVE MATCH", V(0.5f, 0.5f), V(260f, 58f), V(0f, -58f));
    }

    private static void BuildResults(Transform root, out GameObject panel, out TextMeshProUGUI title,
        out TextMeshProUGUI[] ranks, out TextMeshProUGUI best, out TextMeshProUGUI hint,
        out Button rematch, out Button titleButton)
    {
        panel = Panel(root, "Results_Backdrop", C(0.03f, 0.02f, 0.08f, 0.78f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RoundPanel(panel.transform, "Results_Panel", C(0.08f, 0.72f, 1f, 0.96f),
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, 12f), V(680f, 460f), V(0.5f, 0.5f), 54f, C(1f, 1f, 1f, 0.98f));
        title = Text(panel.transform, "Results_Title", "MATCH END", 64, TextAlignmentOptions.Center,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, 184f), V(540f, 80f), C(0f, 0.9f, 1f, 1f));
        ranks = new TextMeshProUGUI[4];
        for (int i = 0; i < ranks.Length; i++)
            ranks[i] = Text(panel.transform, $"Rank_{i + 1}", "", 34, TextAlignmentOptions.Center,
                V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, 94f - i * 44f), V(560f, 42f), Color.white);
        best = Text(panel.transform, "Best_Score", "", 26, TextAlignmentOptions.Center,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, -108f), V(400f, 40f), C(1f, 0.85f, 0.2f, 1f));
        hint = Text(panel.transform, "Results_Hint", "", 20, TextAlignmentOptions.Center,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0f, -142f), V(620f, 32f), C(0.72f, 0.83f, 0.92f, 1f));
        rematch = Button(panel.transform, "Rematch_Button", "REMATCH", V(0.5f, 0.5f), V(240f, 58f), V(-145f, -176f));
        titleButton = Button(panel.transform, "Title_Button", "TITLE", V(0.5f, 0.5f), V(240f, 58f), V(145f, -176f));
    }

    private static GameObject CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = V(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        var go = RectObject(parent, name, min, max, pos, size, pivot);
        var image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private static GameObject RoundPanel(Transform parent, string name, Color color, Vector2 min, Vector2 max,
        Vector2 pos, Vector2 size, Vector2? pivot = null, float radius = 28f, Color? outlineColor = null)
    {
        var go = RectObject(parent, name, min, max, pos, size, pivot);
        var rounded = go.AddComponent<RoundedRectGraphic>();
        rounded.color = color;
        rounded.Radius = radius;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = C(0.03f, 0.01f, 0.08f, 0.55f);
        shadow.effectDistance = V(8f, -8f);

        var outline = go.AddComponent<Outline>();
        outline.effectColor = outlineColor ?? C(1f, 1f, 1f, 0.82f);
        outline.effectDistance = V(4f, -4f);
        return go;
    }

    private static GameObject RectObject(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        if (pivot.HasValue) rect.pivot = pivot.Value;
        return go;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string value, int size, TextAlignmentOptions alignment,
        Vector2 min, Vector2 max, Vector2 pos, Vector2 rectSize, Color color, Vector2? pivot = null)
    {
        var go = RectObject(parent, name, min, max, pos, rectSize, pivot);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = C(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = V(2f, -2f);
        return text;
    }

    private static Button Button(Transform parent, string name, string label, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        var go = RoundPanel(parent, name, C(1f, 0.78f, 0.08f, 0.98f), anchor, anchor, pos, size, V(0.5f, 0.5f), 26f, C(1f, 1f, 1f, 0.94f));
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<RoundedRectGraphic>();
        var colors = button.colors;
        colors.normalColor = C(1f, 0.78f, 0.08f, 0.98f);
        colors.highlightedColor = C(0.18f, 0.88f, 1f, 1f);
        colors.pressedColor = C(1f, 0.38f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = C(0.45f, 0.42f, 0.50f, 0.62f);
        button.colors = colors;
        RoundPanel(go.transform, "Accent", C(1f, 0.3f, 0.62f, 0.92f), V(0.02f, 0.16f), V(0.12f, 0.84f), Vector2.zero, Vector2.zero, null, 12f, C(1f, 1f, 1f, 0.0f));
        Text(go.transform, "Label", label, 28, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        return button;
    }

    private static Slider Slider(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var track = Panel(parent, name, C(0.07f, 0.10f, 0.14f, 0.95f), min, max, Vector2.zero, Vector2.zero);
        var fill = Panel(track.transform, "Fill", C(0f, 0.75f, 1f, 0.95f), Vector2.zero, V(0.8f, 1f), Vector2.zero, Vector2.zero);
        var handle = Panel(track.transform, "Handle", Color.white, V(0.8f, 0.5f), V(0.8f, 0.5f), Vector2.zero, V(18f, 34f));
        var slider = track.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        slider.fillRect = fill.transform as RectTransform;
        slider.handleRect = handle.transform as RectTransform;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static TMP_InputField Input(Transform parent, string name, string value, Vector2 min, Vector2 max)
    {
        var root = Panel(parent, name, C(0.06f, 0.08f, 0.11f, 0.96f), min, max, Vector2.zero, Vector2.zero);
        var input = root.AddComponent<TMP_InputField>();
        input.textComponent = Text(root.transform, "Text", value, 28, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        input.placeholder = Text(root.transform, "Placeholder", value, 28, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, C(0.6f, 0.68f, 0.76f, 0.8f));
        input.text = value;
        input.characterLimit = 32;
        return input;
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"Missing serialized property {propertyName} on {target.name}");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string propertyName, bool value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propertyName);
        if (prop == null) return;
        prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string propertyName, TextMeshProUGUI[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(propertyName);
        if (prop == null) return;
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetScoreRows(ScoreboardUI scoreboard, ScoreboardUI.ScoreRow[] rows)
    {
        var so = new SerializedObject(scoreboard);
        var prop = so.FindProperty("rows");
        if (prop == null) return;
        prop.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = prop.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("root").objectReferenceValue = rows[i].root;
            row.FindPropertyRelative("colorDot").objectReferenceValue = rows[i].colorDot;
            row.FindPropertyRelative("nameText").objectReferenceValue = rows[i].nameText;
            row.FindPropertyRelative("scoreText").objectReferenceValue = rows[i].scoreText;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveCanvasesAndEventSystems()
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(canvas.gameObject);
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(es.gameObject);

        string[] legacyNames =
        {
            "Title Canvas Authored",
            "Title_Main_Runtime",
            "Title_Settings_Runtime",
            "Title_Lobby_Runtime",
            "HUD Canvas",
            "Arena Runtime Canvas"
        };

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid()) continue;
            foreach (string legacyName in legacyNames)
            {
                if (go.name != legacyName) continue;
                Object.DestroyImmediate(go);
                break;
            }
        }
    }

    private static void EnsureEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private static void EnsureMenuCamera()
    {
        var existing = GameObject.FindWithTag("MainCamera");
        if (existing != null) return;

        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(0.005f, 0.007f, 0.012f, 1f);
        go.AddComponent<AudioListener>();
    }

    private static Color C(float r, float g, float b, float a) => new Color(r, g, b, a);
    private static Vector2 V(float x, float y) => new Vector2(x, y);
}
