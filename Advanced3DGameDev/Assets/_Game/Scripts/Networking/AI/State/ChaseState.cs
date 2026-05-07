using Fusion.Addons.FSM;
using UnityEngine;

namespace Fusion.Addons.SimpleKCC
{
    public class ChaseState : NPCStateBehaviour
    {
        [SerializeField] private Transform _target;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        protected override void OnEnterState()
        {
            if (!Object.HasStateAuthority) return;

            Agent.speed = 4.0f;
            Agent.acceleration = 8.0f;

            if (AI != null)
            {
                AI.NetworkedRunning = true;
            }

            if (_target != null)
                Agent.SetDestination(_target.position);
        }

        protected override void OnFixedUpdate()
        {
            if (_target != null)
                Agent.SetDestination(_target.position);
        }
    }
}