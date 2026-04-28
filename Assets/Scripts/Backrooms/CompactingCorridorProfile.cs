using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for CompactingCorridorController — wall compaction driven by proximity.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Compacting Corridor Profile")]
    public class CompactingCorridorProfile : ScriptableObject
    {
        //==================== INPUT =====================
        [Header("Input — Proximity")]
        [Tooltip("Maximum detection distance — proximity is normalized against this value")]
        [Min(0.01f)]
        public float maxDistance = 5f;

        //==================== WALL COMPACTION =====================
        [Header("Wall Compaction — Bound Interaction")]
        [Tooltip("Proximity→compaction remapping curve (X = 0 far‥1 close, Y = 0 rest‥1 full)")]
        public AnimationCurve compactionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Maximum inward offset from the wall rest position (units)")]
        [Min(0f)]
        public float maxCompaction = 1f;

        [Tooltip("How fast walls close when proximity increases (units/sec, 0 = instant)")]
        [Min(0f)]
        public float attackSpeed;

        [Tooltip("How fast walls return to rest when proximity decreases (units/sec, 0 = instant)")]
        [Min(0f)]
        public float releaseSpeed;

        //==================== WALL TEXTURE SCROLL =====================
        [Header("Wall Texture Scroll — Bound Interaction")]
        [Tooltip("Proximity→scroll speed remapping curve (X = 0 far‥1 close, Y = 0‥1)")]
        public AnimationCurve scrollCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Texture offset Y speed at full proximity (units/sec)")]
        [Min(0f)]
        public float maxScrollSpeed = 1f;

        //==================== POST-PROCESSING =====================
        [Header("Post-Processing — Bound Interaction")]
        [Tooltip("Proximity→vignette remapping curve (X = 0 far‥1 close, Y = 0‥1)")]
        public AnimationCurve vignetteCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Vignette intensity at full proximity (0‥1)")]
        [Range(0f, 1f)]
        public float maxVignetteIntensity = 0.4f;

        [Tooltip("Proximity→desaturation remapping curve (X = 0 far‥1 close, Y = 0‥1)")]
        public AnimationCurve desaturationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Color saturation reduction at full proximity (0‥1, where 1 = fully desaturated)")]
        [Range(0f, 1f)]
        public float maxDesaturation = 0.5f;
    }
}
