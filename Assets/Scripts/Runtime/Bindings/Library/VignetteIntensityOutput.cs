using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>Drives a URP Vignette override's intensity on a Volume. Native range [0, 1].</summary>
    [BindMenu("Post FX/Vignette Intensity")]
    [System.Serializable]
    public class VignetteIntensityOutput : PostFxFloatOutput
    {
        public VignetteIntensityOutput() : base(0f, 0.5f) { }

        public override string DisplayName => "Vignette Intensity";

        protected override VolumeParameter<float> Resolve(VolumeProfile profile)
            => profile.TryGet(out Vignette v) ? v.intensity : null;
    }
}
