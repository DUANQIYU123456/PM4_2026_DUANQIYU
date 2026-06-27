using UnityEngine;
using Volumyst.Namespace;

namespace Ludocore
{
    /// <summary>Drives VolumystAudioSystem.phaserFrequency. POCO card analog of VolumystPhaserFrequencyBinding. Native range [0.1, 10].</summary>
    [BindMenu("Audio/Volumyst Phaser Freq")]
    [System.Serializable]
    public class VolumystPhaserFrequencyOutput : BindTarget
    {
        [Tooltip("VolumystAudioSystem to drive. Native phaserFrequency range [0.1, 10]. The phaser effect is " +
                 "force-enabled (and restored) so the bound value is audible.")]
        [SerializeField] private VolumystAudioSystem target;

        private float _original;
        private bool _origActive;

        public VolumystPhaserFrequencyOutput() : base(0.1f, 10f) { }

        public override string DisplayName => "Volumyst Phaser Freq";
        public override Color CardColor => new Color(0.78f, 0.6f, 0.92f);
        public override bool AltersState => true;

        protected override void OnCapture()
        {
            if (!target) return;
            _origActive = target.isPhaserActive;
            _original = target.phaserFrequency;
            target.isPhaserActive = true; // force the effect on so the bound value is audible
        }

        protected override void OnRestore()
        {
            if (!target) return;
            target.phaserFrequency = _original;
            target.isPhaserActive = _origActive;
        }

        protected override void OnApply(float value)
        {
            if (target) target.phaserFrequency = value;
        }
    }
}
