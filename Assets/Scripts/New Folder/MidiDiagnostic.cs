using Minis;
using UnityEngine;
using UnityEngine.InputSystem;

public class MidiDiagnostic : MonoBehaviour
{
    [SerializeField] bool logNotes = true;
    [SerializeField] bool logControlChanges = true;

    void OnEnable()
    {
        foreach (var device in InputSystem.devices)
            TryHook(device);

        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;

        foreach (var device in InputSystem.devices)
            TryUnhook(device);
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added) TryHook(device);
        else if (change == InputDeviceChange.Removed) TryUnhook(device);
    }

    void TryHook(InputDevice device)
    {
        if (device is not MidiDevice midi) return;

        Debug.Log($"[MIDI] Hooked: '{midi.description.product}' channel {midi.channel} (id {midi.deviceId})");

        midi.onWillNoteOn          += OnNoteOn;
        midi.onWillNoteOff         += OnNoteOff;
        midi.onWillControlChange   += OnControlChange;
    }

    void TryUnhook(InputDevice device)
    {
        if (device is not MidiDevice midi) return;

        midi.onWillNoteOn          -= OnNoteOn;
        midi.onWillNoteOff         -= OnNoteOff;
        midi.onWillControlChange   -= OnControlChange;
    }

    void OnNoteOn(MidiNoteControl note, float velocity)
    {
        if (!logNotes) return;
        var ch = (note.device as MidiDevice)?.channel ?? -1;
        Debug.Log($"[MIDI] Note ON  ch{ch} note{note.noteNumber} vel={velocity:F3} | bind: <MidiDevice>/note{note.noteNumber:D3}");
    }

    void OnNoteOff(MidiNoteControl note)
    {
        if (!logNotes) return;
        var ch = (note.device as MidiDevice)?.channel ?? -1;
        Debug.Log($"[MIDI] Note OFF ch{ch} note{note.noteNumber}");
    }

    void OnControlChange(MidiValueControl cc, float value)
    {
        if (!logControlChanges) return;
        var ch = (cc.device as MidiDevice)?.channel ?? -1;
        Debug.Log($"[MIDI] CC ch{ch} cc{cc.controlNumber} value={value:F3} | bind: <MidiDevice>/control{cc.controlNumber:D3}");
    }
}
