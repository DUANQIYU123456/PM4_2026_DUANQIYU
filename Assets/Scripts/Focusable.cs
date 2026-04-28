// ============================================================================
// Focusable — Logic layer (reusable primitive)
//
// Tracks a single bit: "is this object the player's current focus target?"
// Fires events on transitions. Any UI (world-space labels, outlines,
// highlights) can listen and react.
//
// Drop this on any GameObject that should respond to the player's gaze:
// harvestables, build sites, NPCs, doors, signs. PlayerInteractor flips
// the bit via SetFocused(true / false) through the IFocusable contract.
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Tracks focus state and fires OnFocused / OnUnfocused on transitions.</summary>
    public class Focusable : MonoBehaviour, IFocusable
    {
        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isFocused;

        public bool IsFocused => isFocused;

        //==================== OUTPUTS =====================
        public event Action OnFocused;
        public event Action OnUnfocused;

        [Header("Events")]
        [Tooltip("Fired when this object becomes the player's focus.")]
        [SerializeField] private UnityEvent focusedEvent;

        [Tooltip("Fired when this object stops being the player's focus.")]
        [SerializeField] private UnityEvent unfocusedEvent;

        //==================== LIFECYCLE =====================
        private void OnDisable()
        {
            // If we get disabled / destroyed while focused, clean the state.
            if (isFocused) SetFocused(false);
        }

        //==================== INPUTS =====================
        /// <summary>Set focus state. Fires events only on actual transitions.</summary>
        public void SetFocused(bool focused)
        {
            if (isFocused == focused) return;

            isFocused = focused;

            if (focused)
            {
                OnFocused?.Invoke();
                focusedEvent?.Invoke();
            }
            else
            {
                OnUnfocused?.Invoke();
                unfocusedEvent?.Invoke();
            }
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. Add this component to any GameObject that should respond to the
//      player's gaze (typically on the same object as Harvestable, BuildSite,
//      an NPC, etc.).
//   2. Other scripts (e.g. HarvestableUI) subscribe to OnFocused / OnUnfocused
//      to show or hide prompts.
//   3. PlayerInteractor will call SetFocused() automatically through the
//      IFocusable contract — no manual wiring required.
// ============================================================================
