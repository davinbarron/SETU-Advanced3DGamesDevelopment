using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public enum GamePhase
{
    Waiting,
    Countdown,
    Playing,
    GameOver
}

public class GameStateManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static GameStateManager Instance { get; private set; }

    private const float RoundDuration     = 60f;
    private const float CountdownDuration = 5f;
    private const int MinPlayers          = 2;

    [Networked] public float TimeRemaining  { get; set; }
    [Networked] public GamePhase Phase      { get; set; }

    private ChangeDetector _changes;
    private GameHUD         _hud;

    public override void Spawned()
    {
        Instance = this;
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        Runner.AddCallbacks(this);

        _hud = new GameHUD();
        _hud.Build();

        if (HasStateAuthority)
        {
            Phase         = GamePhase.Waiting;
            TimeRemaining = 0;
        }

        _hud.UpdatePhase(Phase);
        _hud.UpdateTimer(TimeRemaining);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // --- Logic for Countdown ---
        if (Phase == GamePhase.Countdown)
        {
            TimeRemaining -= Runner.DeltaTime;
            if (TimeRemaining <= 0f)
            {
                Phase = GamePhase.Playing;
                TimeRemaining = RoundDuration;
                Debug.Log("GameStateManager: Match Started!");
            }
        }
        // --- Logic for Playing ---
        else if (Phase == GamePhase.Playing)
        {
            TimeRemaining -= Runner.DeltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                Phase = GamePhase.GameOver;
                Debug.Log("GameStateManager: Round over.");
            }
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Phase):
                    _hud.UpdatePhase(Phase);
                    break;
                case nameof(TimeRemaining):
                    _hud.UpdateTimer(TimeRemaining);
                    break;
            }
        }

        _hud.UpdateScores(Runner);
    }

    private void TryStartRound()
    {
        if (!HasStateAuthority) return;
        
        // Only start the countdown if we are currently waiting
        if (Phase == GamePhase.Waiting && Runner.ActivePlayers.Count() >= MinPlayers)
        {
            Phase = GamePhase.Countdown;
            TimeRemaining = CountdownDuration;
            Debug.Log("GameStateManager: Starting countdown.");
        }
    }

    private void TryAbortRound()
    {
        if (!HasStateAuthority) return;

        // If a player leaves during Countdown OR Playing, stop the game
        if ((Phase == GamePhase.Playing || Phase == GamePhase.Countdown) && 
            Runner.ActivePlayers.Count() < MinPlayers)
        {
            Phase = GamePhase.Waiting;
            TimeRemaining = 0;
            Debug.Log("GameStateManager: Match aborted - not enough players.");
        }
    }

    // Callbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) => TryStartRound();
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)   => TryAbortRound();
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
}