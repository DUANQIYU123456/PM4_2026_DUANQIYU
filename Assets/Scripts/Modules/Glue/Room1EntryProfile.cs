using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for Room1EntryControllerV2 — four unbound interactions triggered on entry.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Room 1 Entry Profile")]
    public class Room1EntryProfile : ScriptableObject
    {
        //==================== PITCH =====================
        [Header("Pitch — Response Interaction")]
        [Tooltip("Time→pitch curve (X = normalized time 0‥1, Y = normalized output 0‥1)")]
        public AnimationCurve pitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("How long the pitch ramp plays (seconds)")]
        [Min(0f)]
        public float pitchDuration = 1f;

        [Tooltip("Pause before the pitch ramp starts (seconds)")]
        [Min(0f)]
        public float pitchDelay;

        //==================== AMBIENT LIGHTING =====================
        [Header("Ambient Lighting — Response Interaction")]
        [Tooltip("Time→ambient intensity curve (X = normalized time 0‥1, Y = normalized output 0‥1)")]
        public AnimationCurve ambientCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("How long the ambient intensity ramp plays (seconds)")]
        [Min(0f)]
        public float ambientDuration = 1f;

        [Tooltip("Pause before the ambient intensity ramp starts (seconds)")]
        [Min(0f)]
        public float ambientDelay;

        //==================== REFLECTIONS =====================
        [Header("Reflections — Response Interaction")]
        [Tooltip("Time→reflection intensity curve (X = normalized time 0‥1, Y = normalized output 0‥1)")]
        public AnimationCurve reflectionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("How long the reflection intensity ramp plays (seconds)")]
        [Min(0f)]
        public float reflectionDuration = 1f;

        [Tooltip("Pause before the reflection intensity ramp starts (seconds)")]
        [Min(0f)]
        public float reflectionDelay;

        //==================== CEILING EMISSION =====================
        [Header("Ceiling Emission — Response Interaction")]
        [Tooltip("Time→emission intensity curve (X = normalized time 0‥1, Y = normalized output 0‥1)")]
        public AnimationCurve emissionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Base emission color before intensity is applied")]
        public Color emissionColor = Color.white;

        [Tooltip("How long the emission intensity ramp plays (seconds)")]
        [Min(0f)]
        public float emissionDuration = 1f;

        [Tooltip("Pause before the emission intensity ramp starts (seconds)")]
        [Min(0f)]
        public float emissionDelay;
    }
}
