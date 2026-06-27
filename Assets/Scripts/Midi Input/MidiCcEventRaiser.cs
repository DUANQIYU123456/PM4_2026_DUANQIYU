// ============================================
// MIDI CC Event Raiser
// ============================================
// PURPOSE: Bridges a specific MIDI Control Change (knob / fader / slider) to a
//          FloatVariable ScriptableObject, an Inspector UnityEvent<float>, and
//          a C# Action<float>. Hooks Minis' device API directly — no InputAction
//          asset / binding path resolution required.
// USAGE: Set 'CC Number' to the controller (knob CC printed by MidiDiagnostic).
//        Optional 'Channel' filter (-1 = any). Assign a FloatVariable to act as
//        the shared 0..1 value, or wire UnityEvents inspector-side. Sinks read
//        the FloatVariable instead of coupling to this component directly.
// ============================================

using System;
using System.Collections.Generic;
using Minis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    public class MidiCcEventRaiser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("MIDI Control Change number (0..127). Read it from MidiDiagnostic when you turn the knob.")]
        [Range(0, 127)]
        [SerializeField] private int ccNumber = 7;

        [Tooltip("MIDI channel filter. -1 = listen on any channel. 0..15 = specific channel.")]
        [Range(-1, 15)]
        [SerializeField] private int channel = -1;

        [Tooltip("Log every CC change this raiser receives.")]
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
        [Tooltip("Invoked on every CC change. Float arg is the 0..1 value.")]
        [SerializeField] private UnityEvent<float> changedEvent;

        //==================== STATE (PRIVATE) =====================
        private readonly HashSet<MidiDevice> _hooked = new();

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            foreach (var d in InputSystem.devices) TryHook(d);
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var midi in _hooked) Unhook(midi);
            _hooked.Clear();
        }

        //==================== PRIVATE =====================
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)         TryHook(device);
            else if (change == InputDeviceChange.Removed)  TryUnhook(device);
        }

        private void TryHook(InputDevice device)
        {
            if (device is not MidiDevice midi) return;
            if (channel >= 0 && midi.channel != channel) return;
            if (!_hooked.Add(midi)) return;

            midi.onWillControlChange += HandleControlChange;

            if (verboseLogging)
                Debug.Log($"[CcRaiser:{name}] Hooked '{midi.description.product}' ch{midi.channel} for cc{ccNumber}", this);
        }

        private void TryUnhook(InputDevice device)
        {
            if (device is MidiDevice midi && _hooked.Remove(midi)) Unhook(midi);
        }

        private void Unhook(MidiDevice midi)
        {
            midi.onWillControlChange -= HandleControlChange;
        }

        private void HandleControlChange(MidiValueControl cc, float value)
        {
            if (cc.controlNumber != ccNumber) return;

            lastValue = value;

            if (verboseLogging) Debug.Log($"[CcRaiser:{name}] cc{ccNumber}={value:F3}", this);

            OnChanged?.Invoke(value);
            changedEvent?.Invoke(value);
            if (floatVariable) floatVariable.Value = value;
        }
    }
}
