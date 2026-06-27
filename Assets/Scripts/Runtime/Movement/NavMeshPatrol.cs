using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Walks a list of waypoints in order, pausing at each. Loops back to the first when done.</summary>
    [RequireComponent(typeof(NavMeshMotor))]
    public class NavMeshPatrol : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Waypoints to walk between, in order")]
        [SerializeField] private Transform[] waypoints;

        [Tooltip("How long to pause at each waypoint (seconds)")]
        [Min(0f)]
        [SerializeField] private float pauseTime = 1f;

        [Tooltip("Start patrolling on enable")]
        [SerializeField] private bool autoPlay = true;

        //==================== STATE =====================
        private NavMeshMotor _motor;
        private int _currentIndex;
        private float _pauseTimer;
        private bool _wasAtWaypoint;

        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isPatrolling;
        [ReadOnly, SerializeField] private int currentIndex;

        public bool IsPatrolling => isPatrolling;
        public int CurrentIndex => currentIndex;
        public bool IsWaiting => _pauseTimer > 0f;

        //==================== OUTPUTS =====================
        public event Action OnPatrolStarted;
        public event Action OnPatrolStopped;
        public event Action<int> OnWaypointReached;

        [Header("Events")]
        [Tooltip("Fired when patrol begins")]
        [SerializeField] private UnityEvent patrolStartedEvent;
        [Tooltip("Fired when patrol stops")]
        [SerializeField] private UnityEvent patrolStoppedEvent;
        [Tooltip("Fired when a waypoint is reached, passes the waypoint index")]
        [SerializeField] private UnityEvent<int> waypointReachedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _motor = GetComponent<NavMeshMotor>();
        }

        private void OnEnable()
        {
            if (autoPlay) StartPatrol();
        }

        private void OnDisable()
        {
            if (isPatrolling) StopPatrol();
        }

        private void Update()
        {
            if (!isPatrolling || waypoints == null || waypoints.Length == 0) return;
            if (!_motor.HasArrived) { _wasAtWaypoint = false; return; }

            if (!_wasAtWaypoint)
            {
                _wasAtWaypoint = true;
                OnWaypointReached?.Invoke(_currentIndex);
                waypointReachedEvent?.Invoke(_currentIndex);
                _pauseTimer = pauseTime;
            }

            _pauseTimer -= Time.deltaTime;
            if (_pauseTimer > 0f) return;

            _currentIndex = (_currentIndex + 1) % waypoints.Length;
            currentIndex = _currentIndex;
            GoToCurrent();
        }

        //==================== INPUTS =====================
        /// <summary>Begin patrolling from the current index.</summary>
        [ContextMenu("Start Patrol")]
        public void StartPatrol()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            isPatrolling = true;
            _wasAtWaypoint = false;
            _pauseTimer = 0f;
            GoToCurrent();
            OnPatrolStarted?.Invoke();
            patrolStartedEvent?.Invoke();
        }

        /// <summary>Stop patrolling and halt movement.</summary>
        [ContextMenu("Stop Patrol")]
        public void StopPatrol()
        {
            isPatrolling = false;
            _motor.Stop();
            OnPatrolStopped?.Invoke();
            patrolStoppedEvent?.Invoke();
        }

        /// <summary>Jump to a specific waypoint index.</summary>
        public void GoToWaypoint(int index)
        {
            if (waypoints == null || waypoints.Length == 0) return;

            _currentIndex = Mathf.Clamp(index, 0, waypoints.Length - 1);
            currentIndex = _currentIndex;
            _wasAtWaypoint = false;
            GoToCurrent();
        }

        //==================== PRIVATE =====================
        private void GoToCurrent()
        {
            var wp = waypoints[_currentIndex];
            if (!wp) return;
            _motor.MoveTo(wp.position);
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (!waypoints[i]) continue;

                Gizmos.DrawWireSphere(waypoints[i].position, 0.4f);

                int next = (i + 1) % waypoints.Length;
                if (waypoints[next])
                    Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }

            if (!Application.isPlaying || !isPatrolling) return;
            if (_currentIndex >= waypoints.Length || !waypoints[_currentIndex]) return;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(waypoints[_currentIndex].position, 0.25f);
        }
    }
}
