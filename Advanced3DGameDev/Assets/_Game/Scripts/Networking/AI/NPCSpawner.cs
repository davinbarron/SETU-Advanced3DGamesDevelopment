using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

namespace Example
{
    /// <summary>
    /// Spawns the NPC NetworkObject once per session and keeps its
    /// EnemyAI target updated when a player object becomes available.
    /// The peer with the lowest PlayerRef index performs the spawn -
    /// this is determinsistic and consistent across all peers in Shared Mode.
    /// </summary>
    public class NPCSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _npcPrefab;
        [SerializeField] private Transform _spawnPoint;

        private EnemyAI _spawnedEnemy;
        private PlayerRef _pendingTarget = PlayerRef.None;

        // Networked flag - written by the first peer to hold StateAuthority
        // over this scene object. Every other peer sees it as true when their
        // Spawned() fires, preventing a second spawn.
        [Networked] private NetworkBool _npcSpawned { get; set; }

        public override void Spawned()
        {
            Debug.Log($"[NPCSpawner] Spawned on P{Runner.LocalPlayer.PlayerId} | HasStateAuthority: {Object.HasStateAuthority}");

            if (!Object.HasStateAuthority || !Runner.IsSharedModeMasterClient) return;
            if (_npcSpawned) return;

            _npcSpawned = true;

            Vector3    position = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

            Debug.Log($"[NPCSpawner] Spawning NPC at {position}");
            Runner.Spawn(_npcPrefab, position, rotation);
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
    }
}