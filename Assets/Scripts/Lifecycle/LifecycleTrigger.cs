using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Fires an event once when Start runs on this component.</summary>
    public class LifecycleTrigger : MonoBehaviour
    {
        //==================== CONFIG =====================

        //==================== STATE =====================

        //==================== OUTPUTS =====================
        public event Action OnStarted;

        [Header("Events")]
        [Tooltip("Invoked once when Start runs on this component.")]
        [SerializeField] private UnityEvent startEvent;

        //==================== LIFECYCLE =====================
        private void Start()
        {
            OnStarted?.Invoke();
            startEvent?.Invoke();
        }
    }
}
