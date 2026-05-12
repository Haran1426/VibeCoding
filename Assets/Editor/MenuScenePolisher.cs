using UnityEditor;

public static class MenuScenePolisher
{
    [MenuItem("Tools/Neon Rewind/Rebuild Menu Scene Title")]
    public static void RebuildMenuSceneTitle()
    {
        UIScenePolisher.RebuildMenuSceneUI();
    }
}
