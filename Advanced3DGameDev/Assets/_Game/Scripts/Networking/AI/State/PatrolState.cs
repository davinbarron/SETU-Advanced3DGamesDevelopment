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

            // Ensure waypoints are collected. Sometimes OnInitialize fires too early.
            if (_wayPoints == null || _wayPoints.Length == 0)
            {
                CollectWaypoints();
            }

            Agent.speed = 1.5f;
            Agent.acceleration = 4.0f;

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
            if (_wayPoints == null || _wayPoints.Length == 0) return;

            // Check if we reached the destination
            if (!Agent.pathPending && Agent.remainingDistance < Agent.stoppingDistance + 0.5f)
            {
                _waypointIndex = (_waypointIndex + 1) % _wayPoints.Length;
                Agent.SetDestination(_wayPoints[_waypointIndex].position);
                Debug.Log($"[PatrolState] Destination reached. Moving to next waypoint: {_waypointIndex}");
            }
        }
    }
}