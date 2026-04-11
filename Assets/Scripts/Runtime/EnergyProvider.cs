// ============================================
// Energy Provider
// ============================================
// PURPOSE: Provides energy into a Lifecycle at a constant rate.
//          Can be toggled on/off for future conditions (sun, water, soil).
// USAGE: Attach alongside Lifecycle. Set rate in Inspector or via data.
//        Enable/disable to simulate environmental conditions.
// ============================================

using UnityEngine;

namespace Ludocore
{
    /// <summary>Provides energy into a Lifecycle at a constant rate.</summary>
    public class EnergyProvider : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Lifecycle that receives energy each frame.")]
        [SerializeField] private Lifecycle lifecycle;

        [Tooltip("Amount of energy added per second while enabled.")]
        [Min(0f)]
        [SerializeField] private float energyPerSecond = 10f;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            ProvideEnergy();
        }

        //==================== PRIVATE =====================
        private void ProvideEnergy()
        {
            if (!lifecycle.IsAlive) return;

            lifecycle.AddEnergy(energyPerSecond * Time.deltaTime);
        }
    }
}
