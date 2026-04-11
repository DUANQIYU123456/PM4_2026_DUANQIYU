using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Splits a Gate's boolean change into separate true/false events.</summary>
    public class GateResponse : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("The Gate module to listen to")]
        [SerializeField] private Gate gate;

        //==================== OUTPUTS =====================
        public event Action OnTrue;
        public event Action OnFalse;

        [Header("Events")]
        [Tooltip("Invoked when the gate becomes true")]
        [SerializeField] private UnityEvent trueEvent;

        [Tooltip("Invoked when the gate becomes false")]
        [SerializeField] private UnityEvent falseEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (gate) gate.OnChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (gate) gate.OnChanged -= HandleChanged;
        }

        //==================== PRIVATE =====================
        private void HandleChanged(bool value)
        {
            if (value)
            {
                OnTrue?.Invoke();
                trueEvent?.Invoke();
            }
            else
            {
                OnFalse?.Invoke();
                falseEvent?.Invoke();
            }
        }
    }
}
