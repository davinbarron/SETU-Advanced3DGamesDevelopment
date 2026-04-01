using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Joins a Fusion lobby (without entering a room) to receive the live session list.
/// Call <see cref="ShowLobby"/> after authentication to open the browser.
/// Call <see cref="JoinRoom"/> or <see cref="CreateRoom"/> to hand off to FusionBootstrap.
/// </summary>
public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>Fired whenever Fusion delivers a fresh session list.</summary>
    public event Action<List<SessionInfo>> OnRoomsUpdated;

    /// <summary>Fired once the lobby connection is established and rooms can be created or joined.</summary>
    public event Action OnLobbyReady;

    // -------------------------------------------------------------------------
    // Public state
    // -------------------------------------------------------------------------

    /// <summary>True once the lobby runner has successfully connected.</summary>
    public bool IsReady { get; private set; }

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private NetworkRunner   _lobbyRunner;
    private FusionBootstrap _bootstrap;
    private string          _playerId;
    private string          _accessToken;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initialise the manager and start listening for rooms.
    /// Called by <see cref="UnityServicesManager"/> right after sign-in.
    /// </summary>
    public void ShowLobby(FusionBootstrap bootstrap, string playerId, string accessToken)
    {
        _bootstrap   = bootstrap;
        _playerId    = playerId;
        _accessToken = accessToken;

        StartCoroutine(StartLobbyRunner());
    }

    /// <summary>Join an existing room by name and hand control to FusionBootstrap.</summary>
    public void JoinRoom(string roomName)
    {
        StartCoroutine(ConnectToRoom(roomName));
    }

    /// <summary>Create (or re-create) a room with the given name.</summary>
    public void CreateRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            roomName = Guid.NewGuid().ToString("N").Substring(0, 8);

        StartCoroutine(ConnectToRoom(roomName));
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private IEnumerator StartLobbyRunner()
    {
        var go = new GameObject("LobbyRunner");
        DontDestroyOnLoad(go);
        _lobbyRunner = go.AddComponent<NetworkRunner>();
        _lobbyRunner.AddCallbacks(this);

        var auth = BuildAuthValues();

        // Positional args: (SessionLobby, string lobbyName, AuthenticationValues, ...)
        var task = _lobbyRunner.JoinSessionLobby(SessionLobby.Shared, null, auth);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            Debug.LogError($"[LobbyManager] Failed to join lobby: {task.Exception}");
            Destroy(go);
            yield break;
        }

        IsReady = true;
        OnLobbyReady?.Invoke();
    }

    private IEnumerator ConnectToRoom(string roomName)
    {
        if (_lobbyRunner != null && _lobbyRunner.IsRunning)
        {
            var shutdownTask = _lobbyRunner.Shutdown();
            while (!shutdownTask.IsCompleted)
                yield return null;
        }

        if (_lobbyRunner != null)
        {
            Destroy(_lobbyRunner.gameObject);
            _lobbyRunner = null;
        }

        _bootstrap.theUserID      = _playerId;
        _bootstrap.theAccessToken = _accessToken;
        _bootstrap.DefaultRoomName = roomName;
        _bootstrap.StartSharedClient();
    }

    private AuthenticationValues BuildAuthValues()
    {
        var auth = new AuthenticationValues();
        auth.AuthType = CustomAuthenticationType.Custom;
        auth.AddAuthParameter("id",    _playerId);
        auth.AddAuthParameter("token", _accessToken);
        return auth;
    }

    // -------------------------------------------------------------------------
    // INetworkRunnerCallbacks
    // -------------------------------------------------------------------------

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        OnRoomsUpdated?.Invoke(sessionList);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}