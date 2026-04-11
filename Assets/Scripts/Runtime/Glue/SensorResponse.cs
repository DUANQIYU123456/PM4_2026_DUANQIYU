using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Bridges sensor detection state to UnityEvents each frame.</summary>
    public class SensorResponse : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("The Sensor module to listen to")]
        [SerializeField] private Sensor sensor;

        //==================== STATE =====================
        private bool _hadSignal;

        //==================== OUTPUTS =====================
        public event Action<Vector3> OnWhileDetected;
        public event Action OnFirstDetected;
        public event Action OnAllLost;

        [Header("Events")]
        [Tooltip("Fires every frame while a signal is detected. Passes nearest position.")]
        [SerializeField] private UnityEvent<Vector3> whileDetectedEvent;

        [Tooltip("Fires once when the first signal is detected.")]
        [SerializeField] private UnityEvent firstDetectedEvent;

        [Tooltip("Fires once when the last signal is lost.")]
        [SerializeField] private UnityEvent allLostEvent;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!sensor) return;

            bool hasSignal = sensor.TryGetNearest(out var nearest);

            if (hasSignal)
            {
                if (!_hadSignal)
                {
                    OnFirstDetected?.Invoke();
                    firstDetectedEvent?.Invoke();
                }
                Vector3 pos = nearest.Object.transform.position;
                OnWhileDetected?.Invoke(pos);
                whileDetectedEvent?.Invoke(pos);
            }
            else if (_hadSignal)
            {
                OnAllLost?.Invoke();
                allLostEvent?.Invoke();
            }

            _hadSignal = hasSignal;
        }
    }
}
