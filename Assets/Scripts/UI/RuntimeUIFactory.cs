using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeUIFactory
{
    public static Canvas EnsureCanvas(string name = "Runtime HUD Canvas")
    {
        EnsureEventSystem();

        if (!string.IsNullOrEmpty(name))
        {
            var named = GameObject.Find(name)?.GetComponent<Canvas>();
            if (named != null) return named;
        }

        if (name == "Runtime HUD Canvas")
        {
            var existingCanvas = Object.FindFirstObjectByType<Canvas>();
            if (existingCanvas != null) return existingCanvas;
        }

        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem_Runtime");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text,
        int fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return label;
    }

    public static GameObject CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    public static Button CreateButton(Transform parent, string name, string text,
        Vector2 anchoredPosition, Vector2 size)
    {
        var go = CreatePanel(parent, name, new Color(0.05f, 0.08f, 0.12f, 0.92f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.05f, 0.08f, 0.12f, 0.92f);
        colors.highlightedColor = new Color(0.0f, 0.45f, 0.7f, 0.95f);
        colors.pressedColor = new Color(0f, 0.75f, 1f, 1f);
        colors.disabledColor = new Color(0.08f, 0.09f, 0.11f, 0.55f);
        button.colors = colors;

        CreatePanel(go.transform, name + "_Accent", new Color(0f, 0.85f, 1f, 0.95f),
            new Vector2(0f, 0f), new Vector2(0.018f, 1f), Vector2.zero, Vector2.zero);
        CreateText(go.transform, name + "_Text", text, 28, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        return button;
    }
}
