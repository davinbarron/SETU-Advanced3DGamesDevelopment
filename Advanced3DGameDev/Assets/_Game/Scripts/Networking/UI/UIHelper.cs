using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared UI construction utilities used by GameHUD and RoomBrowserUI.
/// </summary>
public static class UIHelper
{
    public static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void StretchFull(GameObject go)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static TMP_Text CreateLabel(Transform parent, string text,
        float fontSize, FontStyles style, Color color, float preferredHeight = 32f)
    {
        var go = CreateUIObject("Label", parent);
        go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        var t       = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    public static Button BuildButton(Transform parent, string label,
        Color color, float width, float height = 40f)
    {
        var go = CreateUIObject("Button", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = height;

        var img   = go.AddComponent<Image>();
        img.color = color;
        var btn   = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = color,
            highlightedColor = color * 1.2f,
            pressedColor     = color * 0.7f,
            disabledColor    = new Color(0.4f, 0.4f, 0.4f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };

        var lblGo = CreateUIObject("Label", go.transform);
        StretchFull(lblGo);
        var t     = lblGo.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 15f;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return btn;
    }

    public static TMP_InputField BuildInputField(Transform parent,
        string placeholder, float width, float height = 40f)
    {
        var go = CreateUIObject("InputField", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = height;
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

        var field = go.AddComponent<TMP_InputField>();

        var textArea = CreateUIObject("TextArea", go.transform);
        StretchFull(textArea);
        textArea.AddComponent<RectMask2D>();
        field.textViewport = textArea.GetComponent<RectTransform>();

        var ph       = CreateUIObject("Placeholder", textArea.transform);
        StretchFull(ph);
        var phText   = ph.AddComponent<TextMeshProUGUI>();
        phText.text  = placeholder;
        phText.color = new Color(0.5f, 0.5f, 0.5f);
        phText.fontSize = 14f;
        phText.margin   = new Vector4(8, 0, 8, 0);
        phText.raycastTarget = false;
        field.placeholder = phText;

        var txt       = CreateUIObject("Text", textArea.transform);
        StretchFull(txt);
        var inputText = txt.AddComponent<TextMeshProUGUI>();
        inputText.color    = Color.white;
        inputText.fontSize = 14f;
        inputText.margin   = new Vector4(8, 0, 8, 0);
        field.textComponent = inputText;

        return field;
    }
}