// ============================================================================
// PlayerInteractor — Glue / input layer
//
// One script per player (or camera). Each frame it reads the forward raycast
// sensor, tracks which focusable it is looking at, and dispatches the
// interact key through the IInteractable contract — Press for single-tap
// verbs, Hold for held-progress verbs.
//
// Talks to two small contracts and nothing else:
//   IFocusable     — drives show/hide of prompts on the currently focused object
//   IInteractable  — dispatches the verb (Press = key-down completes;
//                    Hold = key-down begins, key-up or target-change cancels)
//
// One key from the player's POV. Many verbs under the hood — the interactor
// asks the target what it can do and dispatches accordingly. No knowledge of
// trees, resources, buildings, or UI. It just brokers interfaces.
// ============================================================================

using UnityEngine;

namespace Ludocore
{
    /// <summary>Raycast-based interactor. Drives IFocusable transitions and
    /// dispatches IInteractable Press / Hold verbs on the interact key.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Raycast sensor that looks forward — usually placed on the camera.")]
        [SerializeField] private RaycastSensor raycastSensor;

        [Tooltip("Key pressed (or held) to interact with the currently focused object.")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private GameObject currentTarget;

        private IFocusable _currentFocus;
        private IInteractable _currentHold;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!raycastSensor) return;

            UpdateFocus();
            HandleInteract();
        }

        private void OnDisable()
        {
            // Cancel any in-flight hold and drop focus cleanly.
            _currentHold?.CancelInteract();
            _currentHold = null;

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
            // Resolve the currently focused IInteractable, if any.
            IInteractable interactable = null;
            if (currentTarget) currentTarget.TryGetComponent(out interactable);

            // Drop a stale hold reference if the underlying object was destroyed.
            if (_currentHold is Object held && !held) _currentHold = null;

            // Cancel an in-flight hold if the player released, looked away, or it's no longer interactable.
            if (_currentHold != null)
            {
                bool sameTarget = ReferenceEquals(_currentHold, interactable);
                bool keyHeld = Input.GetKey(interactKey);
                if (!keyHeld || !sameTarget || !_currentHold.CanInteract)
                {
                    _currentHold.CancelInteract();
                    _currentHold = null;
                }
            }

            // Begin a new interaction only on a fresh key-down + valid target.
            if (interactable == null || !interactable.CanInteract) return;
            if (!Input.GetKeyDown(interactKey)) return;

            interactable.BeginInteract();
            if (interactable.Mode == InteractionMode.Hold) _currentHold = interactable;
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
//      - Add an IInteractable implementation: Harvestable (Hold mode) or
//        BuildingSite (Press mode), or your own future verb.
//      - Make sure its collider is on a layer included in the sensor's mask.
// ============================================================================
