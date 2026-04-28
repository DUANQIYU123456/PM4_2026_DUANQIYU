// ============================================
// Int Variable
// ============================================
// Shared int value. One writer sets Value; many readers observe it or
// subscribe to OnChanged.
// Runtime state (default): resets to initialValue each play session.
// Shared config: set resetOnEnable = false to keep the value stable.
// ============================================

using System;
using UnityEngine;

namespace Ludocore
{
    [CreateAssetMenu(fileName = "NewIntVariable", menuName = "Ludocore/Variables/Int")]
    public class IntVariable : ScriptableObject
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Value the variable resets to when entering play mode.")]
        [SerializeField] private int initialValue;

        [Tooltip("Reset runtime value to initialValue when the asset is loaded. " +
                 "Disable if an external system (e.g. save/load) writes into the asset directly.")]
        [SerializeField] private bool resetOnEnable = true;

        //==================== STATE =====================
        [Header("Debug")]
        [Tooltip("Current runtime value. Visible during play.")]
        [ReadOnly, SerializeField] private int runtimeValue;

        public int Value
        {
            get => runtimeValue;
            set
            {
                if (runtimeValue == value) return;
                runtimeValue = value;
                OnChanged?.Invoke(runtimeValue);
            }
        }

        public int InitialValue => initialValue;

        //==================== OUTPUTS =====================
        public event Action<int> OnChanged;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (resetOnEnable) runtimeValue = initialValue;
            OnChanged = null; // clear stale subscribers from previous play session
        }

        //==================== INPUTS =====================
        public void Add(int delta) => Value = runtimeValue + delta;
        public void Increment() => Value = runtimeValue + 1;
    }
}
