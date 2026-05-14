using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Example;

/// <summary>
/// Handles the gameplay logic for collectible orbs (scoring and respawning).
/// Delegates visual "Dissolve" effects to the BurnAndDissolveA2 component if present.
/// </summary>
public class ScoreOrb : NetworkBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float _collectRadius = 1.5f;
    [SerializeField] private int   _scoreValue    = 10;
    [SerializeField] private float _respawnTime   = 10f;

    [Networked] public bool  IsCollected  { get; set; }
    [Networked] public float RespawnTimer { get; set; }

    private ChangeDetector _changes;
    private Collider       _collider;
    private Semester2.BurnAndDissolveA2 _dissolveEffect;

    public override void Spawned()
    {
        _changes   = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _collider  = GetComponent<Collider>();
        
        // Find and configure the visual dissolve effect
        _dissolveEffect = GetComponent<Semester2.BurnAndDissolveA2>();
        if (_dissolveEffect != null)
        {
            // Orbs manage their own lifecycle, so we tell the effect to be passive.
            // This must be set for all clients to prevent accidental triggers.
            _dissolveEffect.independentMode = false;
        }

        if (HasStateAuthority)
        {
            IsCollected  = false;
            RespawnTimer = 0f;
        }

        ApplyCollisionState();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // --- Respawn Logic ---
        if (IsCollected)
        {
            RespawnTimer -= Runner.DeltaTime;
            if (RespawnTimer <= 0f)
            {
                IsCollected  = false;
                RespawnTimer = 0f;
                
                // Visual reset
                if (_dissolveEffect != null)
                    _dissolveEffect.ResetDissolve();
            }
            return;
        }

        // --- Collection Logic ---
        if (GameStateManager.Instance == null || GameStateManager.Instance.Phase != GamePhase.Playing) 
            return;

        var hits = Physics.OverlapSphere(transform.position, _collectRadius);
        foreach (var hit in hits)
        {
            if (NetworkUtils.TryGetPlayer(hit, out var player))
            {
                Collect(player);
                break;
            }
        }
    }

    private void Collect(Example.Player player)
    {
        if (!HasStateAuthority) return;

        IsCollected  = true;
        RespawnTimer = _respawnTime;

        // Delegate visuals to the specialized shader script
        if (_dissolveEffect != null)
            _dissolveEffect.StartDissolve();

        // Grant score via RPC to the player's authority
        Rpc_GrantScore(player.Object.InputAuthority);
        Debug.Log($"[ScoreOrb] {gameObject.name} collected by P{player.Object.InputAuthority.PlayerId}.");
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsCollected):
                    ApplyCollisionState();
                    break;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_GrantScore(PlayerRef targetPlayer)
    {
        var playerObject = Runner.GetPlayerObject(targetPlayer);
        if (playerObject != null && playerObject.TryGetComponent<Example.Player>(out var player))
        {
            player.AddScore(_scoreValue);
        }
    }

    private void ApplyCollisionState()
    {
        // Collider is always managed by the ScoreOrb logic to prevent double-collection
        if (_collider != null)
            _collider.enabled = !IsCollected;

        // If no dissolve effect is present, we provide a fallback simple visibility toggle
        if (_dissolveEffect == null)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = !IsCollected;
        }
    }
}