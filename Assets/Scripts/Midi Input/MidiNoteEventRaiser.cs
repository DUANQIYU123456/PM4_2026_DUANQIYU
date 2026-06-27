// ============================================
// MIDI Note Event Raiser
// ============================================
// PURPOSE: Bridges a specific MIDI note (pad or key) to a GameEvent ScriptableObject
//          channel, an Inspector UnityEvent, and a C# Action. Hooks Minis' device
//          API directly — no InputAction asset / binding path resolution required.
// USAGE: Set 'Note Number' to the MIDI note (e.g. 60 = C4, 36 = lowest MiniLab pad).
//        Optional 'Channel' filter (-1 = any). Assign GameEvent SOs or wire up
//        UnityEvents in the inspector. Velocity (0..1) is forwarded on press.
// ============================================

using System;
using System.Collections.Generic;
using Minis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ludocore
{
    public class MidiNoteEventRaiser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("MIDI note number (0..127). C4 = 60. MiniLab pads typically start at 36.")]
        [Range(0, 127)]
        [SerializeField] private int noteNumber = 60;

        [Tooltip("MIDI channel filter. -1 = listen on any channel. 0..15 = specific channel.")]
        [Range(-1, 15)]
        [SerializeField] private int channel = -1;

        [Tooltip("Log every Note On / Note Off this raiser receives.")]
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
        [Tooltip("Raised on note-on.")]
        [SerializeField] private GameEvent pressedGameEvent;

        [Tooltip("Raised on note-off.")]
        [SerializeField] private GameEvent releasedGameEvent;

        [Header("Unity Events")]
        [Tooltip("Invoked on note-on. Float arg is velocity (0..1).")]
        [SerializeField] private UnityEvent<float> pressedEvent;

        [Tooltip("Invoked on note-off.")]
        [SerializeField] private UnityEvent releasedEvent;

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

            midi.onWillNoteOn  += HandleNoteOn;
            midi.onWillNoteOff += HandleNoteOff;

            if (verboseLogging)
                Debug.Log($"[NoteRaiser:{name}] Hooked '{midi.description.product}' ch{midi.channel} for note{noteNumber}", this);
        }

        private void TryUnhook(InputDevice device)
        {
            if (device is MidiDevice midi && _hooked.Remove(midi)) Unhook(midi);
        }

        private void Unhook(MidiDevice midi)
        {
            midi.onWillNoteOn  -= HandleNoteOn;
            midi.onWillNoteOff -= HandleNoteOff;
        }

        private void HandleNoteOn(MidiNoteControl note, float velocity)
        {
            if (note.noteNumber != noteNumber) return;

            isPressed = true;
            lastVelocity = velocity;

            if (verboseLogging) Debug.Log($"[NoteRaiser:{name}] note{noteNumber} ON vel={velocity:F3}", this);

            OnPressed?.Invoke(velocity);
            pressedEvent?.Invoke(velocity);
            if (pressedGameEvent) pressedGameEvent.Raise();
        }

        private void HandleNoteOff(MidiNoteControl note)
        {
            if (note.noteNumber != noteNumber) return;

            isPressed = false;

            if (verboseLogging) Debug.Log($"[NoteRaiser:{name}] note{noteNumber} OFF", this);

            OnReleased?.Invoke();
            releasedEvent?.Invoke();
            if (releasedGameEvent) releasedGameEvent.Raise();
        }
    }
}
