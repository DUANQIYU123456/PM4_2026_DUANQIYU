using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for TerrainBreathe — round-trip vertical stretch
    /// (inhale to peak, exhale back to baseline) via an animated multiplier
    /// on TerrainData.size.y.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Terrain Breathe Profile")]
    public class TerrainBreatheProfile : ScriptableObject
    {
        //==================== TWEEN =====================
        [Header("Tween")]
        [Tooltip("Total inhale + exhale time (seconds)")]
        [Min(0.01f)]
        public float duration = 2f;

        [Tooltip("Shape of the inhale (X = normalised inhale time 0..1, Y = phase 0..1). " +
                 "Output 0 = baseline; output 1 = peak. The exhale is the mirror of this curve, " +
                 "applied in the second half of the tween.")]
        public AnimationCurve inhaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== BREATHE =====================
        [Header("Breathe")]
        [Tooltip("Multiplier on baseline size.y at the top of the inhale. " +
                 "1.2 = +20% taller; 0.8 = -20% shorter (deflate); 1.0 = no motion.")]
        [Min(0.01f)]
        public float peakHeightMultiplier = 1.2f;
    }
}
