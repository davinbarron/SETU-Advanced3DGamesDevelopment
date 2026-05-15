using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Code-driven HUD. Two separate post-game panels:
/// 1. Scoreboard — shows final rankings for 5 seconds
/// 2. Vote panel — shows rematch/leave options after scoreboard hides
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
    private List<TMP_Text> _scoreLabels     = new List<TMP_Text>();

    private GameObject     _scoreboardPanel;
    private List<TMP_Text> _rankingLabels   = new List<TMP_Text>();

    private GameObject     _votePanel;
    private Button         _rematchButton;
    private Button         _leaveButton;
    private TMP_Text       _voteStatusLabel;

    private Action         _onVoteRematchRequested;
    private Action         _onLeaveRoomRequested;

    private float          _scoreboardHideTime = -1f;
    private bool           _votePanelShown     = false;

    // ---- Build ----

    public void Build(Action onVoteRematchRequested, Action onLeaveRoomRequested)
    {
        _onVoteRematchRequested = onVoteRematchRequested;
        _onLeaveRoomRequested   = onLeaveRoomRequested;

        var canvasGo      = new GameObject("GameHUD");
        UnityEngine.Object.DontDestroyOnLoad(canvasGo);
        var canvas        = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        _root = canvasGo;

        BuildTimerPanel();
        BuildScorePanel();
        BuildScoreboardPanel();
        BuildVotePanel();
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

        UIHelper.CreateLabel(_scorePanel.transform, "SCORES",
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

    private void BuildScoreboardPanel()
    {
        _scoreboardPanel = UIHelper.CreateUIObject("ScoreboardPanel", _root.transform);
        var rt = _scoreboardPanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(340f, 300f);
        rt.anchoredPosition = Vector2.zero;

        _scoreboardPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var layout     = _scoreboardPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        UIHelper.CreateLabel(_scoreboardPanel.transform, "FINAL SCORES",
            24f, FontStyles.Bold, Color.white, 40f);

        for (int i = 0; i < 4; i++)
        {
            var row   = UIHelper.CreateUIObject($"Rank_{i}", _scoreboardPanel.transform);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 44f;
            rowLe.minHeight       = 44f;

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing                = 8f;
            rowLayout.childForceExpandWidth  = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childAlignment         = TextAnchor.MiddleLeft;
            rowLayout.padding                = new RectOffset(4, 4, 2, 2);

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

        _scoreboardPanel.SetActive(false);
    }

    private void BuildVotePanel()
    {
        _votePanel = UIHelper.CreateUIObject("VotePanel", _root.transform);
        var rt = _votePanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(340f, 200f);
        rt.anchoredPosition = Vector2.zero;

        _votePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var layout     = _votePanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 20, 20);
        layout.spacing = 12f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment         = TextAnchor.UpperCenter;

        UIHelper.CreateLabel(_votePanel.transform, "What next?",
            22f, FontStyles.Bold, Color.white, 36f);

        _voteStatusLabel = UIHelper.CreateLabel(_votePanel.transform,
            "Vote to play again or leave",
            14f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f), 24f);

        var buttonRow = UIHelper.CreateUIObject("ButtonRow", _votePanel.transform);
        var buttonRowLe = buttonRow.AddComponent<LayoutElement>();
        buttonRowLe.preferredHeight = 48f;
        buttonRowLe.minHeight       = 48f;
        var buttonRowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.spacing               = 12f;
        buttonRowLayout.childForceExpandWidth = false;
        buttonRowLayout.childAlignment        = TextAnchor.MiddleCenter;

        _rematchButton = UIHelper.BuildButton(buttonRow.transform, "Vote Rematch",
            new Color(0.2f, 0.75f, 0.35f), 150f, 40f);
        _rematchButton.onClick.AddListener(OnVoteRematch);

        _leaveButton = UIHelper.BuildButton(buttonRow.transform, "Leave Room",
            new Color(0.75f, 0.2f, 0.2f), 150f, 40f);
        _leaveButton.onClick.AddListener(OnLeaveRoom);

        _votePanel.SetActive(false);
    }

    // ---- Update ----

    public void UpdatePhase(GamePhase phase)
    {
        _currentPhase = phase;
        if (_phaseLabel == null) return;

        // Reset post-game state when returning to Waiting
        if (phase != GamePhase.GameOver)
        {
            _scoreboardHideTime = -1f;
            _votePanelShown     = false;
            if (_scoreboardPanel != null) _scoreboardPanel.SetActive(false);
            if (_votePanel       != null) _votePanel.SetActive(false);
            if (_panel           != null) _panel.SetActive(true);
        }

        switch (phase)
        {
            case GamePhase.Waiting:
                int currentPlayers = (NetworkRunner.Instances.Count > 0 && NetworkRunner.Instances[0] != null) 
                    ? NetworkRunner.Instances[0].ActivePlayers.Count() 
                    : 0;
                _phaseLabel.text  = currentPlayers < 2 
                    ? "Waiting for more players... (Need 2 to start)" 
                    : "Ready to start!";
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

        // Hide score panel during game over — scoreboard replaces it
        if (_currentPhase != GamePhase.Playing)
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
        if (_scoreboardPanel == null) return;

        // Keep timer panel visible — update phase label with countdown
        if (_panel != null) _panel.SetActive(true);
        if (_scorePanel != null) _scorePanel.SetActive(false);

        _scoreboardPanel.SetActive(true);
        if (_votePanel != null) _votePanel.SetActive(false);
        _votePanelShown     = false;
        _scoreboardHideTime = Time.time + 5f;

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

    public void UpdateVoteStatus(int votes, int total)
    {
        if (_voteStatusLabel == null) return;
        if (total == 0)               return;

        int needed = total / 2 + 1;
        _voteStatusLabel.text = votes == 0
            ? "Vote to play again or leave"
            : $"Rematch votes: {votes} / {total}  (need {needed})";
    }

    /// <summary>
    /// Called every Render() frame — handles the scoreboard to vote panel transition.
    /// </summary>
    public void Tick()
    {
        if (_currentPhase != GamePhase.GameOver) return;

        // Update countdown in the timer panel while scoreboard is showing
        if (!_votePanelShown && _scoreboardHideTime >= 0f && _phaseLabel != null)
        {
            int remaining = Mathf.CeilToInt(_scoreboardHideTime - Time.time);
            remaining = Mathf.Max(0, remaining);
            _phaseLabel.text  = $"Game Over — voting in {remaining}...";
            _timerLabel.text  = "";
        }

        if (_votePanelShown)     return;
        if (_scoreboardHideTime < 0f) return;
        if (Time.time < _scoreboardHideTime) return;

        // Scoreboard time elapsed — hide timer panel, show vote panel
        if (_scoreboardPanel != null) _scoreboardPanel.SetActive(false);
        if (_panel           != null) _panel.SetActive(false); // Hide game over panel

        if (_votePanel != null)
        {
            _votePanel.SetActive(true);
            if (_rematchButton   != null) _rematchButton.interactable   = true;
            if (_leaveButton     != null) _leaveButton.interactable     = true;
            if (_voteStatusLabel != null)
                _voteStatusLabel.text = "Vote to play again or leave";
        }

        _votePanelShown     = true;
        _scoreboardHideTime = -1f;
    }

    public void Destroy()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root);
    }

    // ---- Button handlers ----

    private void OnVoteRematch()
    {
        if (_rematchButton != null) _rematchButton.interactable = false;
        _onVoteRematchRequested?.Invoke();
    }

    private void OnLeaveRoom()
    {
        if (_leaveButton != null) _leaveButton.interactable = false;
        _onLeaveRoomRequested?.Invoke();
    }

    // ---- Helpers ----

    private static Color RankColor(int index)
    {
        switch (index)
        {
            case 0:  return new Color(1f,    0.84f, 0f,    1f);
            case 1:  return new Color(0.75f, 0.75f, 0.75f, 1f);
            case 2:  return new Color(0.8f,  0.5f,  0.2f,  1f);
            default: return new Color(0.3f,  0.3f,  0.3f,  1f);
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