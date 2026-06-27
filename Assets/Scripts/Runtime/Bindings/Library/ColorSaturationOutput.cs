using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>Drives a URP Color Adjustments override's saturation on a Volume. Native range [-100, 100] (-100 = grayscale).</summary>
    [BindMenu("Post FX/Saturation")]
    [System.Serializable]
    public class ColorSaturationOutput : PostFxFloatOutput
    {
        public ColorSaturationOutput() : base(-100f, 100f) { }

        public override string DisplayName => "Saturation";

        protected override VolumeParameter<float> Resolve(VolumeProfile profile)
            => profile.TryGet(out ColorAdjustments c) ? c.saturation : null;
    }
}
