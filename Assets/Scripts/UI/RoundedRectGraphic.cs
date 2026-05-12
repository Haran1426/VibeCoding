using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight filled rounded rectangle for authored/runtime UI.
/// Avoids square default Image panels when the UI needs a playful brawler look.
/// </summary>
public class RoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private float radius = 28f;
    [SerializeField, Range(2, 12)] private int cornerSegments = 6;

    public float Radius
    {
        get => radius;
        set
        {
            radius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float r = Mathf.Min(radius, rect.width * 0.5f, rect.height * 0.5f);

        var points = new List<Vector2>(cornerSegments * 4 + 4);
        AddCorner(points, new Vector2(rect.xMax - r, rect.yMax - r), r, 0f, 90f);
        AddCorner(points, new Vector2(rect.xMin + r, rect.yMax - r), r, 90f, 180f);
        AddCorner(points, new Vector2(rect.xMin + r, rect.yMin + r), r, 180f, 270f);
        AddCorner(points, new Vector2(rect.xMax - r, rect.yMin + r), r, 270f, 360f);

        var vert = UIVertex.simpleVert;
        vert.color = color;
        vert.position = rect.center;
        vh.AddVert(vert);

        for (int i = 0; i < points.Count; i++)
        {
            vert.position = points[i];
            vh.AddVert(vert);
        }

        for (int i = 1; i <= points.Count; i++)
        {
            int next = i == points.Count ? 1 : i + 1;
            vh.AddTriangle(0, i, next);
        }
    }

    private void AddCorner(List<Vector2> points, Vector2 center, float r, float startDeg, float endDeg)
    {
        for (int i = 0; i <= cornerSegments; i++)
        {
            float t = i / (float)cornerSegments;
            float angle = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
        }
    }
}
