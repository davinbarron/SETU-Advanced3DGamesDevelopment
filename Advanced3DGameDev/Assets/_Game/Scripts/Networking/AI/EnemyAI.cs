using Example;
using Fusion;
using Fusion.Addons.FSM;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Fusion.Addons.SimpleKCC
{
    public class EnemyAI : NetworkBehaviour, IStateMachineOwner
    {
        [SerializeField] private PatrolState _patrol;
        [SerializeField] private ChaseState _chase;
        [SerializeField] private float _chaseRange = 10.0f;

        [Networked] private PlayerRef _chaseTarget { get; set; }

        [Networked] public float NetworkedSpeed { get; set; }
        [Networked] public bool NetworkedRunning { get; set; }

        private StateMachine<StateBehaviour> _machine;
        private NavMeshAgent _agent;
        private Animator _animator;

        private int _speedHash;
        private int _runningHash;

        public void SetPlayerTarget(PlayerRef playerRef)
        {
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

            InitializeAgentOnAuthority();

            var spawners = Runner.SimulationUnityScene.GetComponents<Example.NPCSpawner>(false);
            if (spawners.Length > 0)
                spawners[0].Register(this);
            else
                Debug.LogWarning("[EnemyAI] No NPCSpawner found in the scene.");
        }

        public void CollectStateMachines(List<IStateMachine> stateMachines)
        {
            _machine = new StateMachine<StateBehaviour>("NPC AI", _patrol, _chase);
            stateMachines.Add(_machine);
        }

        public override void FixedUpdateNetwork()
        {
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

            NetworkObject targetObject = Runner.GetPlayerObject(_chaseTarget);
            bool hasValidTarget = targetObject != null;

            if (hasValidTarget)
            {
                _chase.SetTarget(targetObject.transform);
            }

            float dist = hasValidTarget ? Vector3.Distance(transform.position, targetObject.transform.position) : float.MaxValue;

            if (_machine != null)
            {
                _machine.TryToggleState(_chase.StateId, hasValidTarget && dist < _chaseRange);
            }
        }

        public override void Render()
        {
            if (_animator != null)
            {
                // Smoothly lerp speed for visual stability on all clients.
                float currentSpeed = _animator.GetFloat(_speedHash);
                _animator.SetFloat(_speedHash, Mathf.Lerp(currentSpeed, NetworkedSpeed, Time.deltaTime * 5f));
                _animator.SetBool(_runningHash, NetworkedRunning);
            }
        }
    }
}