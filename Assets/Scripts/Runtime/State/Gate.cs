using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Holds a boolean state and notifies when it changes.</summary>
    public class Gate : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Starting value of the gate when the scene begins")]
        [SerializeField] private bool initialValue;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool value;

        public bool Value => value;

        //==================== OUTPUTS =====================
        public event Action<bool> OnChanged;

        [Header("Events")]
        [Tooltip("Invoked when the gate value changes, passes the new value")]
        [SerializeField] private UnityEvent<bool> changedEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            value = initialValue;
        }

        //==================== INPUTS =====================
        /// <summary>Set the gate to true. Fires OnChanged if value actually changes.</summary>
        [ContextMenu("Set True")]
        public void SetTrue() => Apply(true);

        /// <summary>Set the gate to false. Fires OnChanged if value actually changes.</summary>
        [ContextMenu("Set False")]
        public void SetFalse() => Apply(false);

        /// <summary>Flip the gate value.</summary>
        [ContextMenu("Toggle")]
        public void Toggle() => Apply(!value);

        //==================== PRIVATE =====================
        private void Apply(bool newValue)
        {
            if (value == newValue) return;

            value = newValue;
            OnChanged?.Invoke(value);
            changedEvent?.Invoke(value);
        }
    }
}
