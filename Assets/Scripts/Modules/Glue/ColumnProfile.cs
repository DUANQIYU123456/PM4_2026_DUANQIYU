using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for ColumnControllerV2 — emission bound interaction driven by proximity.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Column Profile")]
    public class ColumnProfile : ScriptableObject
    {
        //==================== INPUT =====================
        [Header("Input — Proximity")]
        [Tooltip("Maximum detection distance — input is normalized against this value")]
        [Min(0.01f)]
        public float maxDistance = 5f;

        //==================== EMISSION =====================
        [Header("Emission — Bound Interaction")]
        [Tooltip("Proximity→emission remapping curve (X = 0 far‥1 close, Y = 0‥1 normalized output)")]
        public AnimationCurve emissionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Base emission color")]
        [ColorUsage(false, true)]
        public Color emissionColor = Color.white;

        [Tooltip("Emission intensity when nothing is detected or at max distance")]
        [Min(0f)]
        public float minIntensity;

        [Tooltip("Emission intensity when the player is right on top")]
        [Min(0f)]
        public float maxIntensity = 2f;

        [Tooltip("How fast emission catches up when proximity increases (units/sec, 0 = instant)")]
        [Min(0f)]
        public float emissionAttackSpeed;

        [Tooltip("How fast emission returns to rest when proximity decreases (units/sec, 0 = instant)")]
        [Min(0f)]
        public float emissionReleaseSpeed;
    }
}
