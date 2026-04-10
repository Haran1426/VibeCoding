using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Tools > Neon Rewind > Fix Pink Materials (URP)
/// Standard 셰이더 머티리얼을 URP Lit 으로 일괄 변환합니다.
/// </summary>
public static class MaterialFixer
{
    [MenuItem("Tools/Neon Rewind/Fix Pink Materials (URP)")]
    public static void FixAllMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[MaterialFixer] URP Lit 셰이더를 찾을 수 없습니다. URP 패키지가 설치되어 있는지 확인하세요.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixed_count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // TMP 머티리얼은 건드리지 않음
            if (path.Contains("TextMesh Pro")) continue;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Standard 또는 셰이더 없음(핑크)인 경우만 변환
            bool isStandard = mat.shader != null && mat.shader.name == "Standard";
            bool isMissing  = mat.shader == null || mat.shader.name.Contains("Hidden/InternalError");

            if (!isStandard && !isMissing) continue;

            // 기존 색상 보존
            Color oldColor = mat.HasProperty("_Color")    ? mat.GetColor("_Color")
                           : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                           : Color.white;

            float oldMetallic   = mat.HasProperty("_Metallic")    ? mat.GetFloat("_Metallic")    : 0f;
            float oldGlossiness = mat.HasProperty("_Glossiness")  ? mat.GetFloat("_Glossiness")  : 0.5f;

            mat.shader = urpLit;

            // URP Lit 프로퍼티에 색상 재적용
            mat.SetColor("_BaseColor", oldColor);
            mat.SetFloat("_Metallic",  oldMetallic);
            mat.SetFloat("_Smoothness", oldGlossiness);

            EditorUtility.SetDirty(mat);
            fixed_count++;

            Debug.Log($"[MaterialFixer] 변환: {Path.GetFileName(path)} ({oldColor})");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MaterialFixer] ✅ 완료 — {fixed_count}개 머티리얼 URP Lit 으로 변환");
    }

    // ── 네온 머티리얼 생성 (아레나용) ────────────────────────
    [MenuItem("Tools/Neon Rewind/Create Neon Materials")]
    public static void CreateNeonMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) { Debug.LogError("URP Lit 없음"); return; }

        string dir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // 아레나 플랫폼
        CreateOrUpdateMat($"{dir}/ArenaPlatformMat.mat", urpLit,
            new Color(0.06f, 0.06f, 0.14f), metallic: 0.4f, smoothness: 0.9f,
            emission: Color.black);

        // 땅
        CreateOrUpdateMat($"{dir}/GroundMat.mat", urpLit,
            new Color(0.04f, 0.04f, 0.08f), metallic: 0.1f, smoothness: 0.5f,
            emission: Color.black);

        // 플레이어 (네온 블루)
        CreateOrUpdateMat($"{dir}/PlayerMat.mat", urpLit,
            new Color(0f, 0.75f, 1f), metallic: 0f, smoothness: 0.6f,
            emission: new Color(0f, 0.4f, 0.8f));

        // 분신 1 — 네온 핑크 반투명
        CreateOrUpdateMat($"{dir}/CloneMat_1.mat", urpLit,
            new Color(1f, 0.18f, 0.58f, 0.5f), metallic: 0f, smoothness: 0.5f,
            emission: new Color(0.5f, 0.05f, 0.25f), transparent: true);

        // 분신 2 — 네온 퍼플 반투명
        CreateOrUpdateMat($"{dir}/CloneMat_2.mat", urpLit,
            new Color(0.6f, 0.2f, 1f, 0.5f), metallic: 0f, smoothness: 0.5f,
            emission: new Color(0.3f, 0.05f, 0.5f), transparent: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MaterialFixer] ✅ 네온 머티리얼 생성 완료 — Assets/Materials/ 확인");
    }

    private static void CreateOrUpdateMat(string path, Shader shader,
        Color baseColor, float metallic, float smoothness,
        Color emission, bool transparent = false)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic",  metallic);
        mat.SetFloat("_Smoothness", smoothness);

        // Emission
        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        // 반투명
        if (transparent)
        {
            mat.SetFloat("_Surface", 1);          // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend",   0);           // Alpha
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        EditorUtility.SetDirty(mat);
    }
}
