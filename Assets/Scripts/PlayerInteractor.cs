// ============================================================================
// PlayerInteractor — Glue / input layer
//
// One script per player (or camera). Each frame it reads the forward raycast
// sensor, tracks which focusable it is looking at, and on the interact
// key-down dispatches BeginInteract on whatever IInteractable lives on the
// focused object.
//
// Talks to two small contracts and nothing else:
//   IFocusable     — drives show/hide of prompts on the currently focused object
//   IInteractable  — fires the verb on key-down (verb owns its own state)
//
// One key from the player's POV. Many verbs under the hood — the interactor
// just brokers interfaces with no knowledge of pickups, doors, switches, or UI.
// ============================================================================

using UnityEngine;

namespace Ludocore
{
    /// <summary>Raycast-based interactor. Drives IFocusable transitions and
    /// dispatches IInteractable.BeginInteract on the interact key-down.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Raycast sensor that looks forward — usually placed on the camera.")]
        [SerializeField] private RaycastSensor raycastSensor;

        [Tooltip("Key pressed to interact with the currently focused object.")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private GameObject currentTarget;

        private IFocusable _currentFocus;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!raycastSensor) return;

            UpdateFocus();
            HandleInteract();
        }

        private void OnDisable()
        {
            _currentFocus?.SetFocused(false);
            _currentFocus = null;
            currentTarget = null;
        }

        //==================== PRIVATE =====================
        private void UpdateFocus()
        {
            IFocusable nextFocus = null;
            GameObject nextTarget = null;

            if (raycastSensor.TryGetNearest(out Signal signal) && signal.Object)
            {
                nextTarget = signal.Object;
                nextTarget.TryGetComponent(out nextFocus);
            }

            // Clear stale reference if the previous focus was destroyed.
            if (_currentFocus is Object prev && !prev) _currentFocus = null;

            if (ReferenceEquals(nextFocus, _currentFocus)) return;

            _currentFocus?.SetFocused(false);
            nextFocus?.SetFocused(true);

            _currentFocus = nextFocus;
            currentTarget = nextTarget;
        }

        private void HandleInteract()
        {
            if (!Input.GetKeyDown(interactKey)) return;
            if (!currentTarget) return;
            if (!currentTarget.TryGetComponent(out IInteractable interactable)) return;
            if (!interactable.CanInteract) return;

            interactable.BeginInteract();
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. On the player camera: add a RaycastSensor. Set its distance (e.g. 3m),
//      layer mask (which layers can be interacted with), and optional sphere
//      cast radius for forgiving aim.
//   2. Add this PlayerInteractor on the same GameObject. Wire the sensor
//      reference and pick the interact key (default: E).
//   3. For any object that should respond to the player's gaze:
//      - Add a Focusable component (fires focus events)
//      - Add an IInteractable implementation: Grabbable, Activatable, or
//        Toggleable.
//      - Make sure its collider is on a layer included in the sensor's mask.
// ============================================================================
