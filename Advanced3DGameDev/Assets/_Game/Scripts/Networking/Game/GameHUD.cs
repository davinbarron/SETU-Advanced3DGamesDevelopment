using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Code-driven HUD displaying the current game phase and countdown timer.
/// Instantiated and owned by GameStateManager
/// </summary>
public class GameHUD
{
    private GameObject _root;
    private TMP_Text   _phaseLabel;
    private TMP_Text   _timerLabel;

    public void Build()
    {
        var canvasGo = new GameObject("GameHUD");
        Object.DontDestroyOnLoad(canvasGo);

        var canvas        = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        _root = canvasGo;

        // Panel anchored to top-centre
        var panel   = CreateUIObject("Panel", canvasGo.transform);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 1f);
        panelRt.anchorMax        = new Vector2(0.5f, 1f);
        panelRt.pivot            = new Vector2(0.5f, 1f);
        panelRt.sizeDelta        = new Vector2(320f, 80f);
        panelRt.anchoredPosition = new Vector2(0f, -10f);

        var panelImg   = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);

        var layout     = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 4f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        _phaseLabel = CreateLabel(panel.transform, "Waiting for players...",
            14f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));

        _timerLabel = CreateLabel(panel.transform, "1:00",
            30f, FontStyles.Bold, Color.white);
    }

    public void UpdatePhase(GamePhase phase)
    {
        if (_phaseLabel == null) return;

        switch (phase)
        {
            case GamePhase.Waiting:
                _phaseLabel.text  = "Waiting for players...";
                _timerLabel.color = new Color(0.75f, 0.75f, 0.75f);
                break;
            case GamePhase.Playing:
                _phaseLabel.text  = "Round in progress";
                _timerLabel.color = Color.white;
                break;
            case GamePhase.GameOver:
                _phaseLabel.text  = "Game Over";
                _timerLabel.color = new Color(1f, 0.4f, 0.4f);
                break;
        }
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (_timerLabel == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        _timerLabel.text = $"{minutes}:{seconds:00}";
    }

    public void Destroy()
    {
        if (_root != null)
            Object.Destroy(_root);
    }

    private static TMP_Text CreateLabel(Transform parent, string text,
        float fontSize, FontStyles style, Color color)
    {
        var go = CreateUIObject("Label", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 32f;

        var t       = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}