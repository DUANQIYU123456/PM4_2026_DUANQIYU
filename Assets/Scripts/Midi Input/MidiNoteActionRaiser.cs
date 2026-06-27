// ============================================
// MIDI Note Action Raiser
// ============================================
// PURPOSE: Same outputs as MidiNoteEventRaiser (GameEvent SO + UnityEvent + C# Action,
//          with velocity on press) but driven by the new Input System ACTION layer
//          via an InputActionReference. Pick this if you want to leverage Input
//          Actions / control schemes / rebinding for your MIDI pads.
//
// CAVEAT: For this to work on MIDI you MUST solve the PlayerInput user-pairing
//          problem, otherwise the action's binding will resolve to 0 controls
//          and never fire. Two ways:
//            (a) Add Minis' MidiDeviceAssigner to the GameObject holding
//                PlayerInput (channel -1, product "" = any).
//            (b) Bind this action in a SEPARATE InputActionAsset that no
//                PlayerInput owns.
//          The verbose-log flag will surface "Controls now: 0" so you can detect
//          this in the Console.
//
// BINDING: Path is zero-padded 3 digits: <MidiDevice>/note060, NOT /note60.
//          Action Type = Button (or Value/Axis if you want velocity on every
//          state change — Button only fires once per press at the press point).
// ============================================

using System;
using System.Collections;
using Minis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    public class MidiNoteActionRaiser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("InputAction bound to a MIDI note, e.g. <MidiDevice>/note060.")]
        [SerializeField] private InputActionReference inputAction;

        [Tooltip("Log lifecycle, re-resolves, and every press/release. Useful while wiring up.")]
        [SerializeField] private bool verboseLogging;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isPressed;
        [ReadOnly, SerializeField] private float lastVelocity;

        public bool IsPressed => isPressed;
        public float LastVelocity => lastVelocity;

        //==================== OUTPUTS =====================
        public event Action<float> OnPressed;
        public event Action OnReleased;

        [Header("Game Event SO")]
        [Tooltip("Raised on press (action performed).")]
        [SerializeField] private GameEvent pressedGameEvent;

        [Tooltip("Raised on release (action canceled).")]
        [SerializeField] private GameEvent releasedGameEvent;

        [Header("Unity Events")]
        [Tooltip("Invoked on press. Float arg is velocity (0..1) for MIDI, or 1 for non-velocity buttons.")]
        [SerializeField] private UnityEvent<float> pressedEvent;

        [Tooltip("Invoked on release.")]
        [SerializeField] private UnityEvent releasedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!inputAction)
            {
                Debug.LogWarning($"[NoteActionRaiser:{name}] No InputActionReference assigned.", this);
                return;
            }

            inputAction.action.Enable();
            inputAction.action.performed += HandlePerformed;
            inputAction.action.canceled  += HandleCanceled;

            InputSystem.onDeviceChange += OnDeviceChange;

            if (verboseLogging) LogActionInfo("enable");
        }

        private void OnDisable()
        {
            if (!inputAction) return;

            InputSystem.onDeviceChange -= OnDeviceChange;

            inputAction.action.performed -= HandlePerformed;
            inputAction.action.canceled  -= HandleCanceled;
            inputAction.action.Disable();
        }

        //==================== PRIVATE =====================
        private void HandlePerformed(InputAction.CallbackContext ctx)
        {
            float velocity = ctx.ReadValue<float>();
            // Button-type actions read 1 on press; pass-through Value reads the actual velocity.
            if (velocity <= 0f) velocity = 1f;

            isPressed = true;
            lastVelocity = velocity;

            if (verboseLogging) Debug.Log($"[NoteActionRaiser:{name}] PRESS vel={velocity:F3}", this);

            OnPressed?.Invoke(velocity);
            pressedEvent?.Invoke(velocity);
            if (pressedGameEvent) pressedGameEvent.Raise();
        }

        private void HandleCanceled(InputAction.CallbackContext ctx)
        {
            isPressed = false;

            if (verboseLogging) Debug.Log($"[NoteActionRaiser:{name}] RELEASE", this);

            OnReleased?.Invoke();
            releasedEvent?.Invoke();
            if (releasedGameEvent) releasedGameEvent.Raise();
        }

        // Minis creates MidiDevices lazily; the binding's resolved-controls list can be
        // empty at OnEnable. Re-resolve when a MidiDevice appears (deferred one frame
        // because onDeviceChange fires inside InputSystem.AddDevice).
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
            if (verboseLogging) LogActionInfo("re-resolve after MidiDevice add");
        }

        private void LogActionInfo(string when)
        {
            var a = inputAction.action;
            Debug.Log(
                $"[NoteActionRaiser:{name}] {when} — action '{a.name}' enabled={a.enabled} controls={a.controls.Count}",
                this);
            if (a.controls.Count == 0)
                Debug.LogWarning($"[NoteActionRaiser:{name}] 0 controls — likely PlayerInput user-pairing or wrong path. See header comment.", this);
        }
    }
}
