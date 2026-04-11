using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Applies a configurable force to a Rigidbody.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Force : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Force direction and magnitude")]
        [SerializeField] private Vector3 force = Vector3.up * 10f;

        [Tooltip("Impulse = instant burst, Force = continuous push")]
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

        [Tooltip("Use object's local direction instead of world")]
        [SerializeField] private bool useLocalDirection;

        //==================== STATE =====================
        private Rigidbody _rb;

        //==================== OUTPUTS =====================
        public event Action OnPushed;

        [Header("Events")]
        [Tooltip("Fired when a force push is applied")]
        [SerializeField] private UnityEvent pushedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        //==================== INPUTS =====================
        /// <summary>Apply the configured force.</summary>
        [ContextMenu("Push")]
        public void Push()
        {
            if (!_rb) return;

            Vector3 finalForce = useLocalDirection
                ? transform.TransformDirection(force)
                : force;

            _rb.AddForce(finalForce, forceMode);

            OnPushed?.Invoke();
            pushedEvent?.Invoke();
        }

        /// <summary>Apply the configured force scaled by a multiplier.</summary>
        public void Push(float multiplier)
        {
            if (!_rb) return;

            Vector3 finalForce = useLocalDirection
                ? transform.TransformDirection(force)
                : force;

            _rb.AddForce(finalForce * multiplier, forceMode);

            OnPushed?.Invoke();
            pushedEvent?.Invoke();
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Vector3 start = transform.position;
            Vector3 dir = useLocalDirection
                ? transform.TransformDirection(force.normalized)
                : force.normalized;

            Gizmos.DrawRay(start, dir * 2f);
        }
    }
}
