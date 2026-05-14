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

    private const float RoundDuration = 60f;
    private const float CountdownDuration = 5f;
    private const int MinPlayers = 2;

    [Networked] public float TimeRemaining { get; set; }
    [Networked] public GamePhase Phase { get; set; }
    [Networked] public PlayerRef Winner { get; set; }
    [Networked] public int RematchVotes { get; set; }
    [Networked] public int TotalPlayers { get; set; }

    private ChangeDetector _changes;
    private GameHUD _hud;
    private bool _gameOverFired;
    private HashSet<PlayerRef> _voters = new HashSet<PlayerRef>();

    // ---- NetworkBehaviours ----

    public override void Spawned()
    {
        Instance = this;
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _gameOverFired = false;

        Runner.AddCallbacks(this);

        _hud = new GameHUD();
        _hud.Build(HandleVoteRematchRequested, HandleLeaveRoomRequested);

        Debug.Log($"GameStateManager: Spawned. Authority: {Object.StateAuthority}, IsMaster: {Runner.IsSharedModeMasterClient}");

        // If we are the master client and no one has authority, take it.
        if (Runner.IsSharedModeMasterClient && Object.StateAuthority == PlayerRef.None)
        {
            Debug.Log("GameStateManager: Master Client taking initial authority.");
            Object.RequestStateAuthority();
        }

        // Only reset if we are the authority AND the game hasn't started yet.
        // This prevents resetting mid-round state during host migration.
        // HOWEVER, if we are the ONLY player and the game is in Playing state, we should reset.
        if (HasStateAuthority)
        {
            if (Phase == GamePhase.Waiting || (Phase == GamePhase.Playing && Runner.ActivePlayers.Count() < MinPlayers))
            {
                ResetGame();
            }
        }

        _hud.UpdatePhase(Phase);
        _hud.UpdateTimer(TimeRemaining);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        runner.RemoveCallbacks(this);
        _hud?.Destroy();
        Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Ensure we check for player count regularly if we are the authority.
        // This handles cases where we just gained authority after host migration.
        TryAbortRound();

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
                TotalPlayers = Runner.ActivePlayers.Count();

                DetermineWinner();

                Rpc_GameOver(Winner);

                Debug.Log($"GameStateManager: Round over. Winner: {Winner}");
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
                case nameof(Winner):
                    if (Phase == GamePhase.GameOver)
                        ShowFinalRankings();
                    break;
                case nameof(RematchVotes):
                    _hud.UpdateVoteStatus(RematchVotes, TotalPlayers);
                    break;
            }
        }

        _hud.UpdateScores(Runner);
        _hud.Tick();
    }

    // ---- RPCs ----

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_GameOver(PlayerRef winner)
    {
        if (_gameOverFired) return;
        _gameOverFired = true;

        ShowFinalRankings();
        Debug.Log($"Rpc_GameOver received. Winner: {winner}");
    }

    /// <summary>
    /// Runs only on StateAuthority — keeps vote counting authoritative.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_CastVote(PlayerRef voter)
    {
        if (!HasStateAuthority) return;
        if (Phase != GamePhase.GameOver) return;
        if (_voters.Contains(voter)) return;

        _voters.Add(voter);
        RematchVotes++;

        Debug.Log($"Vote cast by {voter}. Votes: {RematchVotes}/{TotalPlayers}");

        if (RematchVotes > TotalPlayers / 2)
            Rpc_StartRematch();
    }

    /// <summary>
    /// Resets game state on all clients.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_StartRematch()
    {
        Debug.Log("Rpc_StartRematch: Resetting round.");
        if (HasStateAuthority)
        {
            ResetGame();
            // Immediately try to start if enough players are still in room
            TryStartRound();
        }
    }

    // Called directly by the HUD leave button
    public void LeaveRoom()
    {
        Debug.Log("LeaveRoom: Returning to lobby.");

        var lobbyManager = FindFirstObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobbyManager != null)
        {
            lobbyManager.ReturnToLobbyFromMatch();
            return;
        }

        if (Runner != null && Runner.IsRunning)
        {
            Runner.Shutdown(shutdownReason: ShutdownReason.Ok);
        }
    }

    // ---- Private helpers ----

    private void HandleVoteRematchRequested()
    {
        Rpc_CastVote(Runner.LocalPlayer);
    }

    private void HandleLeaveRoomRequested()
    {
        LeaveRoom();
    }

    private void ResetRoundToWaiting()
    {
        Phase = GamePhase.Waiting;
        TimeRemaining = 0f;
        Winner = PlayerRef.None;
        RematchVotes = 0;
        TotalPlayers = 0;
        _voters.Clear();
        _gameOverFired = false;

        // Force a HUD update immediately to ensure the message is visible
        if (_hud != null)
        {
            _hud.UpdatePhase(Phase);
            _hud.UpdateTimer(TimeRemaining);
        }
    }

    public void ResetGame()
    {
        Debug.Log("GameStateManager: Performing full game reset (scores and state).");
        
        ResetRoundToWaiting();

        // Reset scores for all players.
        var players = new List<Example.Player>();
        Runner.GetAllBehaviours(players);
        foreach (var p in players)
        {
            p.ResetScore();
        }

        // Call RPC to ensure all clients (including proxies) clean up their UI/Input state
        Rpc_OnGameReset();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_OnGameReset()
    {
        Debug.Log("GameStateManager: Rpc_OnGameReset received.");
        _gameOverFired = false;
        _voters.Clear();
        
        if (_hud != null)
        {
            _hud.UpdatePhase(Phase);
            _hud.UpdateTimer(TimeRemaining);
        }

        // Re-lock cursor if it was unlocked during GameOver
        if (Runner.GameMode == GameMode.Shared)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ShowFinalRankings()
    {
        var players = new List<Example.Player>();
        Runner.GetAllBehaviours(players);

        // Sort by score descending
        var ranked = players
            .OrderByDescending(p => p.Score)
            .Select((p, index) => (
                name: p.NameTag != null ? p.NameTag.NickName.Value : $"Player {index + 1}",
                score: p.Score,
                rank: index + 1
            ))
            .ToList();

        _hud.ShowRankings(ranked);
    }

    private void DetermineWinner()
    {
        // Determine winner — highest score wins
        var players = new List<Example.Player>();
        Runner.GetAllBehaviours(players);

        Example.Player topPlayer = null;
        foreach (var p in players)
        {
            if (topPlayer == null || p.Score > topPlayer.Score)
                topPlayer = p;
        }

        Winner = topPlayer != null
            ? topPlayer.Object.InputAuthority
            : PlayerRef.None;
    }

    private void TryStartRound()
    {
        if (!HasStateAuthority) return;

        // Only start the countdown if we are currently waiting
        if (Phase == GamePhase.Waiting && Runner.ActivePlayers.Count() >= MinPlayers)
        {
            Phase = GamePhase.Countdown;
            TimeRemaining = CountdownDuration;
            _gameOverFired = false;
            Debug.Log("GameStateManager: Starting countdown.");
        }
    }

    private void TryAbortRound()
    {
        if (!HasStateAuthority) return;

        int currentPlayers = Runner.ActivePlayers.Count();
        bool notEnoughPlayers = currentPlayers < MinPlayers;

        // If in Countdown and players drop below minimum, reset
        if (Phase == GamePhase.Countdown && notEnoughPlayers)
        {
            ResetGame();
            Debug.Log("GameStateManager: Match aborted - not enough players during countdown.");
        }

        // If in Playing and players drop below minimum, reset (Host leaving usually triggers this)
        if (Phase == GamePhase.Playing && notEnoughPlayers)
        {
            ResetGame();
            Debug.Log("GameStateManager: Match aborted - not enough players to continue.");
        }

        // If in GameOver and players drop to ZERO, reset
        if (Phase == GamePhase.GameOver && currentPlayers == 0)
        {
            ResetGame();
            Debug.Log("GameStateManager: Game over state cleared - all players left.");
        }
    }

    // Callbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"GameStateManager: Player Joined {player}. Active Players: {Runner.ActivePlayers.Count()}");
        
        // If we are the Master Client and don't have authority, try to take it.
        if (Runner.IsSharedModeMasterClient && !HasStateAuthority)
        {
            Debug.Log("GameStateManager: Master Client requesting authority on player join.");
            Object.RequestStateAuthority();
        }

        TryStartRound();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"GameStateManager: Player Left {player}. Authority was: {Object.StateAuthority}");

        // Request authority takeover for this object using shared utility logic.
        Example.NetworkUtils.RequestAuthorityIfOwnerLeft(Object, player, "GameStateManager");

        // Try to abort if we have authority, or we'll try again once authority is granted.
        TryAbortRound();
    }

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
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) 
    {
        Debug.Log("GameStateManager: OnHostMigration callback received. Game will likely reset on new runner.");
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
}