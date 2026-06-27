using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Sensor-driven teleport with two positioning modes and an optional
    /// light blackout.
    ///
    /// Positioning (Mode):
    ///   • Absolute — snap the subject to a fixed Destination transform.
    ///   • Relative — re-emit the subject through EntryAnchor → Destination, so it
    ///     keeps the SAME offset/orientation relative to the exit doorway that it
    ///     had relative to the entry doorway. This is the seamless portal warp and
    ///     uses the exact transform PortalRenderer previews.
    ///
    /// Blackout (Use Blackout):
    ///   • On  — Dim → wait darknessDelay → warp → Brighten, so the cut lands near
    ///     peak darkness and reads as a short blackout (disorienting; good for a
    ///     backrooms "blink and you're elsewhere" jump).
    ///   • Off — warp instantly on detection (pair with Relative for a truly
    ///     seamless walk-through with no fade).
    ///
    /// Lighting is delegated to the LightManager — this component never tweens
    /// lights itself, it only decides WHEN to switch state and WHEN to warp
    /// within that window.</summary>
    public class Teleport : MonoBehaviour
    {
        public enum WarpMode
        {
            Absolute, // snap to a fixed Destination transform
            Relative, // re-emit the subject through EntryAnchor -> Destination
        }

        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Sensor whose first-detection triggers the teleport (e.g. a TriggerSensor)")]
        [SerializeField] private Sensor sensor;

        [Tooltip("Destination frame. Absolute mode: the subject snaps here. " +
                 "Relative mode: this is the EXIT portal (PortalOut) the subject is re-emitted through.")]
        [SerializeField] private Transform destination;

        [Tooltip("CharacterController to warp. If empty, the first detected signal's transform is used.")]
        [SerializeField] private CharacterController player;

        [Tooltip("Light manager to drive. Falls back to LightManager.Instance if empty.")]
        [SerializeField] private LightManager lightManager;

        [Tooltip("Optional counter incremented by 1 each time a teleport completes.")]
        [SerializeField] private IntVariable teleportCount;

        [Tooltip("Optional profile that overrides the LightManager's default for THIS teleport's blackout. " +
                 "Useful for a fast dim/brighten that doesn't disturb the slower global lighting tuning. " +
                 "Leave empty to use the LightManager's default profile.")]
        [SerializeField] private LightManagerProfile lightProfileOverride;

        //==================== MODE =====================
        [Header("Mode")]
        [Tooltip("Absolute = snap to Destination. Relative = re-emit through Entry Anchor -> Destination " +
                 "for a seamless portal that matches PortalRenderer's preview.")]
        [SerializeField] private WarpMode mode = WarpMode.Absolute;

        [Tooltip("Relative mode only: the ENTRY portal frame (PortalIn). The subject's offset/orientation " +
                 "relative to this is preserved when re-emitted through Destination. Falls back to this transform.")]
        [SerializeField] private Transform entryAnchor;

        [Tooltip("On = hide the warp behind a Dim -> wait -> Brighten blackout (disorienting cut). " +
                 "Off = warp instantly on detection (pair with Relative for a seamless walk-through).")]
        [SerializeField] private bool useBlackout = true;

        //==================== TIMING =====================
        [Header("Timing")]
        [Tooltip("Blackout only: seconds to wait after Dim() before performing the warp. Tune to land the warp near peak darkness.")]
        [Min(0f)]
        [SerializeField] private float darknessDelay = .5f;

        [Tooltip("Seconds to ignore further detections after a teleport completes.")]
        [Min(0f)]
        [SerializeField] private float reEnableCooldown = 0.5f;

        //==================== EVENTS =====================
        [Header("Events")]
        [Tooltip("Inspector-wireable. Fires when a teleport completes (after the subject has been warped).")]
        [SerializeField] private UnityEvent onTeleported;

        /// <summary>Code-wireable counterpart of onTeleported. Fires when a teleport completes.</summary>
        public event System.Action Teleported;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isTeleporting;
        [ReadOnly, SerializeField] private bool onCooldown;

        private bool _hadSignal;
        private float _cooldownUntil;
        private Transform _pendingTarget;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (!sensor) return;

            bool hasSignal = sensor.TryGetNearest(out var nearest);

            onCooldown = Time.time < _cooldownUntil;

            if (hasSignal && !_hadSignal && !isTeleporting && !onCooldown)
                TriggerTeleport(nearest);

            _hadSignal = hasSignal;
        }

        //==================== PRIVATE =====================
        private void TriggerTeleport(Signal nearest)
        {
            if (!destination) return;

            isTeleporting = true;
            _pendingTarget = ResolveTarget(nearest);

            if (useBlackout)
            {
                var lm = Manager;
                if (lm) lm.Dim(lightProfileOverride);
                Invoke(nameof(WarpAndFinish), darknessDelay);
            }
            else
            {
                // Seamless: warp the instant the subject crosses the portal plane.
                WarpAndFinish();
            }
        }

        private void WarpAndFinish()
        {
            if (_pendingTarget) Warp(_pendingTarget);
            _pendingTarget = null;

            if (useBlackout)
            {
                var lm = Manager;
                if (lm) lm.Brighten(lightProfileOverride);
            }

            _cooldownUntil = Time.time + reEnableCooldown;
            isTeleporting = false;

            if (teleportCount) teleportCount.Increment();
            onTeleported?.Invoke();
            Teleported?.Invoke();
        }

        private void Warp(Transform t)
        {
            GetTargetPose(t, out var pos, out var rot);

            // A CharacterController fights direct transform writes while enabled,
            // so toggle it around the move (same path the FirstPersonController needs).
            if (player && t == player.transform)
            {
                player.enabled = false;
                player.transform.SetPositionAndRotation(pos, rot);
                player.enabled = true;
                return;
            }

            t.SetPositionAndRotation(pos, rot);
        }

        /// <summary>Resolve the world pose the subject should end up at, per mode.</summary>
        private void GetTargetPose(Transform subject, out Vector3 pos, out Quaternion rot)
        {
            if (mode == WarpMode.Relative)
            {
                var inAnchor = entryAnchor ? entryAnchor : transform;

                // Express the subject in the entry frame, then re-emit through the
                // exit frame: it keeps the same offset/orientation relative to
                // Destination that it had relative to EntryAnchor — i.e. the exact
                // transform PortalRenderer uses to build the preview.
                Matrix4x4 m = destination.localToWorldMatrix
                              * inAnchor.worldToLocalMatrix
                              * subject.localToWorldMatrix;

                pos = m.GetColumn(3);
                rot = m.rotation;
                return;
            }

            // Absolute: snap to the Destination transform.
            pos = destination.position;
            rot = destination.rotation;
        }

        private Transform ResolveTarget(Signal nearest)
        {
            if (player) return player.transform;
            return nearest.Object ? nearest.Object.transform : null;
        }

        private LightManager Manager => lightManager ? lightManager : LightManager.Instance;

        private void OnDisable()
        {
            CancelInvoke(nameof(WarpAndFinish));
            isTeleporting = false;
            _pendingTarget = null;
        }
    }
}
