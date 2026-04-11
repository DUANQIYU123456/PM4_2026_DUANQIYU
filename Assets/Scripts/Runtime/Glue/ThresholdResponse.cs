using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Fires events when a float value crosses a threshold.</summary>
    public class ThresholdResponse : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("The value boundary that triggers crossing events")]
        [SerializeField] private float threshold = 0.5f;

        [Tooltip("Fire when value goes above threshold (true) or below it (false).")]
        [SerializeField] private bool fireAbove = true;

        //==================== STATE =====================
        private bool _isCrossed;
        private bool _initialized;

        //==================== OUTPUTS =====================
        public event Action OnCrossed;
        public event Action OnRecovered;

        [Header("Events")]
        [Tooltip("Invoked when the value crosses the threshold in the watched direction")]
        [SerializeField] private UnityEvent crossedEvent;

        [Tooltip("Invoked when the value returns from the crossed side")]
        [SerializeField] private UnityEvent recoveredEvent;

        //==================== INPUTS =====================
        /// <summary>Feed a value from any module. Wire via UnityEvent or call from code.</summary>
        public void SetValue(float value)
        {
            bool crossed = fireAbove ? value >= threshold : value <= threshold;

            if (!_initialized)
            {
                _isCrossed = crossed;
                _initialized = true;
                return;
            }

            if (crossed == _isCrossed) return;

            _isCrossed = crossed;

            if (_isCrossed)
            {
                OnCrossed?.Invoke();
                crossedEvent?.Invoke();
            }
            else
            {
                OnRecovered?.Invoke();
                recoveredEvent?.Invoke();
            }
        }
    }
}
