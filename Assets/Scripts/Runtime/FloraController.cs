using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Flora behavior: spawns plants as energy grows, replicates at threshold.</summary>
    public class FloraController : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Modules")]
        [Tooltip("Lifecycle component managing this entity's energy.")]
        [SerializeField] private Lifecycle lifecycle;
        [Tooltip("Spawner that creates plant children as energy grows.")]
        [SerializeField] private Spawner plantSpawner;
        [Tooltip("Spawner used to create a replica on replication.")]
        [SerializeField] private Spawner replicationSpawner;

        [Header("Settings")]
        [Tooltip("Energy needed to spawn each additional plant child.")]
        [Min(0.01f)]
        [SerializeField] private float energyPerPlant = 5f;
        [Tooltip("Energy threshold that triggers replication and self-destruction.")]
        [Min(0f)]
        [SerializeField] private float replicationEnergy = 200f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float nextPlantThreshold;
        [ReadOnly, SerializeField] private bool hasReplicated;

        //==================== OUTPUTS =====================
        public event Action OnReplicated;

        [Header("Events")]
        [Tooltip("Fired when this flora replicates")]
        [SerializeField] private UnityEvent replicatedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            nextPlantThreshold = lifecycle.CurrentEnergy + energyPerPlant;
            hasReplicated = false;
            lifecycle.OnEnergyChanged += HandleEnergyChanged;
        }

        private void OnDisable()
        {
            lifecycle.OnEnergyChanged -= HandleEnergyChanged;
        }

        //==================== PRIVATE =====================
        private void HandleEnergyChanged(float energy)
        {
            CheckPlant(energy);
            CheckReplication(energy);
        }

        private void CheckPlant(float energy)
        {
            if (!plantSpawner) return;
            if (energy < nextPlantThreshold) return;

            plantSpawner.SpawnOne();
            nextPlantThreshold += energyPerPlant;
        }

        private void CheckReplication(float energy)
        {
            if (hasReplicated) return;
            if (energy < replicationEnergy) return;

            hasReplicated = true;
            replicationSpawner.Spawn();
            OnReplicated?.Invoke();
            replicatedEvent?.Invoke();
            Destroy(gameObject);
        }
    }
}
