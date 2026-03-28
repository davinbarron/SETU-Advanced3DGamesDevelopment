using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LobbyManager))]
public class RoomBrowserUI : MonoBehaviour
{
    private LobbyManager   _lobbyManager;
    private GameObject     _panel;
    private Transform      _rowContainer;
    private TMP_InputField _nameInput;
    private TMP_InputField _roomNameInput;
    private TMP_Text       _statusLabel;
    private Button         _createButton;

    private readonly List<RoomListItem> _rows = new List<RoomListItem>();

    private void Awake()
    {
        _lobbyManager = GetComponent<LobbyManager>();
        _lobbyManager.OnLobbyReady   += OnLobbyReady;
        _lobbyManager.OnRoomsUpdated += RefreshRoomList;

        BuildUI();
        SetStatus("Connecting to lobby...");
    }

    private void OnDestroy()
    {
        if (_lobbyManager == null) return;
        _lobbyManager.OnLobbyReady   -= OnLobbyReady;
        _lobbyManager.OnRoomsUpdated -= RefreshRoomList;
    }

    public void Hide() => _panel.SetActive(false);

    private void OnLobbyReady()
    {
        _createButton.interactable = true;
        SetStatus("Connected — create or join a room.");
    }

    private void RefreshRoomList(List<SessionInfo> sessions)
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        if (sessions == null || sessions.Count == 0)
        {
            SetStatus("No open rooms. Create one to get started!");
            return;
        }

        SetStatus($"{sessions.Count} room(s) available.");

        foreach (var session in sessions)
        {
            if (!session.IsVisible) continue;
            var captured = session.Name;
            var item = RoomListItem.Create(_rowContainer, session,
                onJoin: () => _lobbyManager.JoinRoom(captured));
            _rows.Add(item);
        }
    }

    private void OnCreateClicked()
    {
        // Save whatever name is in the field before joining
        UnityServiceManager.PlayerName = string.IsNullOrWhiteSpace(_nameInput.text)
            ? UnityServiceManager.PlayerName
            : _nameInput.text.Trim();

        _lobbyManager.CreateRoom(_roomNameInput.text.Trim());
    }

    private void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("RoomBrowserCanvas");
        DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 10;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System
        var esGo = new GameObject("EventSystem");
        DontDestroyOnLoad(esGo);
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        Debug.Log("EventSystem created by RoomBrowserUI.");

        // Backdrop
        var backdrop   = CreateUIObject("Backdrop", canvasGo.transform);
        StretchFull(backdrop);
        var bgImg      = backdrop.AddComponent<Image>();
        bgImg.color    = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;

        // Panel
        _panel = CreateUIObject("Panel", canvasGo.transform);
        var panelRt       = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot     = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 460f);
        var panelImg      = _panel.AddComponent<Image>();
        panelImg.color    = new Color(0.1f, 0.1f, 0.15f, 0.97f);

        var layout     = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 10f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        // Title
        AddLabel(_panel.transform, "Room Browser", 28f, FontStyles.Bold,
            TextAlignmentOptions.Center, 44f);

        // Player name row
        var nameRow = CreateRow(_panel.transform, 44f);
        AddLabel(nameRow.transform, "Your Name:", 16f,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft, preferredWidth: 100f);
        _nameInput = BuildInputField(nameRow.transform,
            UnityServiceManager.PlayerName ?? "Player", 340f);
        _nameInput.onEndEdit.AddListener(val =>
        {
            if (!string.IsNullOrWhiteSpace(val))
                UnityServiceManager.PlayerName = val.Trim();
        });

        // Status
        var statusGo     = CreateUIObject("Status", _panel.transform);
        var statusLe     = statusGo.AddComponent<LayoutElement>();
        statusLe.preferredHeight = 24f;
        _statusLabel     = statusGo.AddComponent<TextMeshProUGUI>();
        _statusLabel.fontSize  = 13f;
        _statusLabel.color     = new Color(0.75f, 0.75f, 0.75f);
        _statusLabel.alignment = TextAlignmentOptions.Center;

        // Scroll view for room list
        var scrollView = BuildScrollView(_panel.transform, 280f);
        var scrollLe   = scrollView.AddComponent<LayoutElement>();
        scrollLe.preferredHeight = 280f;
        scrollLe.flexibleHeight  = 1f;

        // Create room row
        var createRow = CreateRow(_panel.transform, 48f);
        _roomNameInput = BuildInputField(createRow.transform, "Room name (optional)...", 340f);
        _createButton  = BuildButton(createRow.transform, "Create Room",
            new Color(0.2f, 0.75f, 0.35f), 160f);
        _createButton.interactable = false;
        _createButton.onClick.AddListener(OnCreateClicked);
    }

    private GameObject BuildScrollView(Transform parent, float height)
    {
        var scrollGo   = CreateUIObject("ScrollView", parent);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        var scrollImg  = scrollGo.AddComponent<Image>();
        scrollImg.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);

        var viewport = CreateUIObject("Viewport", scrollGo.transform);
        StretchFull(viewport);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        var content   = CreateUIObject("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = Vector2.zero;

        var contentLayout     = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
        contentLayout.childForceExpandWidth  = true;
        contentLayout.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content    = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;

        _rowContainer = content.transform;
        return scrollGo;
    }

    private static TMP_InputField BuildInputField(Transform parent, string placeholder, float width)
    {
        var go = CreateUIObject("InputField", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = 40f;
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

    private static Button BuildButton(Transform parent, string label, Color color, float width)
    {
        var go = CreateUIObject("Button", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = 40f;

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

    private static void AddLabel(Transform parent, string text, float fontSize,
        FontStyles style, TextAlignmentOptions alignment,
        float preferredHeight = 30f, float preferredWidth = -1f)
    {
        var go = CreateUIObject("Label", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        if (preferredWidth > 0f) le.preferredWidth = preferredWidth;

        var t       = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = Color.white;
        t.alignment = alignment;
        t.raycastTarget = false;
    }

    private static GameObject CreateRow(Transform parent, float height)
    {
        var go     = CreateUIObject("Row", parent);
        var le     = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        var hl     = go.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8f;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;
        hl.childAlignment = TextAnchor.MiddleLeft;
        return go;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
            _statusLabel.text = msg;
    }
}