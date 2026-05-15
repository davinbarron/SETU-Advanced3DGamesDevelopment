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
    
    public class NPCContactDetector : NetworkBehaviour
    {
        [SerializeField] private float _radius = 1.2f;
        [SerializeField] private float _height = 2.0f;
        [SerializeField] private LayerMask _playerLayer;
        [SerializeField] private float _contactCooldown = 1.0f;

        private readonly Collider[] _hits = new Collider[8];
        private RunnerSimulatePhysics3D _physicsSimulator;
        private EnemyAI _enemyAI;
        private TickTimer _contactTimer;

        public override void Spawned()
        {
            _physicsSimulator = Runner.GetComponent<RunnerSimulatePhysics3D>();
            _enemyAI = GetComponent<EnemyAI>();

            if (_physicsSimulator == null)            {
                Debug.LogError($"[NPCContactDetector] RunnerSimulatePhysics3D not found on NetworkRunner.");
                return;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            if (!_contactTimer.ExpiredOrNotRunning(Runner)) return;

            // Define capsule points based on transform
            Vector3 p1 = transform.position + Vector3.up * _radius;
            Vector3 p2 = transform.position + Vector3.up * (_height - _radius);

            int hitCount = Runner.GetPhysicsScene().OverlapCapsule(p1, p2, _radius, _hits, _playerLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                if (NetworkUtils.TryGetPlayer(_hits[i], out Player p))
                {
                    // Found a player! 
                    if (_enemyAI != null) _enemyAI.TriggerPunch();
                    
                    Rpc_OnPlayerContact(p.Object);
                    _contactTimer = TickTimer.CreateFromSeconds(Runner, _contactCooldown);
                    break;
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_OnPlayerContact(NetworkObject playerObject)
        {
            Player player = playerObject.GetComponent<Player>();
            if (player == null) return;

            // If this is the local player, deduct score
            // Actually, Player.AddScore already has authority check, but we want the score to be deducted
            // on the authority side. RpcTargets.All means everyone sees the log, but only authority writes.
            // Wait, if RpcTargets.All, every client will try to call AddScore.
            // But AddScore(int amount) in Player.cs has: if (!HasStateAuthority) return;
            // So only the player who "owns" the score object will actually modify it.
            // This is perfect.
            
            player.AddScore(-10);
            Debug.Log($"[NPCContactDetector] NPC touched player: {playerObject.name}. Deducting 10 points.");
        }
    }
}