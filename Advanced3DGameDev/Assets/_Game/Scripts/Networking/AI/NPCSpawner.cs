using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Fusion.Sockets;
using UnityEngine;

namespace Example
{
    /// <summary>
    /// Spawns the NPC NetworkObject once per session and keeps its
    /// EnemyAI target updated when a player object becomes available.
    /// The peer with the lowest PlayerRef index performs the spawn -
    /// this is deterministic and consistent across all peers in Shared Mode.
    /// </summary>
    public class NPCSpawner : NetworkBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkObject _npcPrefab;
        [SerializeField] private Transform _spawnPoint;

        [Networked] private EnemyAI _spawnedEnemy { get; set; }
        private PlayerRef _pendingTarget = PlayerRef.None;

        // Networked flag - written by the first peer to hold StateAuthority
        // over this scene object. Every other peer sees it as true when their
        // Spawned() fires, preventing a second spawn.
        [Networked] private NetworkBool _npcSpawned { get; set; }

        private float _targetUpdateTimer = 0f;

        public override void Spawned()
        {
            Debug.Log($"[NPCSpawner] Spawned on P{Runner.LocalPlayer.PlayerId} | HasStateAuthority: {Object.HasStateAuthority}");

            Runner.AddCallbacks(this);

            // If we are authority and it hasn't been spawned yet, spawn it.
            if (Object.HasStateAuthority && !_npcSpawned)
            {
                _npcSpawned = true;
                SpawnNPC();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            runner.RemoveCallbacks(this);
        }

        private void SpawnNPC()
        {
            Vector3 position = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

            Debug.Log($"[NPCSpawner] Spawning NPC at {position}");
            NetworkObject npcObj = Runner.Spawn(_npcPrefab, position, rotation);
            _spawnedEnemy = npcObj.GetComponent<EnemyAI>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            HandleNpcRecovery();
            HandleTargeting();
        }

        private void HandleNpcRecovery()
        {
            if (_npcSpawned && _spawnedEnemy == null)
            {
                // Double check if it exists but hasn't registered yet
                var enemies = new List<EnemyAI>();
                Runner.GetAllBehaviours(enemies);
                
                if (enemies.Count > 0) _spawnedEnemy = enemies[0];
                else
                {
                    Debug.LogWarning("[NPCSpawner] NPC missing but flag is set. Re-spawning...");
                    SpawnNPC();
                }
            }
        }

        private void HandleTargeting()
        {
            if (_spawnedEnemy == null) return;

            _targetUpdateTimer += Runner.DeltaTime;
            if (_targetUpdateTimer >= 0.25f)
            {
                _targetUpdateTimer = 0f;
                UpdateClosestTarget();
            }
        }

        private void UpdateClosestTarget()
        {
            Player closestPlayer = FindClosestPlayer();

            if (closestPlayer != null)
            {
                PlayerRef newTarget = closestPlayer.Object.InputAuthority;
                if (_spawnedEnemy.ChaseTarget != newTarget)
                {
                    Debug.Log($"[NPCSpawner] Switching target to closest player: {newTarget}");
                    _spawnedEnemy.SetPlayerTarget(newTarget);
                }
            }
            else if (_spawnedEnemy.ChaseTarget != PlayerRef.None)
            {
                _spawnedEnemy.SetPlayerTarget(PlayerRef.None);
            }
        }

        private Player FindClosestPlayer()
        {
            var players = NetworkUtils.GetAllPlayers(Runner);
            Player closestPlayer = null;
            float minDistance = float.MaxValue;

            foreach (var p in players)
            {
                if (p == null || p.Object == null) continue;
                
                float dist = Vector3.Distance(_spawnedEnemy.transform.position, p.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPlayer = p;
                }
            }
            return closestPlayer;
        }


        /// <summary>
        /// Called by EnemyAI from its own Spawned() once all components are ready.
        /// </summary>
        public void Register(EnemyAI enemy)
        {
            _spawnedEnemy = enemy;

            if (_pendingTarget != PlayerRef.None)
            {
                _spawnedEnemy.SetPlayerTarget(_pendingTarget);
                _pendingTarget = PlayerRef.None;
            }
        }

        /// <summary>
        /// Call this from GameplayManager after a player is spawned so the
        /// NPC knows who to chase.
        /// </summary>
        public void SetTarget(PlayerRef playerRef)
        {
            if (_spawnedEnemy != null)
                _spawnedEnemy.SetPlayerTarget(playerRef);
            else
                _pendingTarget = playerRef;
        }

        /// <summary>
        /// Picks a new target from active players if possible.
        /// </summary>
        public void PickNewTarget()
        {
            UpdateClosestTarget();
        }

        // ---- INetworkRunnerCallbacks ----

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // Request authority for the spawner itself if the authority left
            Example.NetworkUtils.RequestAuthorityIfOwnerLeft(Object, player, "NPCSpawner");

            // Request authority for the spawned NPC as well (SRP)
            if (_spawnedEnemy != null)
            {
                Example.NetworkUtils.RequestAuthorityIfOwnerLeft(_spawnedEnemy.Object, player, "NPCSpawner (Child NPC)");
            }
        }

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
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    }
}