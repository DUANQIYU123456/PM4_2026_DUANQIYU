using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Continuously rotates to face a target transform.</summary>
    public class LookAt : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("What to look at")]
        [SerializeField] private Transform target;

        [Tooltip("Rotation speed (higher = snappier)")]
        [Min(0f)]
        [SerializeField] private float smoothSpeed = 5f;

        [Tooltip("Lock rotation on the Y axis only (ignore vertical angle)")]
        [SerializeField] private bool horizontalOnly = true;

        [Tooltip("Start looking on enable")]
        [SerializeField] private bool autoStart = true;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isLooking;

        public bool IsLooking => isLooking;

        //==================== OUTPUTS =====================
        public event Action OnStarted;
        public event Action OnStopped;

        [Header("Events")]
        [Tooltip("Fired when looking begins")]
        [SerializeField] private UnityEvent startedEvent;
        [Tooltip("Fired when looking stops")]
        [SerializeField] private UnityEvent stoppedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoStart) StartLook();
        }

        private void OnDisable()
        {
            if (!isLooking) return;
            StopLook();
        }

        private void Update()
        {
            if (!isLooking || !target) return;

            Vector3 direction = target.position - transform.position;
            if (horizontalOnly) direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Quaternion desired = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, smoothSpeed * Time.deltaTime);
        }

        //==================== INPUTS =====================
        /// <summary>Set the look target at runtime.</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>Begin looking at the target.</summary>
        [ContextMenu("Start")]
        public void StartLook()
        {
            if (!target) return;

            isLooking = true;
            OnStarted?.Invoke();
            startedEvent?.Invoke();
        }

        /// <summary>Stop looking.</summary>
        [ContextMenu("Stop")]
        public void StopLook()
        {
            isLooking = false;
            OnStopped?.Invoke();
            stoppedEvent?.Invoke();
        }

        /// <summary>Snap rotation to face the target immediately.</summary>
        public void Snap()
        {
            if (!target) return;

            Vector3 direction = target.position - transform.position;
            if (horizontalOnly) direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            if (!target) return;

            Gizmos.color = isLooking ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawSphere(target.position, 0.1f);
        }
    }
}
