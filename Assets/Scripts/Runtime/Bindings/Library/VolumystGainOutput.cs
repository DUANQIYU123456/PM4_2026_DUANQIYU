using UnityEngine;
using Volumyst.Namespace;

namespace Ludocore
{
    /// <summary>Drives VolumystAudioSystem.gainFactor. POCO card analog of VolumystGainBinding. Native range [0.1, 10].</summary>
    [BindMenu("Audio/Volumyst Gain")]
    [System.Serializable]
    public class VolumystGainOutput : BindTarget
    {
        [Tooltip("VolumystAudioSystem to drive. Native gainFactor range [0.1, 10]. The gain effect is force-enabled " +
                 "(and restored) so the bound value is audible.")]
        [SerializeField] private VolumystAudioSystem target;

        private float _original;
        private bool _origActive;

        public VolumystGainOutput() : base(1f, 4f) { }

        public override string DisplayName => "Volumyst Gain";
        public override Color CardColor => new Color(0.78f, 0.6f, 0.92f);
        public override bool AltersState => true;

        protected override void OnCapture()
        {
            if (!target) return;
            _origActive = target.isGainActive;
            _original = target.gainFactor;
            target.isGainActive = true; // force the effect on so the bound value is audible
        }

        protected override void OnRestore()
        {
            if (!target) return;
            target.gainFactor = _original;
            target.isGainActive = _origActive;
        }

        protected override void OnApply(float value)
        {
            if (target) target.gainFactor = value;
        }
    }
}
