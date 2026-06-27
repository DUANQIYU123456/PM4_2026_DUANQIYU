using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for ProximityMover — how an object moves and rotates as a sensed target
    /// approaches. Swap profiles on a single floating-house prefab to repose its behaviour (rise, sink,
    /// slide, tumble, latch point) without touching the scene wiring.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Proximity Mover Profile")]
    public class ProximityMoverProfile : ScriptableObject
    {
        //==================== MOTION =====================
        [Header("Motion")]
        [Tooltip("Position offset reached at full proximity. +Y rises, -Y sinks, X/Z slide. Flip the sign to reverse.")]
        public Vector3 moveOffset = new Vector3(0f, 3f, 0f);

        [Tooltip("Rotation offset (euler degrees) reached at full proximity. Tilts/tumbles as it moves.")]
        public Vector3 rotationOffset = Vector3.zero;

        [Tooltip("Apply the offsets in local space (recommended; respects a parent rig).")]
        public bool useLocalSpace = true;

        //==================== INPUT =====================
        [Header("Input — Proximity")]
        [Tooltip("Distance at which proximity reads 0 (far). 0 = use the sensor's radius.")]
        [Min(0f)]
        public float maxDistance;

        [Tooltip("Maps raw proximity (0 far‥1 close) to movement progress (0..1).")]
        public AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        //==================== SMOOTHING =====================
        [Header("Smoothing")]
        [Tooltip("Progress gained per second while approaching (rising). 0 = snap.")]
        [Min(0f)]
        public float riseSpeed = 1f;

        [Tooltip("Progress lost per second while leaving (falling back). 0 = snap.")]
        [Min(0f)]
        public float fallSpeed = 1f;

        //==================== THRESHOLD =====================
        [Header("Threshold")]
        [Tooltip("Progress at which the Entered event fires (and Exited below it).")]
        [Range(0f, 1f)]
        public float threshold = 0.5f;
    }
}
