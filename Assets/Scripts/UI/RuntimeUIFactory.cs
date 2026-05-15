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

    public static PixelTextGraphic CreatePixelText(Transform parent, string name, string text,
        TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var label = go.AddComponent<PixelTextGraphic>();
        label.Text = text;
        label.Alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    public static GameObject CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateRoundedPanel(parent, name, color, anchorMin, anchorMax, anchoredPosition, size, 28f, null);
    }

    public static GameObject CreateRoundedPanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
        float radius = 28f, Color? outlineColor = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        if (go.GetComponent<CanvasRenderer>() == null)
            go.AddComponent<CanvasRenderer>();

        var graphic = go.AddComponent<RoundedRectGraphic>();
        graphic.color = color;
        graphic.Radius = radius;
        graphic.raycastTarget = false;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.03f, 0.01f, 0.08f, 0.45f);
        shadow.effectDistance = new Vector2(7f, -7f);

        if (outlineColor.HasValue)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor.Value;
            outline.effectDistance = new Vector2(4f, -4f);
        }

        return go;
    }

    public static Image CreateImagePanel(Transform parent, string name, Color color,
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
        return image;
    }

    public static Button CreateButton(Transform parent, string name, string text,
        Vector2 anchoredPosition, Vector2 size)
    {
        var go = CreateRoundedPanel(parent, name, new Color(1f, 0.78f, 0.08f, 0.98f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size,
            26f, new Color(1f, 1f, 1f, 0.94f));

        var button = go.AddComponent<Button>();
        var buttonGraphic = go.GetComponent<RoundedRectGraphic>();
        buttonGraphic.raycastTarget = true;
        button.targetGraphic = buttonGraphic;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 0.78f, 0.08f, 0.98f);
        colors.highlightedColor = new Color(0.18f, 0.88f, 1f, 1f);
        colors.pressedColor = new Color(1f, 0.30f, 0.62f, 1f);
        colors.disabledColor = new Color(0.32f, 0.34f, 0.42f, 0.65f);
        button.colors = colors;

        CreateRoundedPanel(go.transform, name + "_Accent", new Color(1f, 0.30f, 0.62f, 0.92f),
            new Vector2(0.02f, 0.16f), new Vector2(0.12f, 0.84f), Vector2.zero, Vector2.zero, 12f);
        CreatePixelText(go.transform, name + "_PixelText", text, TextAnchor.MiddleCenter,
            new Vector2(0.18f, 0.22f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero, Color.white);
        return button;
    }
}
