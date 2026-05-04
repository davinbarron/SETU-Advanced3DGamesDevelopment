using Fusion;
using Fusion.Addons.FSM;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.SimpleKCC
{
    public class EnemyAI : NetworkBehaviour, IStateMachineOwner
    {
        [SerializeField] private PatrolState _patrol;
        [SerializeField] private ChaseState _chase;
        [SerializeField] private float _chaseRange;

        // Networked so every peer knows which player to chase.
        // Resolved to a Transform each tick via Runner.GetPlayerObject().

        [Networked] private PlayerRef _chaseTarget { get; set; }

        private StateMachine<StateBehaviour> _machine;

        public void SetPlayerTarget(PlayerRef playerRef)
        {
            _chaseTarget = playerRef;
        }

        public override void Spawned()
        {
            Debug.Log($"[EnemyAI] Spawned on P{Runner.LocalPlayer.PlayerId} | HasStateAuthority: {Object.HasStateAuthority}");

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
            if (_chaseTarget == PlayerRef.None) return;

            NetworkObject targetObject = Runner.GetPlayerObject(_chaseTarget);
            if (targetObject == null) return;

            Transform targetTransform = targetObject.transform;
            _chase.SetTarget(targetTransform);

            float dist = Vector3.Distance(transform.position, targetTransform.position);
            _machine.TryToggleState(_chase.StateId, dist < _chaseRange);
        }
    }
}