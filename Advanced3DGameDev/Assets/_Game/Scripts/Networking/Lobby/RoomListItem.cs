using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    private TMP_Text _nameLabel;
    private TMP_Text _playersLabel;
    private Button   _joinButton;
    private Action   _onJoin;

    public static RoomListItem Create(Transform parent, SessionInfo session, Action onJoin)
    {
        var go = UIHelper.CreateUIObject("RoomItem", parent);

        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment        = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.spacing               = 10f;
        layout.padding               = new RectOffset(8, 8, 4, 4);
        go.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var bg    = go.AddComponent<Image>();
        bg.color  = new Color(0f, 0f, 0f, 0.35f);

        var item     = go.AddComponent<RoomListItem>();
        item._onJoin = onJoin;

        item._nameLabel = UIHelper.CreateLabel(go.transform, session.Name,
            16f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft, 36f, 200f);
        item._playersLabel = UIHelper.CreateLabel(go.transform,
            $"{session.PlayerCount}/{session.MaxPlayers}",
            16f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft, 36f, 100f);

        var joinColors = new ColorBlock
        {
            normalColor      = new Color(0.2f, 0.6f, 1f),
            highlightedColor = new Color(0.35f, 0.75f, 1f),
            pressedColor     = new Color(0.1f, 0.4f, 0.8f),
            disabledColor    = new Color(0.4f, 0.4f, 0.4f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };

        item._joinButton = UIHelper.BuildButton(go.transform, "Join",
            joinColors, 90f, 36f, 16f, "JoinButton");
        item._joinButton.onClick.AddListener(() => item._onJoin?.Invoke());

        item.Refresh(session);
        return item;
    }

    public void Refresh(SessionInfo session)
    {
        _nameLabel.text      = session.Name;
        _playersLabel.text   = $"{session.PlayerCount} / {session.MaxPlayers}";
        _joinButton.interactable = session.IsOpen;
    }
}