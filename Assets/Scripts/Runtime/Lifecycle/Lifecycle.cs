using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Manages energy — decay over time, feeding, and death.</summary>
    public class Lifecycle : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("ScriptableObject defining energy and decay parameters.")]
        [SerializeField] private LifecycleData lifecycleData;
        [Tooltip("Destroy the GameObject when energy reaches zero.")]
        [SerializeField] private bool destroyOnDeath;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float currentEnergy;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => lifecycleData.MaxEnergy;
        public float EnergyRatio => currentEnergy / lifecycleData.MaxEnergy;
        public bool IsAlive => currentEnergy > 0f;

        //==================== OUTPUTS =====================
        public event Action<float> OnEnergyChanged;
        public event Action OnDied;

        [Header("Events")]
        [Tooltip("Fired when energy is added or removed, passes current energy.")]
        [SerializeField] private UnityEvent<float> energyChangedEvent;
        [Tooltip("Fired when energy reaches zero and the entity dies.")]
        [SerializeField] private UnityEvent diedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            currentEnergy = lifecycleData.StartingEnergy;
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Decay is passive per-frame drain — does not fire events
            currentEnergy -= lifecycleData.EnergyDecayRate * Time.deltaTime;

            if (currentEnergy <= 0f) Die();
        }

        //==================== INPUTS =====================
        /// <summary>Add energy, capped at maximum.</summary>
        public void AddEnergy(float amount)
        {
            if (!IsAlive) return;
            if (amount <= 0f) return;

            currentEnergy = Mathf.Min(currentEnergy + amount, lifecycleData.MaxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
            energyChangedEvent?.Invoke(currentEnergy);
        }

        /// <summary>Remove energy. May trigger death.</summary>
        public void RemoveEnergy(float amount)
        {
            if (!IsAlive) return;
            if (amount <= 0f) return;

            currentEnergy = Mathf.Max(0f, currentEnergy - amount);
            OnEnergyChanged?.Invoke(currentEnergy);
            energyChangedEvent?.Invoke(currentEnergy);

            if (currentEnergy <= 0f) Die();
        }

        //==================== PRIVATE =====================
        private void Die()
        {
            currentEnergy = 0f;
            OnDied?.Invoke();
            diedEvent?.Invoke();

            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}
