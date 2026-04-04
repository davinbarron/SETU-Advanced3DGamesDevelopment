using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Code-driven HUD displaying game phase, countdown timer, live scores and final rankings.
/// Instantiated and owned by GameStateManager.
/// </summary>
public class GameHUD
{
    private GameObject     _root;
    private GameObject     _panel;
    private TMP_Text       _phaseLabel;
    private TMP_Text       _timerLabel;
    private GamePhase      _currentPhase;

    private GameObject     _scorePanel;
    private Transform      _scoreContainer;
    private List<TMP_Text> _scoreLabels   = new List<TMP_Text>();

    private GameObject     _rankingsPanel;
    private List<TMP_Text> _rankingLabels = new List<TMP_Text>();

    // ---- Build ----

    public void Build()
    {
        var canvasGo      = new GameObject("GameHUD");
        Object.DontDestroyOnLoad(canvasGo);
        var canvas        = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _root = canvasGo;

        BuildTimerPanel();
        BuildScorePanel();
        BuildRankingsPanel();
    }

    private void BuildTimerPanel()
    {
        _panel = UIHelper.CreateUIObject("TimerPanel", _root.transform);
        var rt = _panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(320f, 80f);
        rt.anchoredPosition = new Vector2(0f, -10f);

        _panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var layout     = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 4f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        _phaseLabel = UIHelper.CreateLabel(_panel.transform, "Waiting...",
            14f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
        _timerLabel = UIHelper.CreateLabel(_panel.transform, "0:00",
            30f, FontStyles.Bold, Color.white);
    }

    private void BuildScorePanel()
    {
        _scorePanel = UIHelper.CreateUIObject("ScorePanel", _root.transform);
        var rt = _scorePanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(200f, 200f);
        rt.anchoredPosition = new Vector2(10f, -10f);

        _scorePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var layout     = _scorePanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.spacing = 4f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        var header = UIHelper.CreateLabel(_scorePanel.transform, "SCORES",
            13f, FontStyles.Bold, new Color(0.75f, 0.75f, 0.75f), 24f);

        var containerGo = UIHelper.CreateUIObject("Container", _scorePanel.transform);
        var containerLayout     = containerGo.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 2f;
        containerLayout.childForceExpandWidth  = true;
        containerLayout.childForceExpandHeight = false;
        containerGo.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        _scoreContainer = containerGo.transform;

        _scorePanel.SetActive(false);
    }

    private void BuildRankingsPanel()
    {
        _rankingsPanel = UIHelper.CreateUIObject("RankingsPanel", _root.transform);
        var rt = _rankingsPanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(340f, 300f);
        rt.anchoredPosition = Vector2.zero;

        _rankingsPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var layout     = _rankingsPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        UIHelper.CreateLabel(_rankingsPanel.transform, "FINAL SCORES",
            24f, FontStyles.Bold, Color.white, 40f);

        for (int i = 0; i < 4; i++)
        {
            var row = UIHelper.CreateUIObject($"Rank_{i}", _rankingsPanel.transform);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 44f;
            rowLe.minHeight       = 44f;

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing               = 8f;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment        = TextAnchor.MiddleLeft;
            rowLayout.padding               = new RectOffset(4, 4, 2, 2);

            // Badge background
            var badgeGo = UIHelper.CreateUIObject("Badge", row.transform);
            var badgeLe = badgeGo.AddComponent<LayoutElement>();
            badgeLe.preferredWidth  = 50f;
            badgeLe.minWidth        = 50f;
            badgeLe.preferredHeight = 36f;
            badgeGo.AddComponent<Image>().color = RankColor(i);

            // Badge text
            var badgeTextGo = UIHelper.CreateUIObject("BadgeText", badgeGo.transform);
            UIHelper.StretchFull(badgeTextGo);
            var badgeText       = badgeTextGo.AddComponent<TextMeshProUGUI>();
            badgeText.text      = OrdinalRank(i + 1);
            badgeText.fontSize  = 16f;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.color     = Color.white;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.raycastTarget = false;

            // Name and score
            var labelGo = UIHelper.CreateUIObject("Label", row.transform);
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

    // ---- Update ----

    public void UpdatePhase(GamePhase phase)
    {
        _currentPhase = phase;
        if (_phaseLabel == null) return;

        if (_rankingsPanel != null) _rankingsPanel.SetActive(false);
        if (_panel        != null) _panel.SetActive(true);

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
            int   ceil  = Mathf.CeilToInt(timeRemaining);
            float pulse = 1.0f + timeRemaining % 1.0f * 0.2f;
            _timerLabel.transform.localScale = new Vector3(pulse, pulse, 1f);
            _timerLabel.text = ceil > 0 ? ceil.ToString() : "GO!";
        }
        else
        {
            _timerLabel.transform.localScale = Vector3.one;
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            _timerLabel.text = $"{minutes}:{seconds:00}";
        }
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

        var players = new List<Example.Player>();
        runner.GetAllBehaviours(players);

        // Create labels as needed
        while (_scoreLabels.Count < players.Count)
        {
            var go = UIHelper.CreateUIObject($"Score_{_scoreLabels.Count}", _scoreContainer);
            go.AddComponent<LayoutElement>().preferredHeight = 28f;
            var t       = go.AddComponent<TextMeshProUGUI>();
            t.fontSize  = 14f;
            t.color     = Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false;
            _scoreLabels.Add(t);
        }

        for (int i = 0; i < _scoreLabels.Count; i++)
            _scoreLabels[i].gameObject.SetActive(i < players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            string name = players[i].NameTag != null
                ? players[i].NameTag.NickName.Value
                : $"Player {i + 1}";
            _scoreLabels[i].text = $"{name}: {players[i].Score}";
        }
    }

    public void ShowRankings(List<(string name, int score, int rank)> rankings)
    {
        if (_rankingsPanel == null) return;
        if (_panel != null) _panel.SetActive(false);

        _rankingsPanel.SetActive(true);

        for (int i = 0; i < _rankingLabels.Count; i++)
        {
            var rowGo = _rankingLabels[i].transform.parent.gameObject;
            if (i < rankings.Count)
            {
                rowGo.SetActive(true);
                _rankingLabels[i].text = $"{rankings[i].name}  —  {rankings[i].score} pts";
            }
            else
            {
                rowGo.SetActive(false);
            }
        }
    }

    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
    }

    // ---- Helpers ----

    private static Color RankColor(int index)
    {
        switch (index)
        {
            case 0:  return new Color(1f,    0.84f, 0f,    1f); // Gold
            case 1:  return new Color(0.75f, 0.75f, 0.75f, 1f); // Silver
            case 2:  return new Color(0.8f,  0.5f,  0.2f,  1f); // Bronze
            default: return new Color(0.3f,  0.3f,  0.3f,  1f); // Grey
        }
    }

    private static string OrdinalRank(int rank)
    {
        switch (rank)
        {
            case 1:  return "1st";
            case 2:  return "2nd";
            case 3:  return "3rd";
            default: return $"{rank}th";
        }
    }
}