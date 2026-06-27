using UnityEngine;
using UnityEngine.Rendering;

namespace Ludocore
{
    /// <summary>
    /// Shared base for BindTarget cards that drive a single float VolumeParameter on a URP post-processing
    /// Volume — bloom intensity, white balance temperature, saturation, vignette intensity, etc. Resolves the
    /// override from the Volume's RUNTIME profile (<see cref="Volume.profile"/>, an instanced copy) so it never
    /// edits the profile ASSET, force-enables the override so the knob is visible, and captures/restores the
    /// original value and override state on play-exit.
    ///
    /// Subclasses only override <see cref="Resolve"/> to pick the parameter from the profile, plus the usual
    /// <see cref="DisplayName"/> and a sensible default output range via their constructor.
    /// </summary>
    [System.Serializable]
    public abstract class PostFxFloatOutput : BindTarget
    {
        [Tooltip("Volume whose runtime profile to drive. The matching override must already exist on the profile.")]
        [SerializeField] protected Volume volume;

        private VolumeParameter<float> _param;
        private float _original;
        private bool _origOverride;

        protected PostFxFloatOutput(float outMin, float outMax) : base(outMin, outMax) { }

        public override Color CardColor => new Color(0.9f, 0.6f, 0.85f);
        public override bool AltersState => true;

        /// <summary>Pick the float parameter to drive from the runtime profile, or null if its override is missing.</summary>
        protected abstract VolumeParameter<float> Resolve(VolumeProfile profile);

        protected override void OnCapture()
        {
            if (volume && volume.profile) _param = Resolve(volume.profile);
            if (_param == null) return;
            _original = _param.value;
            _origOverride = _param.overrideState;
            _param.overrideState = true;
        }

        protected override void OnRestore()
        {
            if (_param == null) return;
            _param.value = _original;
            _param.overrideState = _origOverride;
        }

        protected override void OnApply(float value)
        {
            if (_param != null) _param.value = value;
        }
    }
}
