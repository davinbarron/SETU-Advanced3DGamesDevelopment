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

            if (_wayPoints == null || _wayPoints.Length == 0)
            {
                CollectWaypoints();
            }

            Agent.speed = 12.0f;
            Agent.acceleration = 28.0f;

            if (AI != null)
            {
                AI.NetworkedRunning = false;
            }

            if (_wayPoints == null || _wayPoints.Length == 0)
            {
                Debug.LogWarning("[PatrolState] No waypoints found with tag 'npc_wp'. NPC will be stuck.");
                return;
            }

            Debug.Log($"[PatrolState] Moving to waypoint {_waypointIndex} at {Agent.speed} speed.");
            Agent.SetDestination(_wayPoints[_waypointIndex].position);
        }

        private void CollectWaypoints()
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag("npc_wp");
            _wayPoints = new Transform[tagged.Length];
            for (int i = 0; i < tagged.Length; i++)
            {
                _wayPoints[i] = tagged[i].transform;
            }
        }

        protected override void OnFixedUpdate()  // authority only
        {
            if (!Object.HasStateAuthority) return;
            if (_wayPoints == null || _wayPoints.Length == 0) return;

            if (!Agent.pathPending && Agent.remainingDistance < Agent.stoppingDistance + 0.5f)
            {
                _waypointIndex = (_waypointIndex + 1) % _wayPoints.Length;
                Agent.SetDestination(_wayPoints[_waypointIndex].position);
                Debug.Log($"[PatrolState] Destination reached. Moving to next waypoint: {_waypointIndex}");
            }
        }
    }
}