using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tools > Neon Rewind > Spawn Player in Scene
/// 현재 열린 씬에 플레이어를 즉시 생성합니다.
/// </summary>
public static class PlayerSpawner
{
    [MenuItem("Tools/Neon Rewind/Spawn Player in Scene")]
    public static void SpawnPlayer()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("플레이 모드 중",
                "플레이 모드에서는 사용할 수 없습니다.\n\n플레이를 중지한 뒤 다시 실행하세요.", "확인");
            return;
        }

        // 기존 Player 있으면 제거
        var existing = GameObject.Find("Player");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[PlayerSpawner] 기존 Player 제거");
        }

        // ── Player 루트 오브젝트 ────────────────────────────
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 0f);

        // 레이어
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) player.layer = playerLayer;

        // 머티리얼 — URP Lit 블루
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMat.mat");
        if (mat != null)
            player.GetComponent<Renderer>().material = mat;
        else
            ApplyUrpColor(player, new Color(0f, 0.75f, 1f));

        // ── Rigidbody ───────────────────────────────────────
        var rb = player.AddComponent<Rigidbody>();
        rb.mass             = 1f;
        rb.linearDamping    = 2f;
        rb.angularDamping   = 5f;
        rb.freezeRotation   = true;   // X,Y,Z 회전 고정

        // ── 컴포넌트 순서대로 추가 ──────────────────────────
        var stats = player.AddComponent<PlayerStats>();
        stats.playerId = 0;
        stats.isClone  = false;

        player.AddComponent<PlayerInput>();
        player.AddComponent<PlayerController>();
        player.AddComponent<InputRecorder>();
        player.AddComponent<KnockbackReceiver>();

        var dd = player.AddComponent<DeathDetector>();

        // ── 눈 방향 표시 (작은 구체) ────────────────────────
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "EyeIndicator";
        eye.transform.SetParent(player.transform);
        eye.transform.localPosition = new Vector3(0f, 0.2f, 0.45f);
        eye.transform.localScale    = new Vector3(0.2f, 0.2f, 0.2f);
        Object.DestroyImmediate(eye.GetComponent<Collider>());
        ApplyUrpColor(eye, new Color(1f, 1f, 1f));

        // ── ArenaCamera 타겟 자동 연결 ──────────────────────
        var cam = Object.FindFirstObjectByType<ArenaCamera>();
        if (cam != null)
        {
            cam.SetTarget(player.transform);
            EditorUtility.SetDirty(cam);
            Debug.Log("[PlayerSpawner] ArenaCamera 타겟 연결 완료");
        }

        // ── RespawnManager 연결 ─────────────────────────────
        var respawn = Object.FindFirstObjectByType<RespawnManager>();
        if (respawn != null)
        {
            var so = new SerializedObject(respawn);
            so.FindProperty("playerObject").objectReferenceValue = player;
            so.ApplyModifiedProperties();
            Debug.Log("[PlayerSpawner] RespawnManager → playerObject 연결 완료");
        }

        // ── 씬 저장 ─────────────────────────────────────────
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeGameObject = player;

        Debug.Log("[PlayerSpawner] ✅ Player 생성 완료!");
        Debug.Log("  WASD=이동  Space=점프  Shift=대시  마우스LMB=공격");
        Debug.Log("  ※ PlayerController > Attack Target Mask 에 Player+Clone 레이어 설정 권장");
    }

    private static void ApplyUrpColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        renderer.material = mat;
    }
}
