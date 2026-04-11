using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Thin wrapper around NavMeshAgent — provides clean movement verbs.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshMotor : MonoBehaviour
    {
        //==================== STATE =====================
        private NavMeshAgent _agent;
        private bool _wasMoving;

        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isMoving;

        public bool IsMoving => isMoving;

        public bool HasArrived => _agent.isOnNavMesh
            && !_agent.pathPending
            && _agent.remainingDistance <= _agent.stoppingDistance;

        public float Speed => _agent.speed;

        //==================== OUTPUTS =====================
        public event Action OnStartedMoving;
        public event Action OnArrived;

        [Header("Events")]
        [Tooltip("Fired when the agent starts moving")]
        [SerializeField] private UnityEvent startedMovingEvent;
        [Tooltip("Fired when the agent arrives at its destination")]
        [SerializeField] private UnityEvent arrivedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            bool moving = _agent.isOnNavMesh
                && !_agent.pathPending
                && _agent.remainingDistance > _agent.stoppingDistance;

            if (moving && !_wasMoving)
            {
                OnStartedMoving?.Invoke();
                startedMovingEvent?.Invoke();
            }
            else if (!moving && _wasMoving)
            {
                OnArrived?.Invoke();
                arrivedEvent?.Invoke();
            }

            _wasMoving = moving;
            isMoving = moving;
        }

        //==================== INPUTS =====================
        /// <summary>Set a NavMesh destination.</summary>
        public void MoveTo(Vector3 position)
        {
            if (!_agent.isOnNavMesh) return;

            _agent.isStopped = false;
            _agent.SetDestination(position);
        }

        /// <summary>Stop movement and clear the current path.</summary>
        public void Stop()
        {
            if (!_agent.isOnNavMesh) return;

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !_agent || !_agent.hasPath) return;

            Gizmos.color = Color.yellow;
            var corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);

            Gizmos.DrawSphere(_agent.destination, 0.2f);
        }
    }
}
