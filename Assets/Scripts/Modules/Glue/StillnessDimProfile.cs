using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for StillnessDimController — dims the environment when the player stops moving.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Stillness Dim Profile")]
    public class StillnessDimProfile : ScriptableObject
    {
        //==================== DETECTION =====================
        [Header("Detection")]
        [Tooltip("Speed below which the player counts as still (CharacterController velocity magnitude)")]
        [Min(0f)]
        public float stillnessThreshold = 0.1f;

        //==================== DIMMING (player stops) =====================
        [Header("Dimming — When Player Stops")]
        [Tooltip("Pause before dimming begins after the player stops (seconds)")]
        [Min(0f)]
        public float dimDelay = 0.5f;

        [Tooltip("How long the full dim takes (seconds)")]
        [Min(0.01f)]
        public float dimDuration = 3f;

        [Tooltip("Shape of the dimming (X = normalized time 0‥1, Y = normalized progress 0‥1)")]
        public AnimationCurve dimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== BRIGHTENING (player moves) =====================
        [Header("Brightening — When Player Moves")]
        [Tooltip("How long the lights take to come back (seconds)")]
        [Min(0.01f)]
        public float brightenDuration = 0.5f;

        [Tooltip("Shape of the brightening (X = normalized time 0‥1, Y = normalized progress 0‥1)")]
        public AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== CEILING EMISSION =====================
        [Header("Ceiling Emission")]
        [Tooltip("Emission intensity exponent when moving (lit) — applied as color * 2^value")]
        public float emissionLit = 5f;

        [Tooltip("Emission intensity exponent when still (dim) — applied as color * 2^value")]
        public float emissionDim = -8f;

        //==================== ENVIRONMENT LIGHTING =====================
        [Header("Environment Lighting")]
        [Tooltip("Ambient intensity multiplier when moving")]
        [Min(0f)]
        public float ambientLit = 1f;

        [Tooltip("Ambient intensity multiplier when still")]
        [Min(0f)]
        public float ambientDim = 0.05f;

        //==================== REFLECTIONS =====================
        [Header("Reflections")]
        [Tooltip("Reflection intensity multiplier when moving")]
        [Min(0f)]
        public float reflectionLit = 1f;

        [Tooltip("Reflection intensity multiplier when still")]
        [Min(0f)]
        public float reflectionDim = 0f;

        //==================== SKYBOX =====================
        [Header("Skybox Exposure (procedural)")]
        [Tooltip("Skybox _Exposure value when moving")]
        [Min(0f)]
        public float skyboxExposureLit = 1f;

        [Tooltip("Skybox _Exposure value when still")]
        [Min(0f)]
        public float skyboxExposureDim = 0.1f;

        //==================== DIRECTIONAL LIGHT =====================
        [Header("Directional Light")]
        [Tooltip("Directional light intensity when moving")]
        [Min(0f)]
        public float directionalLit = 1f;

        [Tooltip("Directional light intensity when still")]
        [Min(0f)]
        public float directionalDim = 0f;
    }
}
