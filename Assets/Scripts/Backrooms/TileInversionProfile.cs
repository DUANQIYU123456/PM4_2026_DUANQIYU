using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for TileInversionController — random-timer and rotation settings.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Tile Inversion Profile")]
    public class TileInversionProfile : ScriptableObject
    {
        //==================== TIMER =====================
        [Header("Timer")]
        [Tooltip("Master enable for the random-interval auto-invert")]
        public bool useTimer = true;

        [Tooltip("Lower bound of random wait between inversions (sec)")]
        [Min(0f)]
        public float minInterval = 5f;

        [Tooltip("Upper bound of random wait between inversions (sec)")]
        [Min(0f)]
        public float maxInterval = 15f;

        //==================== ROTATION =====================
        [Header("Rotation")]
        [Tooltip("Angle applied per Invert() call (degrees)")]
        public float rotationAmount = 180f;

        [Tooltip("True = DoTween animated rotation, false = instant snap")]
        public bool animate = true;

        [Tooltip("Tween duration when animate = true (sec)")]
        [Min(0f)]
        public float duration = 1f;

        [Tooltip("Tween easing when animate = true")]
        public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}
