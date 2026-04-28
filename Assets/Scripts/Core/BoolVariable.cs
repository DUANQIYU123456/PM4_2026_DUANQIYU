// ============================================
// Bool Variable
// ============================================
// Shared bool value. One writer sets Value; many readers observe it or
// subscribe to OnChanged.
// Runtime state (default): resets to initialValue each play session.
// Shared config: set resetOnEnable = false to keep the value stable.
// ============================================

using System;
using UnityEngine;

namespace Ludocore
{
    [CreateAssetMenu(fileName = "NewBoolVariable", menuName = "Ludocore/Variables/Bool")]
    public class BoolVariable : ScriptableObject
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Value the variable resets to when entering play mode.")]
        [SerializeField] private bool initialValue;

        [Tooltip("Reset runtime value to initialValue when the asset is loaded. " +
                 "Disable if an external system (e.g. save/load) writes into the asset directly.")]
        [SerializeField] private bool resetOnEnable = true;

        //==================== STATE =====================
        [Header("Debug")]
        [Tooltip("Current runtime value. Visible during play.")]
        [ReadOnly, SerializeField] private bool runtimeValue;

        public bool Value
        {
            get => runtimeValue;
            set
            {
                if (runtimeValue == value) return;
                runtimeValue = value;
                OnChanged?.Invoke(runtimeValue);
            }
        }

        public bool InitialValue => initialValue;

        //==================== OUTPUTS =====================
        public event Action<bool> OnChanged;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (resetOnEnable) runtimeValue = initialValue;
            OnChanged = null; // clear stale subscribers from previous play session
        }

        //==================== INPUTS =====================
        public void Toggle() => Value = !runtimeValue;
        public void SetTrue() => Value = true;
        public void SetFalse() => Value = false;
    }
}
