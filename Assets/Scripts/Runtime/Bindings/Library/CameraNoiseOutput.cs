using Unity.Cinemachine;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Drives a Cinemachine Perlin noise component's AmplitudeGain (and proportional FrequencyGain).
    /// POCO card analog of CameraNoiseBinding. Assign a rotation-dominant NoiseProfile for camera shake.
    /// </summary>
    [BindMenu("Camera/Noise")]
    [System.Serializable]
    public class CameraNoiseOutput : BindTarget
    {
        [Tooltip("Perlin noise component to drive (sits on your CinemachineCamera).")]
        [SerializeField] private CinemachineBasicMultiChannelPerlin noise;
        [Tooltip("FrequencyGain = output value × this scale. 0 = drive amplitude only, leaving FrequencyGain as authored.")]
        [Min(0f)]
        [SerializeField] private float frequencyScale = 1f;

        private float _origAmplitude;
        private float _origFrequency;

        public CameraNoiseOutput() : base(0f, 1f) { }

        public override string DisplayName => "Camera Noise";
        public override Color CardColor => new Color(0.5f, 0.7f, 0.95f);
        public override bool AltersState => true;

        protected override void OnCapture()
        {
            if (!noise) return;
            _origAmplitude = noise.AmplitudeGain;
            _origFrequency = noise.FrequencyGain;
        }

        protected override void OnRestore()
        {
            if (!noise) return;
            noise.AmplitudeGain = _origAmplitude;
            // Only revert FrequencyGain if we actually drove it (amplitude-only mode never touched it).
            if (frequencyScale > 0f) noise.FrequencyGain = _origFrequency;
        }

        protected override void OnApply(float value)
        {
            if (!noise) return;
            noise.AmplitudeGain = value;
            if (frequencyScale > 0f) noise.FrequencyGain = value * frequencyScale;
        }
    }
}
