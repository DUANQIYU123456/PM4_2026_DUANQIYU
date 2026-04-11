using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Applies a configurable torque to a Rigidbody.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Torque : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Rotation axis and magnitude")]
        [SerializeField] private Vector3 torque = Vector3.up * 10f;

        [Tooltip("Impulse = instant spin, Force = continuous")]
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

        [Tooltip("Use object's local axes instead of world")]
        [SerializeField] private bool useLocalDirection;

        //==================== STATE =====================
        private Rigidbody _rb;

        //==================== OUTPUTS =====================
        public event Action OnSpun;

        [Header("Events")]
        [Tooltip("Fired when torque is applied")]
        [SerializeField] private UnityEvent spunEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        //==================== INPUTS =====================
        /// <summary>Apply the configured torque.</summary>
        [ContextMenu("Spin")]
        public void Spin()
        {
            if (!_rb) return;

            Vector3 finalTorque = useLocalDirection
                ? transform.TransformDirection(torque)
                : torque;

            _rb.AddTorque(finalTorque, forceMode);

            OnSpun?.Invoke();
            spunEvent?.Invoke();
        }

        /// <summary>Apply the configured torque scaled by a multiplier.</summary>
        public void Spin(float multiplier)
        {
            if (!_rb) return;

            Vector3 finalTorque = useLocalDirection
                ? transform.TransformDirection(torque)
                : torque;

            _rb.AddTorque(finalTorque * multiplier, forceMode);

            OnSpun?.Invoke();
            spunEvent?.Invoke();
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 dir = useLocalDirection
                ? transform.TransformDirection(torque.normalized)
                : torque.normalized;

            Gizmos.DrawRay(transform.position, dir * 1.5f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
