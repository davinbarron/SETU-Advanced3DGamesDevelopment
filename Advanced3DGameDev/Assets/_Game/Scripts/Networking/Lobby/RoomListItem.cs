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
        var go     = new GameObject("RoomItem", typeof(RectTransform));
        go.transform.SetParent(parent, false);

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

        item._nameLabel    = CreateLabel(go.transform, session.Name, 200f);
        item._playersLabel = CreateLabel(go.transform,
            $"{session.PlayerCount}/{session.MaxPlayers}", 100f);
        item._joinButton   = CreateButton(go.transform, "Join",
            () => item._onJoin?.Invoke());

        item.Refresh(session);
        return item;
    }

    public void Refresh(SessionInfo session)
    {
        _nameLabel.text      = session.Name;
        _playersLabel.text   = $"{session.PlayerCount} / {session.MaxPlayers}";
        _joinButton.interactable = session.IsOpen;
    }

    private static TMP_Text CreateLabel(Transform parent, string text, float width)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = 36f;
        var t       = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = 16f;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        return t;
    }

    private static Button CreateButton(Transform parent, string label, Action onClick)
    {
        var go = new GameObject("JoinButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 90f;
        le.preferredHeight = 36f;

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 1f);
        var btn   = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = new Color(0.2f, 0.6f, 1f),
            highlightedColor = new Color(0.35f, 0.75f, 1f),
            pressedColor     = new Color(0.1f, 0.4f, 0.8f),
            disabledColor    = new Color(0.4f, 0.4f, 0.4f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
        btn.onClick.AddListener(() => onClick?.Invoke());

        var lblGo = new GameObject("Text", typeof(RectTransform));
        lblGo.transform.SetParent(go.transform, false);
        var rt       = lblGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t        = lblGo.AddComponent<TextMeshProUGUI>();
        t.text       = label;
        t.fontSize   = 16f;
        t.color      = Color.white;
        t.alignment  = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return btn;
    }
}