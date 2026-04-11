// ============================================
// Lifecycle Data
// ============================================
// PURPOSE: Defines energy and decay parameters for any living entity.
// USAGE: Create via Assets > Create > Ludocore/Lifecycle Data.
//        Assign to Lifecycle component on the prefab.
//        One asset per entity type — Flora, Fauna, Apex each get their own.
// ============================================

using UnityEngine;

namespace Ludocore
{
    [CreateAssetMenu(fileName = "NewLifecycleData", menuName = "Ludocore/Lifecycle Data")]
    public class LifecycleData : ScriptableObject
    {
        [Header("Energy")]
        [Tooltip("Energy the entity starts with on enable.")]
        [Min(0f)]
        [SerializeField] private float startingEnergy = 100f;
        [Tooltip("Maximum energy the entity can hold.")]
        [Min(0.01f)]
        [SerializeField] private float maxEnergy = 100f;
        [Tooltip("Energy lost per second from passive decay.")]
        [Min(0f)]
        [SerializeField] private float energyDecayRate = 5f;

        public float StartingEnergy => startingEnergy;
        public float MaxEnergy => maxEnergy;
        public float EnergyDecayRate => energyDecayRate;
    }
}
