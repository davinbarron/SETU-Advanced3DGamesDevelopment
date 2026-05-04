using Fusion.Addons.FSM;
using UnityEngine;

namespace Fusion.Addons.SimpleKCC
{
    public class ChaseState : NPCStateBehaviour
    {
        private Transform _target;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        protected override void OnEnterState()
        {
            if (!Object.HasStateAuthority) return;
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
            Animator?.SetFloat(SpeedHash,
                Mathf.Lerp(Animator.GetFloat(SpeedHash), 1f, Time.deltaTime * 8f));
        }
    }
}