using UnityEngine;

namespace Ludocore
{
    /// <summary>Sensor-driven teleport to a destination transform, hidden under
    /// a global light dim. The flow is Dim → wait → warp → Brighten, so the cut
    /// lands near peak darkness and reads as a short blackout.
    ///
    /// Lighting is delegated to the LightManager — this component never tweens
    /// lights itself, it only decides WHEN to switch state and WHEN to warp
    /// within that window.</summary>
    public class Teleport : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Sensor whose first-detection triggers the teleport (e.g. a TriggerSensor)")]
        [SerializeField] private Sensor sensor;

        [Tooltip("Destination transform to warp to (e.g. an empty GameObject placed in the scene).")]
        [SerializeField] private Transform destination;

        [Tooltip("CharacterController to warp. If empty, the first detected signal's transform is used.")]
        [SerializeField] private CharacterController player;

        [Tooltip("Light manager to drive. Falls back to LightManager.Instance if empty.")]
        [SerializeField] private LightManager lightManager;

        [Tooltip("Optional profile that overrides the LightManager's default for THIS teleport's blackout. " +
                 "Useful for a fast dim/brighten that doesn't disturb the slower global lighting tuning. " +
                 "Leave empty to use the LightManager's default profile.")]
        [SerializeField] private LightManagerProfile lightProfileOverride;

        //==================== TIMING =====================
        [Header("Timing")]
        [Tooltip("Seconds to wait after Dim() before performing the warp. Tune to land the warp near peak darkness.")]
        [Min(0f)]
        [SerializeField] private float darknessDelay = .5f;

        [Tooltip("Seconds to ignore further detections after a teleport completes.")]
        [Min(0f)]
        [SerializeField] private float reEnableCooldown = 0.5f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isTeleporting;
        [ReadOnly, SerializeField] private bool onCooldown;

        private bool _hadSignal;
        private float _cooldownUntil;

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

            var lm = Manager;
            if (lm) lm.Dim(lightProfileOverride);

            var target = ResolveTarget(nearest);
            Invoke(nameof(WarpAndBrighten), darknessDelay);

            _pendingTarget = target;
        }

        private Transform _pendingTarget;

        private void WarpAndBrighten()
        {
            if (_pendingTarget) Warp(_pendingTarget);
            _pendingTarget = null;

            var lm = Manager;
            if (lm) lm.Brighten(lightProfileOverride);

            _cooldownUntil = Time.time + reEnableCooldown;
            isTeleporting = false;
        }

        private void Warp(Transform t)
        {
            if (player && t == player.transform)
            {
                player.enabled = false;
                player.transform.SetPositionAndRotation(destination.position, destination.rotation);
                player.enabled = true;
                return;
            }

            t.SetPositionAndRotation(destination.position, destination.rotation);
        }

        private Transform ResolveTarget(Signal nearest)
        {
            if (player) return player.transform;
            return nearest.Object ? nearest.Object.transform : null;
        }

        private LightManager Manager => lightManager ? lightManager : LightManager.Instance;

        private void OnDisable()
        {
            CancelInvoke(nameof(WarpAndBrighten));
            isTeleporting = false;
            _pendingTarget = null;
        }
    }
}
