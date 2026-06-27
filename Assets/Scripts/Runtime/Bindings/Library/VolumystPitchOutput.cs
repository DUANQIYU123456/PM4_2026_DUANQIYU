using UnityEngine;
using Volumyst.Namespace;

namespace Ludocore
{
    /// <summary>Drives VolumystAudioSystem.pitch. POCO card analog of VolumystPitchBinding. Native range [-4, 4].</summary>
    [BindMenu("Audio/Volumyst Pitch")]
    [System.Serializable]
    public class VolumystPitchOutput : BindTarget
    {
        [Tooltip("VolumystAudioSystem to drive. Pitch is applied every frame, so no toggle is needed.")]
        [SerializeField] private VolumystAudioSystem target;

        private float _original;

        public VolumystPitchOutput() : base(1f, 2f) { }

        public override string DisplayName => "Volumyst Pitch";
        public override Color CardColor => new Color(0.78f, 0.6f, 0.92f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (target) _original = target.pitch; }
        protected override void OnRestore() { if (target) target.pitch = _original; }

        protected override void OnApply(float value)
        {
            if (target) target.pitch = value;
        }
    }
}
