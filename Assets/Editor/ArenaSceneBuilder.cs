using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Tools > Neon Rewind > Build Arena Scene
/// ArenaScene 의 기본 구조를 자동으로 생성합니다.
/// </summary>
public static class ArenaSceneBuilder
{
    // ── 아레나 치수 ───────────────────────────────────────────
    private const float PlatformRadius   = 14f;
    private const float PlatformThick    = 1f;
    private const float DeathZoneDepth   = 30f;   // 플랫폼 아래 낙사 감지 높이
    private const float DeathZoneY       = -6f;   // 낙사 판정 Y 위치
    private const float WallHeight       = 2f;    // 낮은 가드레일 (선택)

    [MenuItem("Tools/Neon Rewind/Build Arena Scene")]
    public static void BuildArenaScene()
    {
        // ── 씬 열기 또는 생성 ──────────────────────────────────
        string scenePath = "Assets/Scenes/ArenaScene/ArenaScene.unity";
        EnsureDirectory("Assets/Scenes/ArenaScene");

        Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
        if (!scene.IsValid())
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else
            EditorSceneManager.OpenScene(scenePath);

        // ── 태그 등록 ──────────────────────────────────────────
        EnsureTag("DeathZone");
        EnsureTag("SpawnPoint");

        // ── 레이어 등록 ───────────────────────────────────────
        EnsureLayer("Player");
        EnsureLayer("Clone");
        EnsureLayer("Ground");

        // ────────────────────────────────────────────────────
        // 1. 조명
        // ────────────────────────────────────────────────────
        var dirLight = new GameObject("Directional Light");
        var dl = dirLight.AddComponent<Light>();
        dl.type      = LightType.Directional;
        dl.color     = new Color(0.8f, 0.8f, 1f);
        dl.intensity = 0.6f;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 환경광 다크 설정
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.1f);
        RenderSettings.fogColor     = new Color(0.02f, 0.02f, 0.05f);

        // ────────────────────────────────────────────────────
        // 2. 아레나 플랫폼
        // ────────────────────────────────────────────────────
        var arena = new GameObject("Arena");

        // 메인 플랫폼 (원형 — Cylinder로 대체)
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Platform";
        platform.transform.SetParent(arena.transform);
        platform.transform.localPosition = Vector3.zero;
        platform.transform.localScale = new Vector3(PlatformRadius * 2f, PlatformThick * 0.5f, PlatformRadius * 2f);

        // 플랫폼 머티리얼 (URP Lit)
        var platMat = MakeUrpMaterial(new Color(0.06f, 0.06f, 0.12f), metallic: 0.3f, smoothness: 0.8f);
        platform.GetComponent<Renderer>().material = platMat;

