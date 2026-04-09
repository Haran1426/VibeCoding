#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 메뉴: Tools > Color Pop > Generate Levels
/// 20개의 LevelData ScriptableObject를 자동 생성합니다.
/// </summary>
public static class LevelDataFactory
{
    [MenuItem("Tools/Color Pop/Generate 20 Levels")]
    public static void GenerateLevels()
    {
        string folder = "Assets/ScriptableObjects/Levels";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Levels");

        // (rows, cols, colors, target, star2, star3)
        var configs = new (int r, int c, int col, int tgt, int s2, int s3)[]
        {
            (6, 6, 3,     0,   200,  500),  // 1
            (6, 6, 3,     0,   300,  700),  // 2
            (7, 7, 4,     0,   400,  900),  // 3
            (7, 7, 4,     0,   500, 1100),  // 4
            (8, 8, 4,     0,   600, 1300),  // 5
            (8, 8, 4,  1000,   800, 1600),  // 6
            (8, 8, 4,  1200,  1000, 1800),  // 7
            (8, 8, 5,  1200,  1000, 2000),  // 8
            (9, 9, 4,  1500,  1200, 2200),  // 9
            (9, 9, 5,  1500,  1300, 2400),  // 10
            (9, 9, 5,  1800,  1500, 2700),  // 11
            (9, 9, 5,  2000,  1700, 3000),  // 12
            (10,10, 4, 2000,  1800, 3200),  // 13
            (10,10, 5, 2200,  2000, 3500),  // 14
            (10,10, 5, 2500,  2200, 3800),  // 15
            (10,10, 5, 2800,  2500, 4200),  // 16
            (10,10, 5, 3000,  2800, 4600),  // 17
            (10,10, 5, 3200,  3000, 5000),  // 18
            (10,10, 5, 3500,  3200, 5500),  // 19
            (10,10, 5, 4000,  3500, 6000),  // 20
        };

        for (int i = 0; i < configs.Length; i++)
        {
            var (r, c, col, tgt, s2, s3) = configs[i];
            var data = ScriptableObject.CreateInstance<LevelData>();
            data.rows         = r;
            data.cols         = c;
            data.colorCount   = col;
            data.targetScore  = tgt;
            data.star2Score   = s2;
            data.star3Score   = s3;
            data.levelTitle   = $"Level {i + 1}";

            string path = $"{folder}/Level_{i + 1:D2}.asset";
            AssetDatabase.CreateAsset(data, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 20개 레벨 데이터 생성 완료: Assets/ScriptableObjects/Levels/");
    }
}
#endif
