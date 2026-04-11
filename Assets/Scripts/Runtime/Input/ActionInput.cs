using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    /// <summary>Bridges a New Input System action to C# and UnityEvent outputs.</summary>
    public class ActionInput : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Reference to an Input Action from an Input Actions asset")]
        [SerializeField] private InputActionReference inputAction;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isPressed;

        public bool IsPressed => isPressed;

        //==================== OUTPUTS =====================
        public event Action OnPerformed;
        public event Action OnCanceled;

        [Header("Events")]
        [Tooltip("Invoked when the input action is performed")]
        [SerializeField] private UnityEvent performedEvent;

        [Tooltip("Invoked when the input action is canceled")]
        [SerializeField] private UnityEvent canceledEvent;

        //==================== LIFECYCLE =====================
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

        //==================== PRIVATE =====================
        private void HandlePerformed(InputAction.CallbackContext context)
        {
            isPressed = true;
            OnPerformed?.Invoke();
            performedEvent?.Invoke();
        }

        private void HandleCanceled(InputAction.CallbackContext context)
        {
            isPressed = false;
            OnCanceled?.Invoke();
            canceledEvent?.Invoke();
        }
    }
}
