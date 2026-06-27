using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives a Unity AudioDistortionFilter's distortionLevel. POCO card analog of AudioDistortionBinding. Native range [0, 1].</summary>
    [BindMenu("Audio/Distortion")]
    [System.Serializable]
    public class AudioDistortionOutput : BindTarget
    {
        [Tooltip("AudioDistortionFilter to drive.")]
        [SerializeField] private AudioDistortionFilter target;

        private float _original;

        public AudioDistortionOutput() : base(0f, 1f) { }

        public override string DisplayName => "Audio Distortion";
        public override Color CardColor => new Color(0.78f, 0.6f, 0.92f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (target) _original = target.distortionLevel; }
        protected override void OnRestore() { if (target) target.distortionLevel = _original; }

        protected override void OnApply(float value)
        {
            if (target) target.distortionLevel = value;
        }
    }
}
