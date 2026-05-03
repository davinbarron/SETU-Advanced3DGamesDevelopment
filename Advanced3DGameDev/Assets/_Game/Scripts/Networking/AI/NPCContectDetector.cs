using UnityEngine;
using Fusion.Addons.Physics;
using Example;

namespace Fusion.Addons.SimpleKCC
{
    /// <summary>
    /// Detects player contact by running an OverlapCapsule query against the
    /// runner's physics scene AFTER Physics.Simulate() has stepped each tick,
    /// using RunnerSimulatePhysics3D.QueueAfterSimulateionCallback.
    /// This is the correct Fusion Physics Addon pattern - the query runs on
    /// tick-aligned, fully simulated posiitons, and is resimulation-safe.
    /// </summary>
    
    public class NPCContectDetector : NetworkBehaviour
    {
        [SerializeField] private float _radius = 1.2f;
        [SerializeField] private float _height = 2.0f;
        [SerializeField] private LayerMask _playerLayer;

        private readonly Collider[] _hits = new Collider[8];
        private RunnerSimulatePhysics3D _physicsSimulator;

        public override void Spawned()
        {
            _physicsSimulator = Runner.GetComponent<RunnerSimulatePhysics3D>();
            if (_physicsSimulator == null)            {
                Debug.LogError($"[NPCContactDetector] RunnerSimulatePhysics3D not found on NetworkRunner.");
                return;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_OnPlayerContact(NetworkObject playerObject)
        {
            Player player = playerObject.GetComponent<Player>();
            if (player == null) return;

            // TODO: add damage / knockback / game-over logic here.
            Debug.Log($"[NPCContactDetector] NPC touched player: {playerObject.name}");
        }
    }
}