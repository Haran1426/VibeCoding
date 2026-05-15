using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Font-independent block text for critical menu labels.
/// Keeps title/buttons readable even if TMP font assets or materials fail to render.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class PixelTextGraphic : MaskableGraphic
{
    [SerializeField] private string text = "";
    [SerializeField] private TextAnchor alignment = TextAnchor.MiddleCenter;
    [SerializeField, Range(0.4f, 1f)] private float fill = 0.78f;

    private const int GlyphHeight = 7;

    public string Text
    {
        get => text;
        set
        {
            text = value ?? "";
            SetVerticesDirty();
        }
    }

    public TextAnchor Alignment
    {
        get => alignment;
        set
        {
            alignment = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (string.IsNullOrEmpty(text)) return;

        Rect rect = GetPixelAdjustedRect();
        string[] lines = text.ToUpperInvariant().Split('\n');
        int maxColumns = 1;
        foreach (string line in lines)
            maxColumns = Mathf.Max(maxColumns, MeasureColumns(line));

        float cell = Mathf.Min(rect.width / maxColumns, rect.height / Mathf.Max(1, lines.Length * (GlyphHeight + 2) - 2));
        if (cell <= 0.01f) return;

        float lineHeight = cell * (GlyphHeight + 2);
        float totalHeight = lineHeight * lines.Length - cell * 2f;
        float y = GetStartY(rect, totalHeight);

        foreach (string line in lines)
        {
            int columns = MeasureColumns(line);
            float lineWidth = columns * cell;
            float x = GetStartX(rect, lineWidth);
            DrawLine(vh, line, x, y, cell);
            y -= lineHeight;
        }
    }

    private void DrawLine(VertexHelper vh, string line, float x, float y, float cell)
    {
        foreach (char ch in line.ToUpperInvariant())
        {
            string[] glyph = Glyph(ch);
            int width = GlyphWidth(ch);
            if (glyph != null)
            {
                for (int row = 0; row < glyph.Length; row++)
                {
                    string pattern = glyph[row];
                    for (int col = 0; col < pattern.Length; col++)
                    {
                        if (pattern[col] != '1') continue;
                        AddQuad(vh, x + col * cell, y - row * cell, cell * fill);
                    }
                }
            }

            x += (width + 1) * cell;
        }
    }

    private void AddQuad(VertexHelper vh, float x, float y, float size)
    {
        int start = vh.currentVertCount;
        var vert = UIVertex.simpleVert;
        vert.color = color;

        vert.position = new Vector3(x, y, 0f);
        vh.AddVert(vert);
        vert.position = new Vector3(x + size, y, 0f);
        vh.AddVert(vert);
        vert.position = new Vector3(x + size, y - size, 0f);
        vh.AddVert(vert);
        vert.position = new Vector3(x, y - size, 0f);
        vh.AddVert(vert);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private float GetStartX(Rect rect, float lineWidth)
    {
        if (alignment == TextAnchor.UpperLeft || alignment == TextAnchor.MiddleLeft || alignment == TextAnchor.LowerLeft)
            return rect.xMin;
        if (alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight || alignment == TextAnchor.LowerRight)
            return rect.xMax - lineWidth;
        return rect.center.x - lineWidth * 0.5f;
    }

    private float GetStartY(Rect rect, float totalHeight)
    {
        if (alignment == TextAnchor.UpperLeft || alignment == TextAnchor.UpperCenter || alignment == TextAnchor.UpperRight)
            return rect.yMax;
        if (alignment == TextAnchor.LowerLeft || alignment == TextAnchor.LowerCenter || alignment == TextAnchor.LowerRight)
            return rect.yMin + totalHeight;
        return rect.center.y + totalHeight * 0.5f;
    }

    private static int MeasureColumns(string line)
    {
        int columns = 0;
        foreach (char ch in line)
            columns += GlyphWidth(ch) + 1;
        return Mathf.Max(1, columns - 1);
    }

    private static int GlyphWidth(char ch) => ch == ' ' ? 3 : 5;

    private static string[] Glyph(char ch)
    {
        switch (ch)
        {
            case 'A': return G("01110","10001","10001","11111","10001","10001","10001");
            case 'B': return G("11110","10001","10001","11110","10001","10001","11110");
            case 'C': return G("01111","10000","10000","10000","10000","10000","01111");
            case 'D': return G("11110","10001","10001","10001","10001","10001","11110");
            case 'E': return G("11111","10000","10000","11110","10000","10000","11111");
            case 'F': return G("11111","10000","10000","11110","10000","10000","10000");
            case 'G': return G("01111","10000","10000","10111","10001","10001","01111");
            case 'H': return G("10001","10001","10001","11111","10001","10001","10001");
            case 'I': return G("11111","00100","00100","00100","00100","00100","11111");
            case 'J': return G("00111","00010","00010","00010","00010","10010","01100");
            case 'K': return G("10001","10010","10100","11000","10100","10010","10001");
            case 'L': return G("10000","10000","10000","10000","10000","10000","11111");
            case 'M': return G("10001","11011","10101","10101","10001","10001","10001");
            case 'N': return G("10001","11001","10101","10011","10001","10001","10001");
            case 'O': return G("01110","10001","10001","10001","10001","10001","01110");
            case 'P': return G("11110","10001","10001","11110","10000","10000","10000");
            case 'Q': return G("01110","10001","10001","10001","10101","10010","01101");
            case 'R': return G("11110","10001","10001","11110","10100","10010","10001");
            case 'S': return G("01111","10000","10000","01110","00001","00001","11110");
            case 'T': return G("11111","00100","00100","00100","00100","00100","00100");
            case 'U': return G("10001","10001","10001","10001","10001","10001","01110");
            case 'V': return G("10001","10001","10001","10001","10001","01010","00100");
            case 'W': return G("10001","10001","10001","10101","10101","10101","01010");
            case 'X': return G("10001","10001","01010","00100","01010","10001","10001");
            case 'Y': return G("10001","10001","01010","00100","00100","00100","00100");
            case 'Z': return G("11111","00001","00010","00100","01000","10000","11111");
            case '0': return G("01110","10001","10011","10101","11001","10001","01110");
            case '1': return G("00100","01100","00100","00100","00100","00100","01110");
            case '2': return G("01110","10001","00001","00010","00100","01000","11111");
            case '3': return G("11110","00001","00001","01110","00001","00001","11110");
            case '4': return G("00010","00110","01010","10010","11111","00010","00010");
            case '5': return G("11111","10000","10000","11110","00001","00001","11110");
            case '6': return G("01110","10000","10000","11110","10001","10001","01110");
            case '7': return G("11111","00001","00010","00100","01000","01000","01000");
            case '8': return G("01110","10001","10001","01110","10001","10001","01110");
            case '9': return G("01110","10001","10001","01111","00001","00001","01110");
            case '.': return G("00000","00000","00000","00000","00000","01100","01100");
            case ':': return G("00000","01100","01100","00000","01100","01100","00000");
            case '-': return G("00000","00000","00000","11111","00000","00000","00000");
            case '/': return G("00001","00010","00010","00100","01000","01000","10000");
            case '!': return G("00100","00100","00100","00100","00100","00000","00100");
            default: return null;
        }
    }

    private static string[] G(params string[] rows) => rows;
}
