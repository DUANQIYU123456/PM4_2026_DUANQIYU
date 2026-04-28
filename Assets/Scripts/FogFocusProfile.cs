using UnityEngine;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Tuning profile for FogFocusController — drives scene fog density based on a Focusable's focus state.</summary>
    [CreateAssetMenu(menuName = "Ludocore/Fog Focus Profile")]
    public class FogFocusProfile : ScriptableObject
    {
        //==================== FOG =====================
        [Header("Fog")]
        [Tooltip("Density when the object is NOT focused")]
        [Min(0f)]
        public float unfocusedDensity = 0.01f;

        [Tooltip("Density when the object IS focused")]
        [Min(0f)]
        public float focusedDensity = 0.15f;

        //==================== TWEEN =====================
        [Header("Tween")]
        [Tooltip("Seconds to ramp density up on focus")]
        [Min(0f)]
        public float focusDuration = 1.5f;

        [Tooltip("Seconds to ramp density down on unfocus")]
        [Min(0f)]
        public float unfocusDuration = 1.5f;

        [Tooltip("Easing for both transitions")]
        public Ease ease = Ease.InOutSine;
    }
}
