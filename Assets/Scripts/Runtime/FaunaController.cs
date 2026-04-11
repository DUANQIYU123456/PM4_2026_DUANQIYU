using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Fauna behavior: flee threats, seek food, or wander. Replicates at high energy.</summary>
    public class FaunaController : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Modules")]
        [Tooltip("Lifecycle component managing this entity's energy.")]
        [SerializeField] private Lifecycle lifecycle;
        [Tooltip("Motor used for NavMesh-based movement.")]
        [SerializeField] private NavMeshMotor motor;
        [Tooltip("Wander behavior used when not fleeing or seeking.")]
        [SerializeField] private NavMeshWander wander;
        [Tooltip("Sensor that detects nearby food targets.")]
        [SerializeField] private Sensor foodSensor;
        [Tooltip("Sensor that detects nearby threats to flee from.")]
        [SerializeField] private Sensor threatSensor;
        [Tooltip("Spawner used to create a replica on replication.")]
        [SerializeField] private Spawner replicationSpawner;

        [Header("Behavior")]
        [Tooltip("Distance to flee away from a detected threat.")]
        [Min(0f)]
        [SerializeField] private float fleeDistance = 10f;

        [Header("Replication")]
        [Tooltip("Energy ratio above which replication is triggered.")]
        [Range(0f, 1f)]
        [SerializeField] private float replicationThreshold = 0.8f;
        [Tooltip("Fraction of current energy spent on replication.")]
        [Range(0f, 1f)]
        [SerializeField] private float replicationCost = 0.5f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private string currentBehavior;

        //==================== OUTPUTS =====================
        public event Action OnReplicated;

        [Header("Events")]
        [Tooltip("Fired when this entity replicates")]
        [SerializeField] private UnityEvent replicatedEvent;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!lifecycle.IsAlive) return;

            CheckReplication();

            if (Flee())
            {
                currentBehavior = "Flee";
            }
            else if (Seek())
            {
                currentBehavior = "Seek";
            }
            else
            {
                currentBehavior = "Wander";
            }

            wander.enabled = currentBehavior == "Wander";
        }

        //==================== PRIVATE =====================
        private bool Flee()
        {
            if (!threatSensor.TryGetNearest(out var threat)) return false;
            if (!threat.Object) return false;

            var away = transform.position - threat.Object.transform.position;
            motor.MoveTo(transform.position + away.normalized * fleeDistance);
            return true;
        }

        private bool Seek()
        {
            if (!foodSensor.TryGetNearest(out var food)) return false;
            if (!food.Object) return false;

            motor.MoveTo(food.Object.transform.position);
            return true;
        }

        private void CheckReplication()
        {
            if (lifecycle.EnergyRatio < replicationThreshold) return;

            replicationSpawner.Spawn();
            lifecycle.RemoveEnergy(lifecycle.CurrentEnergy * replicationCost);
            OnReplicated?.Invoke();
            replicatedEvent?.Invoke();
        }
    }
}
