using System.Collections.Generic;
using Fusion;
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
    private GameObject _panel;
    private TMP_Text   _phaseLabel;
    private TMP_Text   _timerLabel;
    private GamePhase  _currentPhase;
    private GameObject _scorePanel;
    private Transform  _scoreContainer;
    private List<TMP_Text> _scoreLabels = new List<TMP_Text>();
    private GameObject     _rankingsPanel;
    private List<TMP_Text> _rankingLabels = new List<TMP_Text>();

    public void Build()
    {
        var canvasGo = new GameObject("GameHUD");
        Object.DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        _root = canvasGo;

        _panel = CreateUIObject("Panel", canvasGo.transform);
        var panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot     = new Vector2(0.5f, 1f);
        panelRt.sizeDelta = new Vector2(320f, 80f);
        panelRt.anchoredPosition = new Vector2(0f, -10f);

        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.55f);

        var layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 4f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        _phaseLabel = CreateLabel(_panel.transform, "Waiting...", 14f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
        _timerLabel = CreateLabel(_panel.transform, "0:00", 30f, FontStyles.Bold, Color.white);

        BuildScorePanel();
        BuildRankingsPanel();
    }

    private void BuildScorePanel()
    {
        _scorePanel = CreateUIObject("ScorePanel", _root.transform);
        var rt = _scorePanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(200f, 200f);
        rt.anchoredPosition = new Vector2(10f, -10f);

        var bg    = _scorePanel.AddComponent<Image>();
        bg.color  = new Color(0f, 0f, 0f, 0.55f);

        var layout     = _scorePanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 4f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        var header = CreateUIObject("Header", _scorePanel.transform);
        header.AddComponent<LayoutElement>().preferredHeight = 24f;
        var headerText       = header.AddComponent<TextMeshProUGUI>();
        headerText.text      = "SCORES";
        headerText.fontSize  = 13f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color     = new Color(0.75f, 0.75f, 0.75f);
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.raycastTarget = false;

        _scoreContainer = CreateUIObject("Container", _scorePanel.transform).transform;
        _scorePanel.SetActive(false);
    }

    private void BuildRankingsPanel()
    {
        _rankingsPanel = CreateUIObject("RankingsPanel", _root.transform);
        var rt = _rankingsPanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(340f, 300f);
        rt.anchoredPosition = Vector2.zero;

        var bg    = _rankingsPanel.AddComponent<Image>();
        bg.color  = new Color(0f, 0f, 0f, 0.85f);

        var layout     = _rankingsPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        // Title
        var titleGo = CreateUIObject("Title", _rankingsPanel.transform);
        titleGo.AddComponent<LayoutElement>().preferredHeight = 40f;
        var titleText       = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text      = "FINAL SCORES";
        titleText.fontSize  = 24f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color     = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        // Pre-build 4 ranking slots
        for (int i = 0; i < 4; i++)
        {
            var row = CreateUIObject($"Rank_{i}", _rankingsPanel.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 44f;

            var rowLayout     = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childForceExpandWidth  = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment         = TextAnchor.MiddleLeft;

            // Rank badge background
            var badgeGo  = CreateUIObject("Badge", row.transform);
            var badgeLe  = badgeGo.AddComponent<LayoutElement>();
            badgeLe.preferredWidth  = 50f;
            badgeLe.preferredHeight = 36f;
            var badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color = RankColor(i);

            // Badge text as child of badge background
            var badgeTextGo = CreateUIObject("BadgeText", badgeGo.transform);
            var badgeTextRt = badgeTextGo.GetComponent<RectTransform>();
            badgeTextRt.anchorMin = Vector2.zero;
            badgeTextRt.anchorMax = Vector2.one;
            badgeTextRt.offsetMin = Vector2.zero;
            badgeTextRt.offsetMax = Vector2.zero;
            var badgeText       = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeText.text      = OrdinalRank(i + 1);
            badgeText.fontSize  = 16f;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.color     = Color.white;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.raycastTarget = false;

            // Name and score label
            var labelGo = CreateUIObject("Label", row.transform);
            labelGo.AddComponent<LayoutElement>().preferredHeight = 36f;
            var t       = labelGo.AddComponent<TextMeshProUGUI>();
            t.fontSize  = 18f;
            t.color     = Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false;
            _rankingLabels.Add(t);
        }

        _rankingsPanel.SetActive(false);
    }

    public void UpdateScores(NetworkRunner runner)
    {
        if (_scorePanel == null) return;
        if (_currentPhase != GamePhase.Playing && _currentPhase != GamePhase.GameOver)
        {
            _scorePanel.SetActive(false);
            return;
        }

        _scorePanel.SetActive(true);

        // Get all players
        var players = new List<Example.Player>();
        runner.GetAllBehaviours(players);

        // Ensure we have enough labels
        while (_scoreLabels.Count < players.Count)
        {
            var labelGo = CreateUIObject($"Score_{_scoreLabels.Count}", _scoreContainer);
            labelGo.AddComponent<LayoutElement>().preferredHeight = 28f;
            var t       = labelGo.AddComponent<TextMeshProUGUI>();
            t.fontSize  = 14f;
            t.color     = Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false;
            _scoreLabels.Add(t);
        }

        // Hide excess labels
        for (int i = 0; i < _scoreLabels.Count; i++)
            _scoreLabels[i].gameObject.SetActive(i < players.Count);

        // Update each label
        for (int i = 0; i < players.Count; i++)
        {
            string name  = players[i].NameTag != null
                ? players[i].NameTag.NickName.Value
                : $"Player {i + 1}";
                
            _scoreLabels[i].text = $"{name}: {players[i].Score}";
        }
    }

    public void UpdatePhase(GamePhase phase)
    {
        _currentPhase = phase;
        if (_phaseLabel == null) return;

        // Show/hide rankings panel
        if (_rankingsPanel != null)
            _rankingsPanel.SetActive(false);

        // Restore timer panel if it was hidden
        if (_panel != null)
            _panel.SetActive(true);

        switch (phase)
        {
            case GamePhase.Waiting:
                _phaseLabel.text  = "Waiting for players...";
                _timerLabel.color = new Color(0.75f, 0.75f, 0.75f);
                break;
            case GamePhase.Countdown:
                _phaseLabel.text  = "Get Ready!";
                _timerLabel.color = Color.yellow;
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

        if (_currentPhase == GamePhase.Countdown)
        {
            // Show big whole numbers during countdown
            int ceilSeconds = Mathf.CeilToInt(timeRemaining);
            float pulse = 1.0f + timeRemaining % 1.0f * 0.2f;
            _timerLabel.transform.localScale = new Vector3(pulse, pulse, 1f);
            _timerLabel.text = ceilSeconds > 0 ? ceilSeconds.ToString() : "GO!";
        }
        else
        {
            // Show standard clock format during play
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            _timerLabel.text = $"{minutes}:{seconds:00}";
        }
    }

    public void ShowRankings(List<(string name, int score, int rank)> rankings)
    {
        if (_rankingsPanel == null) return;

        // Hide the timer panel — rankings replace it
        if (_panel != null) _panel.SetActive(false);

        _rankingsPanel.SetActive(true);

        for (int i = 0; i < _rankingLabels.Count; i++)
        {
            if (i < rankings.Count)
            {
                _rankingLabels[i].gameObject.transform.parent.gameObject.SetActive(true);
                _rankingLabels[i].text = $"{rankings[i].name}  —  {rankings[i].score} pts";
            }
            else
            {
                _rankingLabels[i].gameObject.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    private static Color RankColor(int index)
    {
        switch (index)
        {
            case 0:  return new Color(1f,   0.84f, 0f,   1f); // Gold
            case 1:  return new Color(0.75f,0.75f, 0.75f,1f); // Silver
            case 2:  return new Color(0.8f, 0.5f,  0.2f, 1f); // Bronze
            default: return new Color(0.3f, 0.3f,  0.3f, 1f); // Grey
        }
    }

    private static string OrdinalRank(int rank)
    {
        switch (rank)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return $"{rank}th";
        }
    }

    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
    }

    private static TMP_Text CreateLabel(Transform parent, string text, float fontSize, FontStyles style, Color color)
    {
        var go = CreateUIObject("Label", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 32f;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
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