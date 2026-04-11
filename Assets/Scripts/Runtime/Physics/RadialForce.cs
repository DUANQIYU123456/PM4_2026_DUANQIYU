using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Applies radial force to all Rigidbodies within a radius.</summary>
    public class RadialForce : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Force strength")]
        [SerializeField] private float force = 500f;

        [Tooltip("Effect radius")]
        [Min(0f)]
        [SerializeField] private float radius = 5f;

        [Tooltip("Extra upward lift")]
        [SerializeField] private float upwardsModifier = 1f;

        [Tooltip("Impulse = instant, Force = continuous")]
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

        [Tooltip("Which layers are affected")]
        [SerializeField] private LayerMask affectedLayers = ~0;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private int lastHitCount;

        public int LastHitCount => lastHitCount;

        //==================== OUTPUTS =====================
        public event Action<int> OnFired;

        [Header("Events")]
        [Tooltip("Fired after radial force is applied, passes the number of bodies hit")]
        [SerializeField] private UnityEvent<int> firedEvent;

        //==================== INPUTS =====================
        /// <summary>Fire the radial force, pushing all Rigidbodies in range.</summary>
        [ContextMenu("Fire")]
        public void Fire()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, affectedLayers);

            int hitCount = 0;
            foreach (Collider col in colliders)
            {
                if (!col.attachedRigidbody) continue;

                col.attachedRigidbody.AddExplosionForce(
                    force, transform.position, radius, upwardsModifier, forceMode);
                hitCount++;
            }

            lastHitCount = hitCount;
            OnFired?.Invoke(hitCount);
            firedEvent?.Invoke(hitCount);
        }

        //==================== PRIVATE =====================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
