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

            Agent.speed = 6.0f;
            Agent.acceleration = 8.0f;

            if (_target != null)
                Agent.SetDestination(_target.position);
        }

        protected override void OnFixedUpdate()
        {
            if (_target != null)
                Agent.SetDestination(_target.position);
        }

        protected override void OnRender()
        {
            float currentVelocity = Agent.velocity.magnitude;
            Animator?.SetFloat(SpeedHash, Mathf.Lerp(Animator.GetFloat(SpeedHash), currentVelocity, Time.deltaTime * 8f));
        }
    }
}