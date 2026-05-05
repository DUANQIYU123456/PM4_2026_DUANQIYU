// ============================================================================
// Toggleable — Logic / verb layer (IInteractable)
//
// Two-state "press to flip" verb. Each interact key-down flips an internal
// bool and fires the corresponding UnityEvent (OnToggleOn / OnToggleOff) —
// designers wire those events in the inspector to whatever should happen on
// each side (door open/close, light on/off, valve opened/closed).
//
// State lives on the component, not on the consumer. Initial state is
// configurable, so a door can start open or closed without extra wiring.
//
// Drop on any prop with a binary state. Pairs with Focusable for the standard
// prompt UI.
// ============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Two-way IInteractable. Each press flips IsOn and fires the
    /// corresponding UnityEvent.</summary>
    public class Toggleable : MonoBehaviour, IInteractable
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("State the toggle starts in when the scene loads.")]
        [SerializeField] private bool initialState = false;

        [Header("Events")]
        [Tooltip("Invoked when the toggle flips from off to on.")]
        [SerializeField] private UnityEvent onToggleOn;

        [Tooltip("Invoked when the toggle flips from on to off.")]
        [SerializeField] private UnityEvent onToggleOff;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isOn;

        //==================== PUBLIC API =====================
        public bool IsOn => isOn;

        //==================== IInteractable =====================
        public bool CanInteract => true;

        public void BeginInteract() => SetState(!isOn);

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            isOn = initialState;
        }

        //==================== PRIVATE =====================
        private void SetState(bool next)
        {
            isOn = next;
            if (isOn) onToggleOn?.Invoke();
            else onToggleOff?.Invoke();
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. On the prop the player should toggle:
//      - Add a Collider on a layer included in the player's RaycastSensor mask.
//      - (Optional) Add a Focusable for the standard prompt UI.
//      - Add this Toggleable component.
//   2. Wire OnToggleOn and OnToggleOff in the inspector to whatever should
//      happen on each side — Animator.SetBool, AudioSource.Play, GameObject
//      enable/disable, etc.
//   3. Set initialState if the prop should start "on" (e.g. a light that's
//      already lit when the scene loads). Other scripts can read IsOn to
//      branch on the toggle's current state.
// ============================================================================
