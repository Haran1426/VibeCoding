using UnityEngine;

/// <summary>
/// 네온 글로우 머티리얼을 쉽게 만드는 유틸리티
/// </summary>
public static class NeonMaterialHelper
{
    public static Material CreateNeonMaterial(Color baseColor, float emissionIntensity = 3f)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = baseColor;
        mat.EnableKeyword("_EMISSION");
        Color emission = baseColor * emissionIntensity;
        mat.SetColor("_EmissionColor", emission);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.2f);
        return mat;
    }

    public static void ApplyNeon(GameObject go, Color color, float intensity = 3f)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.material = CreateNeonMaterial(color, intensity);
    }

    // 자주 쓰는 네온 색상 프리셋
    public static Color NeonBlue   => new Color(0.0f, 0.6f, 1.0f);
    public static Color NeonRed    => new Color(1.0f, 0.1f, 0.1f);
    public static Color NeonGreen  => new Color(0.1f, 1.0f, 0.3f);
    public static Color NeonYellow => new Color(1.0f, 0.9f, 0.0f);
    public static Color NeonPurple => new Color(0.7f, 0.0f, 1.0f);
    public static Color NeonOrange => new Color(1.0f, 0.5f, 0.0f);
}
