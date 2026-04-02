using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class ScoreOrb : NetworkBehaviour
{
    [SerializeField] private float _collectRadius = 1.5f;
    [SerializeField] private int   _scoreValue    = 10;
    [SerializeField] private float _respawnTime   = 10f;

    [Networked] public bool  IsCollected  { get; set; }
    [Networked] public float RespawnTimer { get; set; }

    private ChangeDetector _changes;
    private Renderer[]     _renderers;
    private Collider       _collider;

    public override void Spawned()
    {
        _changes   = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _renderers = GetComponentsInChildren<Renderer>();
        _collider  = GetComponent<Collider>();

        if (HasStateAuthority)
        {
            IsCollected  = false;
            RespawnTimer = 0f;
        }

        // Apply initial visual state
        ApplyVisual();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // --- Respawn countdown ---
        if (IsCollected)
        {
            RespawnTimer -= Runner.DeltaTime;
            if (RespawnTimer <= 0f)
            {
                IsCollected  = false;
                RespawnTimer = 0f;
                Debug.Log($"ScoreOrb {gameObject.name}: Respawned.");
            }
            return;
        }

        // --- Overlap check for nearby players ---
        if (GameStateManager.Instance == null) return;
        if (GameStateManager.Instance.Phase != GamePhase.Playing) return;

        var hits = Physics.OverlapSphere(transform.position, _collectRadius);

        foreach (var hit in hits)
        {
            var player = hit.GetComponentInParent<Example.Player>();
            if (player == null) continue;

            // Mark collected and start respawn timer
            IsCollected  = true;
            RespawnTimer = _respawnTime;

            // Grant score using RPC so the correct authority client writes it
            Rpc_GrantScore(player.Object.InputAuthority);
            Debug.Log($"ScoreOrb {gameObject.name}: Collected by {player.Object.InputAuthority}.");
            break; // Only one player can collect
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsCollected):
                    ApplyVisual();
                    break;
            }
        }
    }

    // -- RPC triggered by orb StateAuthority, runs on ALL peers. --
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_GrantScore(PlayerRef targetPlayer)
    {
        // Find the player object
        var playerObject = Runner.GetPlayerObject(targetPlayer);
        if (playerObject == null) return;

        var player = playerObject.GetComponent<Example.Player>();
        if (player == null) return;

        // Only the client that owns this player object writes the score
        player.AddScore(_scoreValue);
    }

    private void ApplyVisual()
    {
        bool visible = !IsCollected;
        foreach (var r in _renderers)
            r.enabled = visible;

        if (_collider != null)
            _collider.enabled = visible;
    }
}