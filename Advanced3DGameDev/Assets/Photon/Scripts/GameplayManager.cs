using System;
using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine;
using Fusion;

namespace Example
{
	/// <summary>
	/// Main entry point for gameplay logic and spawning players.
	/// There exists only ONE instance spawned in the scene.
	/// </summary>
	public sealed class GameplayManager : NetworkBehaviour, INetworkRunnerCallbacks
	{
		public NetworkObject PlayerPrefab;

		public override void Spawned()
		{
			Runner.AddCallbacks(this);

			if (Runner.GameMode == GameMode.Shared)
			{
				// In Shared mode every player spawn the player object on their own.
				SpawnPlayer(Runner.LocalPlayer);
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			runner.RemoveCallbacks(this);
		}

		public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
		{
			Debug.Log("GameplayManager: OnHostMigration started. Restarting runner...");

			// Shutdown the current runner
			await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

			// Create a new runner and start with the migration token
			var newRunner = Instantiate(runner);
			newRunner.name = "Migrated Runner";

			await newRunner.StartGame(new StartGameArgs
			{
				HostMigrationToken = hostMigrationToken
			});
		}

		public override void FixedUpdateNetwork()
		{
			if (Runner.IsServer == true)
			{
				// With Client-Server topology only the Server spawn player objects.
				// PlayerManager is a special helper class which iterates over list of active players (NetworkRunner.ActivePlayers) and call spawn/despawn callbacks on demand.
				PlayerManager<Player>.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);
			}
		}

		private void SpawnPlayer(PlayerRef playerRef)
		{
			// Get all spawnpoints in the scene.
			SpawnPoint[] spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);

			// Select random spawnpoint.
			Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform;

			// Spawn the player object with correct input authority.
			NetworkObject player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

			// Set the spawned instance as player object so we can easily get it from other locations using Runner.GetPlayerObject(playerRef).
			// This is optional, but it is a good practice as there is usually 1 main object spawned for each player.
			Runner.SetPlayerObject(playerRef, player);

			// Every player should be always interested to his player object to prevent accidentally getting out of Area of Interest.
			// This is valid only if the Interest Management is enabled in Network Project Config.
			Runner.SetPlayerAlwaysInterested(playerRef, player, true);

			NPCSpawner npcSpawner = FindFirstObjectByType<NPCSpawner>();
			if (npcSpawner != null)
			{
				npcSpawner.SetTarget(playerRef);
			}
		}

		private void DespawnPlayer(PlayerRef playerRef, Player player)
		{
			// We simply despawn the player object. No other cleanup is needed here.
			Runner.Despawn(player.Object);
		}

		// ---- INetworkRunnerCallbacks ----
		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
		public void OnInput(NetworkRunner runner, NetworkInput input) { }
		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
	}
}