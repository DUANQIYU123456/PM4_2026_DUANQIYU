using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Flees from a position by picking a NavMesh point in the opposite direction.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MoveAway : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Max flee distance")]
        [Min(0f)]
        [SerializeField] private float range = 10f;

        [Tooltip("Min flee distance")]
        [Min(0f)]
        [SerializeField] private float minDistance = 2f;

        [Tooltip("Random spread around the flee direction (degrees)")]
        [Range(0f, 90f)]
        [SerializeField] private float randomAngle = 35f;

        //==================== STATE =====================
        private NavMeshAgent _agent;

        public bool IsFleeing => _agent.isOnNavMesh
            && !_agent.pathPending
            && _agent.remainingDistance > _agent.stoppingDistance;

        //==================== OUTPUTS =====================
        public event Action<Vector3> OnFled;

        [Header("Events")]
        [Tooltip("Fired when a flee destination is set")]
        [SerializeField] private UnityEvent<Vector3> fledEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        //==================== INPUTS =====================
        /// <summary>Flee from a world position.</summary>
        public void FleeFrom(Vector3 threat)
        {
            if (!_agent.isOnNavMesh) return;

            Vector3 away = transform.position - threat;
            if (away.sqrMagnitude < 0.0001f)
                away = UnityEngine.Random.onUnitSphere;

            away.y = 0f;
            away.Normalize();

            float yaw = UnityEngine.Random.Range(-randomAngle, randomAngle);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * away;
            float dist = UnityEngine.Random.Range(minDistance, range);
            Vector3 desired = transform.position + dir * dist;

            if (TrySetDestination(desired)) return;

            // Fallback: try random directions
            for (int i = 0; i < 4; i++)
            {
                float angle = UnityEngine.Random.Range(-180f, 180f);
                Vector3 fallbackDir = Quaternion.Euler(0f, angle, 0f) * away;
                Vector3 fallbackPos = transform.position + fallbackDir * UnityEngine.Random.Range(minDistance, range);

                if (TrySetDestination(fallbackPos)) return;
            }
        }

        //==================== PRIVATE =====================
        private bool TrySetDestination(Vector3 desired)
        {
            if (!NavMesh.SamplePosition(desired, out var hit, range, NavMesh.AllAreas)) return false;

            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
            OnFled?.Invoke(hit.position);
            fledEvent?.Invoke(hit.position);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
