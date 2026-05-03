using Fusion.Addons.FSM;
using UnityEngine;
using UnityEngine.AI;

namespace Fusion.Addons.SimpleKCC
{
    public class NPCStateBehaviour : StateBehaviour
    {
        protected NavMeshAgent Agent;
        protected Animator Animator;
        protected int SpeedHash;

        private void Awake()
        {
            Agent = GetComponentInParent<NavMeshAgent>();
            Animator = GetComponentInParent<Animator>();
            SpeedHash = Animator.StringToHash("Speed");
        }
    }
}