// ============================================================================
// Activatable — Logic / verb layer (IInteractable)
//
// One-shot or repeatable "do something in the world" verb. On each interact
// key-down, fires its OnActivate UnityEvent — designers wire the event in the
// inspector to whatever should happen (animator trigger, audio, particle,
// custom script). The component itself has no opinion about what activation
// means.
//
// Two flavors via the oneShot flag:
//   oneShot = false → fires every press (lever, doorbell, button you can spam)
//   oneShot = true  → fires once, then CanInteract flips false forever
//                     (key pickup, single-use switch, consumable trigger)
//
// Drop on any prop that should be "press to do X". Pairs with Focusable for
// the standard prompt UI.
// ============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Fire-and-forget IInteractable. Press the interact key to invoke
    /// OnActivate. Set oneShot to disable further activations after the first.</summary>
    public class Activatable : MonoBehaviour, IInteractable
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("If true, the verb fires once and then refuses further presses.")]
        [SerializeField] private bool oneShot = false;

        [Header("Events")]
        [Tooltip("Invoked on every successful interact key-down (or once, if oneShot).")]
        [SerializeField] private UnityEvent onActivate;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool consumed;

        //==================== IInteractable =====================
        public bool CanInteract => !consumed;

        public void BeginInteract()
        {
            if (!CanInteract) return;

            onActivate?.Invoke();
            if (oneShot) consumed = true;
        }

        //==================== PUBLIC API =====================
        /// <summary>Re-arm a consumed one-shot Activatable so it can fire again.
        /// Call from another script (e.g. UnityEvent wiring) at runtime.</summary>
        public void Rearm() => consumed = false;
    }
}

// ============================================================================
// Setup in a scene
//   1. On the prop the player should activate:
//      - Add a Collider on a layer included in the player's RaycastSensor mask.
//      - (Optional) Add a Focusable for the standard prompt UI.
//      - Add this Activatable component.
//   2. Wire the OnActivate UnityEvent in the inspector to whatever should
//      happen — Animator.SetTrigger, AudioSource.Play, a custom script's
//      method, etc.
//   3. Tick "oneShot" if the prop should fire only once (e.g. a key pickup
//      that disappears, a single-use switch). Leave unticked for buttons /
//      levers the player can spam. Call Rearm() from another script if you
//      need to re-arm a consumed one-shot at runtime.
// ============================================================================
