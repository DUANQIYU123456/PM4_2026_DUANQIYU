// ============================================
// Input Action Event Raiser
// ============================================
// PURPOSE: Bridges a New Input System action (keyboard, gamepad, MIDI pad, etc.)
//          to all three reaction styles at once: a ScriptableObject GameEvent
//          channel, an Inspector UnityEvent, and a C# Action for code subscribers.
// USAGE: Assign an InputActionReference (e.g. a MIDI pad bound to
//        <MidiDevice>/note36). Optionally assign a GameEvent SO to raise on press
//        and another on release. Inspector UnityEvents and C# events fire too.
// ============================================

using System;
using System.Collections;
using Minis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    public class InputActionEventRaiser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Reference to an Input Action from an Input Actions asset. Works for MIDI pads, keys, buttons.")]
        [SerializeField] private InputActionReference inputAction;

        [Tooltip("Log every Started / Performed / Canceled callback and the action's binding info on Enable. Use to diagnose why a binding isn't firing.")]
        [SerializeField] private bool verboseLogging;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isPressed;

        public bool IsPressed => isPressed;

        //==================== OUTPUTS =====================
        public event Action OnPressed;
        public event Action OnReleased;

        [Header("Game Event SO")]
        [Tooltip("Raised when the action is performed (button down / note on).")]
        [SerializeField] private GameEvent pressedGameEvent;

        [Tooltip("Raised when the action is canceled (button up / note off).")]
        [SerializeField] private GameEvent releasedGameEvent;

        [Header("Unity Events")]
        [Tooltip("Invoked when the action is performed.")]
        [SerializeField] private UnityEvent pressedEvent;

        [Tooltip("Invoked when the action is canceled.")]
        [SerializeField] private UnityEvent releasedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!inputAction)
            {
                Debug.LogWarning($"[Raiser:{name}] No InputActionReference assigned.", this);
                return;
            }

            inputAction.action.Enable();
            inputAction.action.started   += HandleStarted;
            inputAction.action.performed += HandlePerformed;
            inputAction.action.canceled  += HandleCanceled;

            InputSystem.onDeviceChange += OnDeviceChange;

            if (verboseLogging) LogActionInfo();
        }

        private void OnDisable()
        {
            if (!inputAction) return;

            InputSystem.onDeviceChange -= OnDeviceChange;

            inputAction.action.started   -= HandleStarted;
            inputAction.action.performed -= HandlePerformed;
            inputAction.action.canceled  -= HandleCanceled;
            inputAction.action.Disable();
        }

        // Minis creates a MidiDevice lazily — on the FIRST message per channel.
        // If our action was enabled before that, it resolved zero controls and stays dead.
        // Force a re-resolve whenever a MidiDevice is added — but DEFER one frame,
        // because onDeviceChange fires INSIDE InputSystem.AddDevice before the
        // device is fully registered with its layout (controls would still be 0).
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Added || device is not MidiDevice) return;
            if (isActiveAndEnabled) StartCoroutine(ReresolveNextFrame());
        }

        private IEnumerator ReresolveNextFrame()
        {
            yield return null;
            inputAction.action.Disable();
            inputAction.action.Enable();
            if (verboseLogging)
                Debug.Log($"[Raiser:{name}] Re-resolved after MidiDevice add. Controls now: {inputAction.action.controls.Count}", this);
        }

        //==================== PRIVATE =====================
        private void HandleStarted(InputAction.CallbackContext context)
        {
            if (verboseLogging) Debug.Log($"[Raiser:{name}] STARTED path='{context.control?.path}' value={context.ReadValueAsObject()}", this);
        }

        private void HandlePerformed(InputAction.CallbackContext context)
        {
            isPressed = true;
            if (verboseLogging) Debug.Log($"[Raiser:{name}] PERFORMED path='{context.control?.path}' value={context.ReadValueAsObject()}", this);
            OnPressed?.Invoke();
            pressedEvent?.Invoke();
            if (pressedGameEvent) pressedGameEvent.Raise();
        }

        private void HandleCanceled(InputAction.CallbackContext context)
        {
            isPressed = false;
            if (verboseLogging) Debug.Log($"[Raiser:{name}] CANCELED path='{context.control?.path}'", this);
            OnReleased?.Invoke();
            releasedEvent?.Invoke();
            if (releasedGameEvent) releasedGameEvent.Raise();
        }

        private void LogActionInfo()
        {
            var a = inputAction.action;
            var bindings = string.Join(", ", a.bindings);
            Debug.Log(
                $"[Raiser:{name}] Action '{a.name}' enabled={a.enabled} type={a.type} expectedControlType='{a.expectedControlType}'\n" +
                $"  bindings: {bindings}\n" +
                $"  resolved controls: {a.controls.Count}",
                this);
            foreach (var c in a.controls)
                Debug.Log($"[Raiser:{name}]   resolved -> {c.path}  (device='{c.device.description.product}')", this);
        }
    }
}
