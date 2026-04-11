using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Holds a boolean state driven by camera visibility.</summary>
    [RequireComponent(typeof(Renderer))]
    public class VisibilityGate : MonoBehaviour
    {
        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isVisible;

        public bool IsVisible => isVisible;

        //==================== OUTPUTS =====================
        public event Action OnShown;
        public event Action OnHidden;

        [Header("Events")]
        [Tooltip("Invoked when the renderer becomes visible to any camera")]
        [SerializeField] private UnityEvent shownEvent;

        [Tooltip("Invoked when the renderer is no longer visible to any camera")]
        [SerializeField] private UnityEvent hiddenEvent;

        //==================== LIFECYCLE =====================
        private void OnBecameVisible()
        {
            isVisible = true;
            OnShown?.Invoke();
            shownEvent?.Invoke();
        }

        private void OnBecameInvisible()
        {
            isVisible = false;
            OnHidden?.Invoke();
            hiddenEvent?.Invoke();
        }
    }
}
