using Example;
using Fusion;
using Fusion.Addons.FSM;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Fusion.Addons.SimpleKCC
{
    public class EnemyAI : NetworkBehaviour, IStateMachineOwner, INetworkRunnerCallbacks
    {
        [SerializeField] private PatrolState _patrol;
        [SerializeField] private ChaseState _chase;
        [SerializeField] private float _chaseRange = 10.0f;

        [Networked] private PlayerRef _chaseTarget { get; set; }
        public PlayerRef ChaseTarget => _chaseTarget;

        [Networked] public float NetworkedSpeed { get; set; }
        [Networked] public bool NetworkedRunning { get; set; }

        private StateMachine<StateBehaviour> _machine;
        private NavMeshAgent _agent;
        private Animator _animator;

        private int _speedHash;
        private int _runningHash;

        public void SetPlayerTarget(PlayerRef playerRef)
        {
            if (!HasStateAuthority) return;
            _chaseTarget = playerRef;
        }

        private void InitializeAgentOnAuthority()
        {
            if (!Object.HasStateAuthority) return;
            if (_agent == null) return;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            _agent.updatePosition = true;
        }

        public override void Spawned()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _speedHash = Animator.StringToHash("Speed");
            _runningHash = Animator.StringToHash("Running");

            Debug.Log($"[EnemyAI] Spawned on P{Runner.LocalPlayer.PlayerId} | HasStateAuthority: {Object.HasStateAuthority}");

            Runner.AddCallbacks(this);
            InitializeAgentOnAuthority();

            var spawners = Runner.SimulationUnityScene.GetComponents<Example.NPCSpawner>(false);
            if (spawners.Length > 0)
                spawners[0].Register(this);
            else
                Debug.LogWarning("[EnemyAI] No NPCSpawner found in the scene.");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            runner.RemoveCallbacks(this);
        }

        public void CollectStateMachines(List<IStateMachine> stateMachines)
        {
            _machine = new StateMachine<StateBehaviour>("NPC AI", _patrol, _chase);
            stateMachines.Add(_machine);
        }

        private bool _hadAuthority;
        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority && !_hadAuthority)
            {
                InitializeAgentOnAuthority();
            }
            _hadAuthority = Object.HasStateAuthority;

            if (!Object.HasStateAuthority) return;

            if (_agent != null)
            {
                float desiredSpeed = _agent.velocity.magnitude;
                NetworkedSpeed = Mathf.Min(desiredSpeed, 6.0f);

                if (_animator != null)
                {
                    _animator.SetFloat(_speedHash, NetworkedSpeed);
                    _animator.SetBool(_runningHash, NetworkedRunning);
                }
            }

            if (_machine != null)
            {
                NetworkObject targetObject = Example.NetworkUtils.GetPlayerObject(Runner, _chaseTarget);
                bool hasValidTarget = targetObject != null;

                if (hasValidTarget)
                {
                    _chase.SetTarget(targetObject.transform);
                }

                float dist = hasValidTarget ? Vector3.Distance(transform.position, targetObject.transform.position) : float.MaxValue;
                _machine.TryToggleState(_chase.StateId, hasValidTarget && dist < _chaseRange);
            }
        }


        public override void Render()
        {
            if (_animator != null)
            {
                float currentSpeed = _animator.GetFloat(_speedHash);
                _animator.SetFloat(_speedHash, Mathf.Lerp(currentSpeed, NetworkedSpeed, Time.deltaTime * 5f));
                _animator.SetBool(_runningHash, NetworkedRunning);
            }
        }

        // ---- INetworkRunnerCallbacks ----

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // Use shared logic for self-authority takeover
            Example.NetworkUtils.RequestAuthorityIfOwnerLeft(Object, player, "EnemyAI");
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