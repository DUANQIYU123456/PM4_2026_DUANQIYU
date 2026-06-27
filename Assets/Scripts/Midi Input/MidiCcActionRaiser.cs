// ============================================
// MIDI CC Action Raiser
// ============================================
// PURPOSE: Same outputs as MidiCcEventRaiser (FloatVariable SO + UnityEvent<float>
//          + C# Action<float>) but driven by the new Input System ACTION layer via
//          an InputActionReference. Pick this if you want to leverage Input
//          Actions / control schemes / rebinding for your MIDI knobs.
//
// CAVEAT: For this to work on MIDI you MUST solve the PlayerInput user-pairing
//          problem, otherwise the action's binding will resolve to 0 controls and
//          never fire. Two ways:
//            (a) Add Minis' MidiDeviceAssigner to the GameObject holding
//                PlayerInput (channel -1, product "" = any).
//            (b) Bind this action in a SEPARATE InputActionAsset that no
//                PlayerInput owns.
//          The verbose-log flag will surface "Controls now: 0" so you can detect
//          this in the Console.
//
// BINDING: Path is zero-padded 3 digits: <MidiDevice>/control018, NOT /control18.
//          Action Type = Value, Expected Control Type = Axis (so .performed fires
//          on every value change, not just past a button press point).
// ============================================

using System;
using System.Collections;
using Minis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    public class MidiCcActionRaiser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("InputAction bound to a MIDI Control Change, e.g. <MidiDevice>/control018. " +
                 "Type = Value, Control Type = Axis.")]
        [SerializeField] private InputActionReference inputAction;

        [Tooltip("Log lifecycle, re-resolves, and every value change. Useful while wiring up.")]
        [SerializeField] private bool verboseLogging;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float lastValue;

        public float LastValue => lastValue;

        //==================== OUTPUTS =====================
        public event Action<float> OnChanged;

        [Header("Float Variable SO")]
        [Tooltip("Optional. The 0..1 CC value is written into this FloatVariable on every change. " +
                 "Other components can subscribe to the variable instead of this raiser.")]
        [SerializeField] private FloatVariable floatVariable;

        [Header("Unity Events")]
        [Tooltip("Invoked on every value change. Float arg is the 0..1 value.")]
        [SerializeField] private UnityEvent<float> changedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!inputAction)
            {
                Debug.LogWarning($"[CcActionRaiser:{name}] No InputActionReference assigned.", this);
                return;
            }

            inputAction.action.Enable();
            inputAction.action.performed += HandlePerformed;

            InputSystem.onDeviceChange += OnDeviceChange;

            if (verboseLogging) LogActionInfo("enable");
        }

        private void OnDisable()
        {
            if (!inputAction) return;

            InputSystem.onDeviceChange -= OnDeviceChange;

            inputAction.action.performed -= HandlePerformed;
            inputAction.action.Disable();
        }

        //==================== PRIVATE =====================
        private void HandlePerformed(InputAction.CallbackContext ctx)
        {
            float value = ctx.ReadValue<float>();

            lastValue = value;

            if (verboseLogging) Debug.Log($"[CcActionRaiser:{name}] value={value:F3}", this);

            OnChanged?.Invoke(value);
            changedEvent?.Invoke(value);
            if (floatVariable) floatVariable.Value = value;
        }

        // Same lazy-MidiDevice + deferred re-resolve workaround as MidiNoteActionRaiser.
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
                $"[CcActionRaiser:{name}] {when} — action '{a.name}' enabled={a.enabled} controls={a.controls.Count}",
                this);
            if (a.controls.Count == 0)
                Debug.LogWarning($"[CcActionRaiser:{name}] 0 controls — likely PlayerInput user-pairing or wrong path. See header comment.", this);
        }
    }
}
