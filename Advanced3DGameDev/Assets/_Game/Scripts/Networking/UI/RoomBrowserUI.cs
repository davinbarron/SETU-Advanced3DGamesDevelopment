using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and manages the room browser panel entirely in code – no prefab required.
/// Attach this to the same GameObject as <see cref="LobbyManager"/> (or any persistent GO).
/// </summary>
[RequireComponent(typeof(LobbyManager))]
public class RoomBrowserUI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("Maximum players allowed when creating a new room.")]
    [SerializeField] private int _defaultMaxPlayers = 4;

    // -------------------------------------------------------------------------
    // Private UI references (built in Awake)
    // -------------------------------------------------------------------------

    private Canvas          _canvas;
    private GameObject      _panel;
    private Transform       _rowContainer;
    private TMP_InputField  _roomNameInput;
    private TMP_Text        _statusLabel;
    private Button          _createButton;
    private TMP_InputField  _nameInput;

    private LobbyManager            _lobbyManager;
    private List<RoomListItem>      _rows = new List<RoomListItem>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _lobbyManager = GetComponent<LobbyManager>();
        _lobbyManager.OnRoomsUpdated += RefreshRoomList;
        _lobbyManager.OnLobbyReady   += OnLobbyReady;

        BuildUI();
        Show();
        SetStatus("Connecting to lobby...");
    }

    private void OnDestroy()
    {
        if (_lobbyManager != null)
        {
            _lobbyManager.OnRoomsUpdated -= RefreshRoomList;
            _lobbyManager.OnLobbyReady   -= OnLobbyReady;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Show the browser panel.</summary>
    public void Show() => _panel.SetActive(true);

    /// <summary>Hide the browser panel (e.g. after joining a room).</summary>
    public void Hide() => _panel.SetActive(false);

    // -------------------------------------------------------------------------
    // Room list
    // -------------------------------------------------------------------------

    private void RefreshRoomList(List<SessionInfo> sessions)
    {
        // Remove old rows.
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        if (sessions == null || sessions.Count == 0)
        {
            SetStatus("No open rooms found. Create one!");
            return;
        }

        SetStatus($"{sessions.Count} room(s) available");

        foreach (var session in sessions)
        {
            if (!session.IsVisible) continue;

            var capturedName = session.Name;
            var item = RoomListItem.Create(_rowContainer, session,
                onJoin: () =>
                {
                    Hide();
                    _lobbyManager.JoinRoom(capturedName);
                });
            _rows.Add(item);
        }
    }

    // -------------------------------------------------------------------------
    // Button handlers
    // -------------------------------------------------------------------------

    private void OnLobbyReady()
    {
        _createButton.interactable = true;
        SetStatus("Connected: create or join a room.");
    }

    private void OnCreateClicked()
    {
        // Save whatever name is in the field before joining
        UnityServiceManager.PlayerName = string.IsNullOrWhiteSpace(_nameInput.text)
            ? UnityServiceManager.PlayerName
            : _nameInput.text.Trim();

        string roomName = _roomNameInput.text.Trim();
        Hide();
        _lobbyManager.CreateRoom(roomName);
    }

    // -------------------------------------------------------------------------
    // UI construction
    // -------------------------------------------------------------------------

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

        // Backdrop
        var backdrop = UIHelper.CreateUIObject("Backdrop", canvasGo.transform);
        UIHelper.StretchFull(backdrop);
        var bgImg = backdrop.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;

        // Panel
        _panel = UIHelper.CreateUIObject("Panel", canvasGo.transform);
        var panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot     = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(520f, 460f);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.97f);

        var layout = _panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 10f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        // Title
        var titleLabel = UIHelper.CreateLabel(_panel.transform, "Room Browser", 28f, FontStyles.Bold,
            Color.white);
            
        titleLabel.alignment = TextAlignmentOptions.Center;

        // Player name row
        var nameRow = CreateRow(_panel.transform, 44f);

        var nameLabel = UIHelper.CreateLabel(nameRow.transform, "Your Name:", 16f,
            FontStyles.Normal, Color.white);

        nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
        
        _nameInput = UIHelper.BuildInputField(nameRow.transform,
            UnityServiceManager.PlayerName ?? "Player", 340f);
        _nameInput.onEndEdit.AddListener(val =>
        {
            if (!string.IsNullOrWhiteSpace(val))
                UnityServiceManager.PlayerName = val.Trim();
        });

        // Status
        var statusGo = UIHelper.CreateUIObject("Status", _panel.transform);
        var statusLe = statusGo.AddComponent<LayoutElement>();
        statusLe.preferredHeight = 24f;
        _statusLabel = statusGo.AddComponent<TextMeshProUGUI>();
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
        _roomNameInput = UIHelper.BuildInputField(createRow.transform, "Room name (optional)...", 340f);
        _createButton  = UIHelper.BuildButton(createRow.transform, "Create Room",
            new Color(0.2f, 0.75f, 0.35f), 160f);
        _createButton.interactable = false;
        _createButton.onClick.AddListener(OnCreateClicked);
    }

    // ---- Scroll view helper ----
    private GameObject BuildScrollView(Transform parent, float height)
    {
        var scrollGo = UIHelper.CreateUIObject("ScrollView", parent);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        var scrollImg  = scrollGo.AddComponent<Image>();
        scrollImg.color = new Color(0.05f, 0.05f, 0.1f, 0.5f);

        var viewport = UIHelper.CreateUIObject("Viewport", scrollGo.transform);
        UIHelper.StretchFull(viewport);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        var content   = UIHelper.CreateUIObject("Content", viewport.transform);
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

    private static GameObject CreateRow(Transform parent, float height)
    {
        var go     = UIHelper.CreateUIObject("Row", parent);
        var le     = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        var hl     = go.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8f;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;
        hl.childAlignment = TextAnchor.MiddleLeft;
        return go;
    }

    private void SetStatus(string msg)
    {
        if (_statusLabel != null)
            _statusLabel.text = msg;
    }
}