using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Smoothly moves toward a target transform each frame.</summary>
    public class Follow : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("What to follow")]
        [SerializeField] private Transform target;

        [Tooltip("Offset from the target position")]
        [SerializeField] private Vector3 offset;

        [Tooltip("Follow speed (higher = snappier)")]
        [Min(0f)]
        [SerializeField] private float smoothSpeed = 5f;

        [Tooltip("Start following on enable")]
        [SerializeField] private bool autoStart = true;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isFollowing;

        public bool IsFollowing => isFollowing;
        public float Distance => target ? Vector3.Distance(transform.position, target.position) : 0f;

        //==================== OUTPUTS =====================
        public event Action OnStarted;
        public event Action OnStopped;

        [Header("Events")]
        [Tooltip("Fired when following begins")]
        [SerializeField] private UnityEvent startedEvent;
        [Tooltip("Fired when following stops")]
        [SerializeField] private UnityEvent stoppedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (autoStart) StartFollow();
        }

        private void OnDisable()
        {
            if (!isFollowing) return;
            StopFollow();
        }

        private void Update()
        {
            if (!isFollowing || !target) return;

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }

        //==================== INPUTS =====================
        /// <summary>Set the follow target at runtime.</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>Begin following the target.</summary>
        [ContextMenu("Start")]
        public void StartFollow()
        {
            if (!target) return;

            isFollowing = true;
            OnStarted?.Invoke();
            startedEvent?.Invoke();
        }

        /// <summary>Stop following.</summary>
        [ContextMenu("Stop")]
        public void StopFollow()
        {
            isFollowing = false;
            OnStopped?.Invoke();
            stoppedEvent?.Invoke();
        }

        /// <summary>Teleport to the target position immediately.</summary>
        public void Snap()
        {
            if (!target) return;
            transform.position = target.position + offset;
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            if (!target) return;

            Gizmos.color = isFollowing ? Color.green : Color.cyan;
            Gizmos.DrawLine(transform.position, target.position + offset);
            Gizmos.DrawSphere(target.position + offset, 0.1f);
        }
    }
}
