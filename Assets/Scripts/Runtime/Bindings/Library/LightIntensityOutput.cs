using UnityEngine;

namespace Ludocore
{
    /// <summary>Drives a Light's intensity. POCO card analog of LightIntensityBinding.</summary>
    [BindMenu("Light/Intensity")]
    [System.Serializable]
    public class LightIntensityOutput : BindTarget
    {
        [Tooltip("Light to drive.")]
        [SerializeField] private Light targetLight;

        private float _original;

        public LightIntensityOutput() : base(0f, 2f) { }

        public override string DisplayName => "Light Intensity";
        public override Color CardColor => new Color(0.95f, 0.85f, 0.45f);
        public override bool AltersState => true;

        protected override void OnCapture() { if (targetLight) _original = targetLight.intensity; }
        protected override void OnRestore() { if (targetLight) targetLight.intensity = _original; }

        protected override void OnApply(float value)
        {
            if (targetLight) targetLight.intensity = value;
        }
    }
}
