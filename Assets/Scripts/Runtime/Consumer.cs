// ============================================
// Consumer
// ============================================
// PURPOSE: Eats the nearest target detected by a Sensor when close enough.
//          Gains energy, destroys the target. Fully automatic.
//          Reusable: Fauna eats Flora, Apex eats Fauna — same mechanic.
// USAGE: Attach to entity root. Assign a Sensor (the food sensor) and
//        own Lifecycle. Set consume radius and energy gain.
//        The controller moves toward food, Consumer eats it on arrival.
// ============================================

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Eats the nearest target detected by a Sensor when close enough.</summary>
    public class Consumer : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("References")]
        [Tooltip("Lifecycle that receives energy when consuming a target.")]
        [SerializeField] private Lifecycle lifecycle;
        [Tooltip("Sensor used to detect nearby food targets.")]
        [SerializeField] private Sensor sensor;

        [Header("Settings")]
        [Tooltip("Maximum distance at which a detected target can be consumed.")]
        [Min(0f)]
        [SerializeField] private float consumeRadius = 1.5f;
        [Tooltip("Energy added to Lifecycle when a target is consumed.")]
        [Min(0f)]
        [SerializeField] private float energyGain = 30f;

        //==================== OUTPUTS =====================
        public event Action<GameObject> OnConsumed;

        [Header("Events")]
        [Tooltip("Fired when a target is consumed")]
        [SerializeField] private UnityEvent<GameObject> consumedEvent;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!lifecycle.IsAlive) return;
            if (!sensor.TryGetNearest(out var signal)) return;
            if (signal.Distance > consumeRadius) return;

            Consume(signal.Object);
        }

        //==================== PRIVATE =====================
        private void Consume(GameObject target)
        {
            lifecycle.AddEnergy(energyGain);
            OnConsumed?.Invoke(target);
            consumedEvent?.Invoke(target);
            Destroy(target);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, consumeRadius);
        }
    }
}
