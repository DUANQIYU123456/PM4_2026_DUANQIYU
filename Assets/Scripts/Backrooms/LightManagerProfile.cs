using UnityEngine;

namespace Ludocore
{
    /// <summary>Tuning profile for LightManager — defines the values for the Lit and Dim states
    /// across every channel (ceiling emission, ambient, reflections, skybox exposure, directional
    /// light) and the timing of transitions between them. Paired with LightManager, which owns
    /// the state and transitions.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Light Manager Profile")]
    public class LightManagerProfile : ScriptableObject
    {
        //==================== DIM TRANSITION =====================
        [Header("Dim Transition (Lit → Dim)")]
        [Tooltip("Pause before the transition begins (seconds)")]
        [Min(0f)]
        public float dimDelay = 0.5f;

        [Tooltip("How long the transition takes (seconds)")]
        [Min(0.01f)]
        public float dimDuration = 3f;

        [Tooltip("Shape of the transition (X = normalized time 0‥1, Y = normalized progress 0‥1)")]
        public AnimationCurve dimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== BRIGHTEN TRANSITION =====================
        [Header("Brighten Transition (Dim → Lit)")]
        [Tooltip("How long the transition takes (seconds)")]
        [Min(0.01f)]
        public float brightenDuration = 0.5f;

        [Tooltip("Shape of the transition")]
        public AnimationCurve brightenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        //==================== CEILING EMISSION =====================
        [Header("Ceiling Emission (exponent — applied as color * 2^value)")]
        public float emissionLit = 5f;
        public float emissionDim = -8f;

        //==================== AMBIENT =====================
        [Header("Ambient Intensity")]
        [Min(0f)] public float ambientLit = 1f;
        [Min(0f)] public float ambientDim = 0.05f;

        //==================== REFLECTIONS =====================
        [Header("Reflections")]
        [Min(0f)] public float reflectionLit = 1f;
        [Min(0f)] public float reflectionDim = 0f;

        //==================== SKYBOX =====================
        [Header("Skybox Exposure (procedural skybox _Exposure)")]
        [Min(0f)] public float skyboxExposureLit = 1f;
        [Min(0f)] public float skyboxExposureDim = 0.1f;

        //==================== DIRECTIONAL LIGHT =====================
        [Header("Directional Light Intensity")]
        [Min(0f)] public float directionalLit = 1f;
        [Min(0f)] public float directionalDim = 0f;
    }
}
