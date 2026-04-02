using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EmoteType { Wave = 0, Cheer = 1, Taunt = 2 }

public class PlayerNameTag : NetworkBehaviour
{
    [Tooltip("How far above the player root the label floats.")]
    public float HeightOffset = 2.2f;

    [Tooltip("How long the emote label stays visible.")]
    public float EmoteDuration = 2.5f;

    [Networked]
    public NetworkString<_32> NickName { get; set; }

    private ChangeDetector _changes;

    // Name tag
    private GameObject _labelRoot;
    private TMP_Text   _labelText;

    // Emote bubble
    private GameObject _bubbleRoot;
    private TMP_Text   _bubbleText;
    private float      _bubbleHideTime = -1f;

    private static readonly string[] EmoteStrings =
    {
        "o/ Wave",
        "\\o/ Cheer",
        ">:) Taunt",
    };

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        BuildLabel();
        BuildEmoteBubble();

        if (HasStateAuthority)
        {
            string name = PlayerPrefs.GetString("PlayerName", UnityServiceManager.PlayerName);
            NickName = string.IsNullOrEmpty(name)
                ? $"Player_{Random.Range(1000, 9999)}"
                : name;
        }

        ApplyNickName();
        _labelRoot.SetActive(true);
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(NickName):
                    ApplyNickName();
                    break;
            }
        }

        if (_labelRoot == null) return;

        Vector3 basePos = transform.position + Vector3.up * HeightOffset;
        _labelRoot.transform.position = basePos;

        if (_bubbleRoot != null)
        {
            // Hide bubble once timer expires — no coroutine needed
            if (_bubbleRoot.activeSelf && _bubbleHideTime >= 0f
                && Time.time >= _bubbleHideTime)
            {
                _bubbleRoot.SetActive(false);
                _bubbleHideTime = -1f;
            }

            _bubbleRoot.transform.position = basePos + Vector3.up * 0.6f;
        }

        if (Camera.main != null)
        {
            _labelRoot.transform.forward = Camera.main.transform.forward;
            if (_bubbleRoot != null)
                _bubbleRoot.transform.forward = Camera.main.transform.forward;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_labelRoot != null) Destroy(_labelRoot);
        if (_bubbleRoot != null) Destroy(_bubbleRoot);
    }

    // Called by Rpc_PlayEmote on every peer
    public void ShowEmote(EmoteType emote)
    {
        _bubbleText.text = EmoteStrings[(int)emote];
        _bubbleRoot.SetActive(true);
        _bubbleHideTime = Time.time + EmoteDuration;
    }

    private void ApplyNickName()
    {
        if (_labelText != null)
            _labelText.text = NickName.Value;
    }

    private void BuildLabel()
    {
        _labelRoot = new GameObject("NameTag");
        _labelRoot.transform.position = transform.position + Vector3.up * HeightOffset;

        var canvas        = _labelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var rt        = _labelRoot.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(200f, 40f);
        rt.localScale = Vector3.one * 0.01f;

        var bgGo       = new GameObject("Background");
        bgGo.transform.SetParent(_labelRoot.transform, false);
        var bgImg      = bgGo.AddComponent<Image>();
        bgImg.color    = new Color(0f, 0f, 0f, 0.55f);
        var bgRt       = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var textGo           = new GameObject("Label");
        textGo.transform.SetParent(_labelRoot.transform, false);
        _labelText           = textGo.AddComponent<TextMeshProUGUI>();
        _labelText.fontSize  = 18f;
        _labelText.color     = Color.white;
        _labelText.fontStyle = FontStyles.Bold;
        _labelText.alignment = TextAlignmentOptions.Center;
        _labelText.raycastTarget = false;

        var textRt       = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4f, 0f);
        textRt.offsetMax = new Vector2(-4f, 0f);
    }

    private void BuildEmoteBubble()
    {
        _bubbleRoot = new GameObject("EmoteBubble");
        _bubbleRoot.transform.position =
            transform.position + Vector3.up * (HeightOffset + 0.6f);

        var canvas        = _bubbleRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 6;

        var rt        = _bubbleRoot.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(220f, 50f);
        rt.localScale = Vector3.one * 0.01f;

        var bgGo       = new GameObject("Background");
        bgGo.transform.SetParent(_bubbleRoot.transform, false);
        var bgImg      = bgGo.AddComponent<Image>();
        bgImg.color    = new Color(1f, 1f, 1f, 0.92f);
        var bgRt       = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var textGo           = new GameObject("EmoteLabel");
        textGo.transform.SetParent(_bubbleRoot.transform, false);
        _bubbleText          = textGo.AddComponent<TextMeshProUGUI>();
        _bubbleText.fontSize = 20f;
        _bubbleText.color    = new Color(0.1f, 0.1f, 0.1f);
        _bubbleText.fontStyle  = FontStyles.Bold;
        _bubbleText.alignment  = TextAlignmentOptions.Center;
        _bubbleText.raycastTarget = false;

        var textRt       = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 0f);
        textRt.offsetMax = new Vector2(-6f, 0f);

        _bubbleRoot.SetActive(false);
    }
}