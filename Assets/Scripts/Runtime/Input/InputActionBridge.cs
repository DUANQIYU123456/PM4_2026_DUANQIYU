using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    /// <summary>Bridges a New Input System action to C# and UnityEvent outputs.</summary>
    public class InputActionBridge : MonoBehaviour
    {
        // ═══════════════════════════════════════
        // CONFIG
        // ═══════════════════════════════════════
        [Header("Config")]
        [Tooltip("Reference to an Input Action from an Input Actions asset")]
        [SerializeField] private InputActionReference inputAction;

        // ═══════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════
        private bool _isPressed;

        public bool IsPressed => _isPressed;

        // ═══════════════════════════════════════
        // OUTPUTS
        // ═══════════════════════════════════════
        public event Action OnPerformed;
        public event Action OnCanceled;

        [Header("Events")]
        [SerializeField] private UnityEvent performedEvent;
        [SerializeField] private UnityEvent canceledEvent;

        // ═══════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════
        private void OnEnable()
        {
            if (!inputAction) return;

            inputAction.action.Enable();
            inputAction.action.performed += HandlePerformed;
            inputAction.action.canceled += HandleCanceled;
        }

        private void OnDisable()
        {
            if (!inputAction) return;

            inputAction.action.performed -= HandlePerformed;
            inputAction.action.canceled -= HandleCanceled;
            inputAction.action.Disable();
        }

        // ═══════════════════════════════════════
        // PRIVATE
        // ═══════════════════════════════════════
        private void HandlePerformed(InputAction.CallbackContext context)
        {
            _isPressed = true;
            OnPerformed?.Invoke();
            performedEvent?.Invoke();
        }

        private void HandleCanceled(InputAction.CallbackContext context)
        {
            _isPressed = false;
            OnCanceled?.Invoke();
            canceledEvent?.Invoke();
        }
    }
}