        // Ground 레이어
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) platform.layer = groundLayer;

        // ────────────────────────────────────────────────────
        // 3. 낙사존 (DeathZone)
        // ────────────────────────────────────────────────────
        var deathZone = new GameObject("DeathZone");
        deathZone.tag = "DeathZone";
        deathZone.transform.SetParent(arena.transform);
        deathZone.transform.localPosition = new Vector3(0f, DeathZoneY, 0f);

        var dzCol = deathZone.AddComponent<BoxCollider>();
        dzCol.isTrigger = true;
        dzCol.size      = new Vector3(200f, 2f, 200f);  // 넓고 얇은 트리거

        // 에디터에서 보이도록 색상 표시용 Gizmo (실제 렌더러 없음)
        // — SceneView 기즈모로 대신 표시됨

        // ────────────────────────────────────────────────────
        // 4. 스폰 포인트 4개
        // ────────────────────────────────────────────────────
        var spawnRoot = new GameObject("SpawnPoints");
        spawnRoot.transform.SetParent(arena.transform);

        float spawnR = PlatformRadius * 0.6f;
        Vector3[] spawnOffsets = {
            new Vector3( spawnR,  1f,  0f),
            new Vector3(-spawnR,  1f,  0f),
            new Vector3( 0f,      1f,  spawnR),
            new Vector3( 0f,      1f, -spawnR),
        };

        var spawnPoints = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var sp = new GameObject($"SpawnPoint_{i + 1}");
            sp.tag = "SpawnPoint";
            sp.transform.SetParent(spawnRoot.transform);
            sp.transform.localPosition = spawnOffsets[i];
            spawnPoints[i] = sp.transform;
        }

        // ────────────────────────────────────────────────────
        // 5. 네온 포인트 라이트 4개 (구석마다 분위기)
        // ────────────────────────────────────────────────────
        Color[] neonColors = {
            new Color(0f,  0.75f, 1f),   // 블루
            new Color(1f,  0.18f, 0.58f),// 핑크
            new Color(0.6f,0.2f,  1f),   // 퍼플
            new Color(1f,  0.83f, 0f),   // 옐로우
        };

        var lightsRoot = new GameObject("NeonLights");
        lightsRoot.transform.SetParent(arena.transform);

        for (int i = 0; i < 4; i++)
        {
            var lg = new GameObject($"NeonLight_{i + 1}");
            lg.transform.SetParent(lightsRoot.transform);
            lg.transform.localPosition = spawnOffsets[i] + Vector3.up * 3f;

            var pl = lg.AddComponent<Light>();
            pl.type      = LightType.Point;
            pl.color     = neonColors[i];
            pl.intensity = 2f;
            pl.range     = 12f;
        }

        // ────────────────────────────────────────────────────
        // 6. _Systems 계층
        // ────────────────────────────────────────────────────
        var systems = new GameObject("_Systems");
        AddComponent<MatchManager>(systems);
        AddComponent<ScoreSystem>(systems);
        AddComponent<AudioManager>(systems);

        // RespawnManager — 스폰 포인트 배열 자동 연결은 Inspector에서
        var respawnGo = AddComponent<RespawnManager>(systems);

        // CloneManager — 스폰 포인트 배열 자동 연결은 Inspector에서
        var cloneGo = AddComponent<CloneManager>(systems);

        // ────────────────────────────────────────────────────
        // 7. _Core 계층
        // ────────────────────────────────────────────────────
        var core = new GameObject("_Core");
        AddComponent<VFXManager>(core);
        AddComponent<SceneBootstrapper>(core);

        // ────────────────────────────────────────────────────
        // 8. 메인 카메라
        // ────────────────────────────────────────────────────
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.02f, 0.02f, 0.05f);
        cam.fieldOfView      = 60f;
        camGo.AddComponent<AudioListener>();

        var arenaCamera = camGo.AddComponent<ArenaCamera>();
        camGo.transform.position = new Vector3(0f, 16f, -10f);
        camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        // ────────────────────────────────────────────────────
        // 9. HUD Canvas 기본 구조
        // ────────────────────────────────────────────────────
        BuildHUDCanvas();

        // ────────────────────────────────────────────────────
        // 10. 씬 저장
        // ────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);

        Debug.Log("[ArenaSceneBuilder] ✅ ArenaScene 구성 완료!");
        Debug.Log("  ▶ 남은 작업 (Inspector):");
        Debug.Log("    1. RespawnManager → Player Object, Spawn Points[] 연결");
        Debug.Log("    2. CloneManager → Clone Prefab, Spawn Points[] 연결");
        Debug.Log("    3. ArenaCamera → Target (Player Transform) 연결");
        Debug.Log("    4. Player 프리팹 씬에 배치 후 컴포넌트 연결");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    private static GameObject AddComponent<T>(GameObject parent) where T : Component
    {
        // 이미 있으면 추가하지 않음
        if (parent.GetComponent<T>() == null)
            parent.AddComponent<T>();
        return parent;
    }

    private static void BuildHUDCanvas()
    {
        var canvasGo = new GameObject("HUD Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // HUDManager 부착
        canvasGo.AddComponent<HUDManager>();

        // 결과 패널
        var resultsGo = new GameObject("ResultsPanel");
        resultsGo.transform.SetParent(canvasGo.transform, false);
        resultsGo.AddComponent<ResultsPanel>();

        Debug.Log("  ▶ HUD Canvas 생성됨 — TMP 텍스트 요소는 Inspector에서 연결 필요");
    }

    private static void EnsureTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    private static void EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // 이미 있으면 스킵
        for (int i = 0; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (el.stringValue == layerName) return;
        }

        // 빈 슬롯 (8번부터) 에 추가
        for (int i = 8; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogWarning($"[ArenaSceneBuilder] 레이어 슬롯이 없어 '{layerName}' 추가 실패. Project Settings > Tags and Layers에서 수동 추가하세요.");
    }

    /// <summary>URP Lit 셰이더 기반 머티리얼 생성 (Standard 대체)</summary>
    private static Material MakeUrpMaterial(Color baseColor, float metallic = 0f, float smoothness = 0.5f)
    {
        // URP Lit 셰이더 이름 시도 순서
        string[] candidates = {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Sprites/Default",          // 최후 폴백
        };

        Shader shader = null;
        foreach (var name in candidates)
        {
            shader = Shader.Find(name);
            if (shader != null) break;
        }

        var mat = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
        mat.SetColor("_BaseColor", baseColor);          // URP Lit
        mat.color = baseColor;                          // 폴백용
        mat.SetFloat("_Metallic",    metallic);
        mat.SetFloat("_Smoothness",  smoothness);
        return mat;
    }

    private static void EnsureDirectory(string path)
    {
        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }
    }
}
