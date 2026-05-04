using UnityEngine;

namespace Fusion.Addons.SimpleKCC
{
    public class PatrolState : NPCStateBehaviour
    {
        [SerializeField] private Transform[] _wayPoints;
        private int _waypointIndex;

        protected override void OnInitialize()
        {
            // Collect all scene waypoints by tag so the prefab needs no scene references.
            GameObject[] tagged = GameObject.FindGameObjectsWithTag("npc_wp");
            _wayPoints = new Transform[tagged.Length];
            for (int i = 0; i < tagged.Length; i++)
            {
                _wayPoints[i] = tagged[i].transform;
            }
        }

        protected override void OnEnterState()
        {
            if (!Object.HasStateAuthority) return; // guard — authority only
            if (_wayPoints == null || _wayPoints.Length == 0) return;
            Agent.SetDestination(_wayPoints[_waypointIndex].position);
        }

        protected override void OnFixedUpdate()  // authority only
        {
            if (_wayPoints == null || _wayPoints.Length == 0) return;
            if (Agent.remainingDistance < Agent.stoppingDistance + 0.1f)
            {
                _waypointIndex = (_waypointIndex + 1) % _wayPoints.Length;
                Agent.SetDestination(_wayPoints[_waypointIndex].position);
            }
        }

        protected override void OnRender()  // all peers - safe for Animator
        {
            Animator?.SetFloat(SpeedHash, 
                Mathf.Lerp(Animator.GetFloat(SpeedHash), Agent.velocity.magnitude, Time.deltaTime * 8f));
        }
    }
}