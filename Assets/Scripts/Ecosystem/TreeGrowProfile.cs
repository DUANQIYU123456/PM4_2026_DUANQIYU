using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for TreeGrower — uniform scale-up plus bark and leaves shader animation.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Tree Grow Profile")]
    public class TreeGrowProfile : ScriptableObject
    {
        //==================== TIMING =====================
        [Header("Timing")]
        [Tooltip("How long the full grow takes (seconds)")]
        [Min(0.01f)]
        public float growDuration = 3f;

        [Tooltip("Shape of the grow ramp (X = normalized time 0‥1, Y = normalized progress 0‥1)")]
        public AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== SCALE =====================
        [Header("Scale")]
        [Tooltip("Uniform scale at the start of the grow")]
        [Min(0f)]
        public float startScale = 0f;

        [Tooltip("Uniform scale at the end of the grow")]
        [Min(0f)]
        public float endScale = 1f;

        //==================== BARK (material 0) =====================
        [Header("Bark — Idyllic Fantasy Nature/Surface")]
        [Tooltip("_Base_Map_Scale at the start of the grow")]
        public float barkScaleStart = 0f;

        [Tooltip("_Base_Map_Scale at the end of the grow")]
        public float barkScaleEnd = 5f;

        //==================== LEAVES (material 1) =====================
        [Header("Leaves — Idyllic Fantasy Nature/Vegetation")]
        [Tooltip("_Blend_Height at the start of the grow")]
        public float leavesBlendHeightStart = 0.1f;

        [Tooltip("_Blend_Height at the end of the grow")]
        public float leavesBlendHeightEnd = 0.75f;

        [Tooltip("_Alpha_Cutoff at the start of the grow (1 = leaves invisible)")]
        public float leavesAlphaCutoffStart = 1f;

        [Tooltip("_Alpha_Cutoff at the end of the grow")]
        public float leavesAlphaCutoffEnd = 0.2f;
    }
}
