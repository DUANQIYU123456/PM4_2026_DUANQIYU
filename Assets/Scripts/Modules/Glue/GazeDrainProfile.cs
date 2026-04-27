using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for GazeDrainController — desaturates and vignettes while the player stares at an object.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Gaze Drain Profile")]
    public class GazeDrainProfile : ScriptableObject
    {
        //==================== ONSET (player starts looking) =====================
        [Header("Onset — While Gazing")]
        [Tooltip("Pause before the drain begins after gaze is detected (seconds)")]
        [Min(0f)]
        public float onsetDelay;

        [Tooltip("How long the full drain takes (seconds)")]
        [Min(0.01f)]
        public float onsetDuration = 3f;

        [Tooltip("Shape of the drain ramp (X = normalized time 0‥1, Y = normalized progress 0‥1)")]
        public AnimationCurve onsetCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== RELEASE (player looks away) =====================
        [Header("Release — When Gaze Breaks")]
        [Tooltip("How long recovery takes when the player looks away (seconds)")]
        [Min(0.01f)]
        public float releaseDuration = 1f;

        [Tooltip("Shape of the recovery (X = normalized time 0‥1, Y = normalized progress 0‥1)")]
        public AnimationCurve releaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== SATURATION =====================
        [Header("Saturation (Color Adjustments)")]
        [Tooltip("Saturation when not gazing (0 = no adjustment)")]
        [Range(-100f, 100f)]
        public float saturationNormal;

        [Tooltip("Saturation at full drain (-100 = monochrome)")]
        [Range(-100f, 100f)]
        public float saturationGazed = -100f;

        //==================== VIGNETTE =====================
        [Header("Vignette")]
        [Tooltip("Vignette intensity when not gazing")]
        [Range(0f, 1f)]
        public float vignetteNormal = 0.15f;

        [Tooltip("Vignette intensity at full drain (tunnel vision)")]
        [Range(0f, 1f)]
        public float vignetteGazed = 0.55f;
    }
}
