using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Fires events when this object becomes visible or invisible to any camera.</summary>
    [RequireComponent(typeof(Renderer))]
    public class VisibilitySensor : MonoBehaviour
    {
        // ═══════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════
        private bool _isVisible;

        public bool IsVisible => _isVisible;

        // ═══════════════════════════════════════
        // OUTPUTS
        // ═══════════════════════════════════════
        public event Action OnShown;
        public event Action OnHidden;

        [Header("Events")]
        [SerializeField] private UnityEvent visibleEvent;
        [SerializeField] private UnityEvent invisibleEvent;

        // ═══════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════
        private void OnBecameVisible()
        {
            _isVisible = true;
            OnShown?.Invoke();
            visibleEvent?.Invoke();
        }

        private void OnBecameInvisible()
        {
            _isVisible = false;
            OnHidden?.Invoke();
            invisibleEvent?.Invoke();
        }
    }
}
