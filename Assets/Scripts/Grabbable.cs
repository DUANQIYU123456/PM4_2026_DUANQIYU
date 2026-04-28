// ============================================================================
// Grabbable — Logic / verb layer (IInteractable, Hold mode)
//
// Physics-driven "carry this thing" verb. While the player holds the interact
// key, the rigidbody is velocity-driven toward the active GrabAnchor (a
// transform parented under the camera). Releasing the key applies an optional
// forward impulse along the anchor's facing — release force is per-object, so
// a feather flutters and a brick chucks.
//
// Drop on any prop that should be carryable. Requires:
//   - Rigidbody (must NOT be kinematic)
//   - Collider on a layer included in the player's RaycastSensor mask
//   - Exactly one GrabAnchor active in the scene
//
// Composes cleanly with Focusable for the standard prompt-UI flow, but does
// not require it. Implemented as Hold mode so PlayerInteractor's existing
// dispatch handles release-on-key-up and release-on-look-away for free.
// ============================================================================

using System;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Carry-via-physics IInteractable. Hold mode: rigidbody is driven toward
    /// GrabAnchor.Current while the interact key is held; released on key-up with an
    /// optional forward impulse.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour, IInteractable
    {
        //==================== CONFIG =====================
        [Header("Carry")]
        [Tooltip("How aggressively the prop accelerates toward the grab anchor. " +
                 "Higher = stiffer follow, lower = floatier.")]
        [SerializeField] private float followStiffness = 18f;

        [Tooltip("Per-fixed-step damping on the carry velocity (0..1). " +
                 "Higher = less wobble, slower response.")]
        [Range(0f, 1f)]
        [SerializeField] private float followDamping = 0.85f;

        [Tooltip("How aggressively the prop rotates to match the anchor's rotation.")]
        [SerializeField] private float rotationStiffness = 25f;

        [Tooltip("If the prop ends up further than this from the anchor (e.g. clipped " +
                 "through a wall, snagged on geometry), auto-release.")]
        [SerializeField] private float breakDistance = 2.5f;

        [Header("Release")]
        [Tooltip("Forward impulse applied along the grab anchor's forward on release. " +
                 "0 = simple drop. Tune per object — light props ~3, heavy ~8+.")]
        [SerializeField] private float throwForce = 5f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isHeld;

        private Rigidbody _rb;
        private GrabAnchor _anchor;
        private bool _cachedUseGravity;
        private float _cachedLinearDamping;
        private float _cachedAngularDamping;

        //==================== IInteractable =====================
        public InteractionMode Mode => InteractionMode.Hold;
        public float Duration => 0f; // grab is instantaneous; carry is the "verb"
        public bool CanInteract => _rb && !_rb.isKinematic && !isHeld;
        public event Action<float> OnProgress;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void OnDisable()
        {
            // If we get yanked offline mid-carry, drop cleanly without a throw.
            if (isHeld) ReleaseInternal(applyThrow: false);
        }

        private void FixedUpdate()
        {
            if (!isHeld || !_anchor) return;

            // --- Auto-release if the prop has been pushed too far (wall clip, snag).
            Vector3 toTarget = _anchor.transform.position - _rb.worldCenterOfMass;
            if (toTarget.sqrMagnitude > breakDistance * breakDistance)
            {
                ReleaseInternal(applyThrow: false);
                return;
            }

            // --- Position drive: damped velocity toward the anchor.
            Vector3 desiredVelocity = toTarget * followStiffness;
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, 1f - followDamping);

            // --- Rotation drive: shortest-path angular velocity to match anchor.rotation.
            Quaternion delta = _anchor.transform.rotation * Quaternion.Inverse(_rb.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (Mathf.Abs(angle) > 0.01f && !float.IsInfinity(axis.x))
            {
                _rb.angularVelocity = axis.normalized * (angle * Mathf.Deg2Rad * rotationStiffness);
            }
        }

        //==================== IInteractable IMPLEMENTATION =====================
        public void BeginInteract()
        {
            if (!CanInteract) return;

            _anchor = GrabAnchor.Current;
            if (!_anchor)
            {
                Debug.LogWarning($"Grabbable: no active GrabAnchor in scene; cannot grab '{name}'.", this);
                return;
            }

            // Cache rigidbody state so release can restore it exactly.
            _cachedUseGravity = _rb.useGravity;
            _cachedLinearDamping = _rb.linearDamping;
            _cachedAngularDamping = _rb.angularDamping;

            _rb.useGravity = false;
            // Heavier damping while carried smooths out collisions with walls.
            _rb.linearDamping = 5f;
            _rb.angularDamping = 5f;

            isHeld = true;
            OnProgress?.Invoke(1f);
        }

        public void CancelInteract()
        {
            if (!isHeld) return;
            ReleaseInternal(applyThrow: throwForce > 0f);
        }

        //==================== PRIVATE =====================
        private void ReleaseInternal(bool applyThrow)
        {
            if (!isHeld) return;

            _rb.useGravity = _cachedUseGravity;
            _rb.linearDamping = _cachedLinearDamping;
            _rb.angularDamping = _cachedAngularDamping;

            if (applyThrow && _anchor)
            {
                _rb.AddForce(_anchor.transform.forward * throwForce, ForceMode.VelocityChange);
            }

            _anchor = null;
            isHeld = false;
            OnProgress?.Invoke(0f);
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. One-time per scene: under the player camera, add a child GameObject
//      with a GrabAnchor component (see GrabAnchor.cs for setup).
//   2. On any carryable prop:
//      - Add a Rigidbody (non-kinematic, mass to taste).
//      - Make sure its Collider is on a layer included in the player's
//        RaycastSensor mask (so PlayerInteractor can focus it).
//      - Add this Grabbable component.
//      - (Optional) Add a Focusable for the standard prompt UI.
//   3. Tune per object:
//      - followStiffness / followDamping: feel of the carry. Heavier props
//        usually want lower stiffness so they lag behind the anchor.
//      - throwForce: 0 = drop, 3 = light toss, 8+ = chuck.
//      - breakDistance: lower for fragile / pin-precise, higher for big props.
//   4. Default control flow (no PlayerInteractor changes needed):
//      - Look at prop, hold E to lift.
//      - Move / look around to carry. Anchor rotation drives prop rotation.
//      - Release E to throw forward (or drop if throwForce = 0).
//      - Looking away from the prop also releases — for the carry, the prop
//        sits in front of the camera so the raycast keeps focusing it.
// ============================================================================
