using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Continuously re-paths toward a moving Transform target.</summary>
    [RequireComponent(typeof(NavMeshMotor))]
    public class NavMeshChase : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("The Transform to chase")]
        [SerializeField] private Transform target;

        [Tooltip("How often to refresh the path (seconds)")]
        [Min(0f)]
        [SerializeField] private float updateRate = 0.2f;

        [Tooltip("Start chasing on enable if a target is set")]
        [SerializeField] private bool autoPlay = true;

        //==================== STATE =====================
        private NavMeshMotor _motor;
        private float _nextUpdateTime;

        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isChasing;

        public bool IsChasing => isChasing;
        public Transform Target => target;

        //==================== OUTPUTS =====================
        public event Action OnChaseStarted;
        public event Action OnChaseStopped;

        [Header("Events")]
        [Tooltip("Fired when chase begins")]
        [SerializeField] private UnityEvent chaseStartedEvent;
        [Tooltip("Fired when chase stops")]
        [SerializeField] private UnityEvent chaseStoppedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _motor = GetComponent<NavMeshMotor>();
        }

        private void OnEnable()
        {
            if (autoPlay && target) StartChase();
        }

        private void OnDisable()
        {
            if (isChasing) StopChase();
        }

        private void Update()
        {
            if (!isChasing || !target) return;
            if (Time.time < _nextUpdateTime) return;

            _motor.MoveTo(target.position);
            _nextUpdateTime = Time.time + updateRate;
        }

        //==================== INPUTS =====================
        /// <summary>Begin chasing the current target.</summary>
        [ContextMenu("Start Chase")]
        public void StartChase()
        {
            if (!target) return;

            isChasing = true;
            _nextUpdateTime = 0f;
            OnChaseStarted?.Invoke();
            chaseStartedEvent?.Invoke();
        }

        /// <summary>Begin chasing a specific target.</summary>
        public void StartChase(Transform newTarget)
        {
            target = newTarget;
            StartChase();
        }

        /// <summary>Stop chasing and halt movement.</summary>
        [ContextMenu("Stop Chase")]
        public void StopChase()
        {
            isChasing = false;
            _motor.Stop();
            OnChaseStopped?.Invoke();
            chaseStoppedEvent?.Invoke();
        }

        /// <summary>Replace the chase target at runtime.</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            if (!target) return;

            Gizmos.color = isChasing ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position, 0.3f);
        }
    }
}
