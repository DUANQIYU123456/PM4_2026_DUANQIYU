using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ludocore
{
    /// <summary>Drives a URP White Balance override's temperature on a Volume (cool ↔ warm). Native range [-100, 100].</summary>
    [BindMenu("Post FX/White Balance Temperature")]
    [System.Serializable]
    public class WhiteBalanceTemperatureOutput : PostFxFloatOutput
    {
        public WhiteBalanceTemperatureOutput() : base(-50f, 50f) { }

        public override string DisplayName => "White Balance Temp";

        protected override VolumeParameter<float> Resolve(VolumeProfile profile)
            => profile.TryGet(out WhiteBalance w) ? w.temperature : null;
    }
}
