using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>Drives a URP Color Adjustments override's hue shift on a Volume. Native range [-180, 180] degrees.</summary>
    [BindMenu("Post FX/Hue Shift")]
    [System.Serializable]
    public class ColorHueShiftOutput : PostFxFloatOutput
    {
        public ColorHueShiftOutput() : base(-180f, 180f) { }

        public override string DisplayName => "Hue Shift";

        protected override VolumeParameter<float> Resolve(VolumeProfile profile)
            => profile.TryGet(out ColorAdjustments c) ? c.hueShift : null;
    }
}
