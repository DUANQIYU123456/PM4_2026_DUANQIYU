using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>Drives a URP Bloom override's intensity on a Volume. Native range [0, ∞), no upper clamp.</summary>
    [BindMenu("Post FX/Bloom Intensity")]
    [System.Serializable]
    public class BloomIntensityOutput : PostFxFloatOutput
    {
        public BloomIntensityOutput() : base(0f, 5f) { }

        public override string DisplayName => "Bloom Intensity";

        protected override VolumeParameter<float> Resolve(VolumeProfile profile)
            => profile.TryGet(out Bloom b) ? b.intensity : null;
    }
}
