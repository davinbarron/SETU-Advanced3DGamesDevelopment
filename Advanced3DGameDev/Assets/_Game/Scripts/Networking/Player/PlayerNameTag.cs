using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameTag : NetworkBehaviour
{
    [Tooltip("How far above the player root the label floats.")]
    public float HeightOffset = 2.2f;

    [Networked]
    public NetworkString<_32> NickName { get; set; }

    private ChangeDetector _changes;
    private GameObject     _labelRoot;
    private TMP_Text       _labelText;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        BuildLabel();

        if (HasStateAuthority)
        {
            NickName = UnityServiceManager.PlayerName ?? $"Player_{Random.Range(1000, 9999)}";
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

        // Keep label floating above player and facing the camera
        _labelRoot.transform.position = transform.position + Vector3.up * HeightOffset;

        if (Camera.main != null)
            _labelRoot.transform.forward = Camera.main.transform.forward;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_labelRoot != null) Destroy(_labelRoot);
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
}