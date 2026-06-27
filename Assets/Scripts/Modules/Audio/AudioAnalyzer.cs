using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Continuously samples audio output and exposes a smoothed, normalized 0..1 intensity value.</summary>
    public class AudioAnalyzer : MonoBehaviour
    {
        public enum SourceMode { AudioListener, AudioSources, Microphone }

        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("Where to sample audio from: the global AudioListener, one or more AudioSources, or a MicrophoneInput.")]
        [SerializeField] private SourceMode mode = SourceMode.AudioListener;

        [Tooltip("AudioSources to analyze when mode is AudioSources. Combined energy is summed across all playing sources.")]
        [SerializeField] private AudioSource[] sources;

        [Tooltip("MicrophoneInput to analyze when mode is Microphone.")]
        [SerializeField] private MicrophoneInput microphone;

        [Header("Sampling")]
        [Tooltip("Samples per frame. Higher = smoother and slightly more CPU. Must be power of 2.")]
        [SerializeField, Range(64, 8192)] private int sampleWindow = 1024;

        [Tooltip("Which channel to read (0 = left / mono)")]
        [SerializeField, Range(0, 7)] private int channel;

        [Header("Normalization")]
        [Tooltip("Multiplies raw RMS before clamping to 0..1. Higher = more sensitive to quiet audio.")]
        [SerializeField, Min(0f)] private float sensitivity = 4f;

        [Tooltip("Seconds it takes intensity to follow rising audio. Lower = snappier reactions.")]
        [SerializeField, Min(0f)] private float attackTime = 0.02f;

        [Tooltip("Seconds it takes intensity to settle back toward silence. Higher = lingers longer.")]
        [SerializeField, Min(0f)] private float decayTime = 0.15f;

        [Header("Threshold")]
        [Tooltip("Intensity01 crossing above this fires the Exceeded outputs.")]
        [SerializeField, Range(0f, 1f)] private float threshold = 0.6f;

        [Tooltip("Intensity must fall back below this before Exceeded can fire again (hysteresis). Keep ≤ threshold.")]
        [SerializeField, Range(0f, 1f)] private float rearmThreshold = 0.4f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private float rawRms;
        [ReadOnly, SerializeField] private float intensity01;
        [ReadOnly, SerializeField] private bool above;

        /// <summary>Unsmoothed RMS amplitude of the most recent buffer (typically 0..~0.5 for normal audio).</summary>
        public float RawRms => rawRms;

        /// <summary>Smoothed, normalized intensity in 0..1. Primary output for reactors to bind against.</summary>
        public float Intensity01 => intensity01;

        /// <summary>True while intensity is above threshold (with hysteresis re-arm).</summary>
        public bool IsAboveThreshold => above;

        //==================== OUTPUTS =====================
        public event Action<float> OnIntensity;
        public event Action OnThresholdExceeded;
        public event Action OnThresholdReleased;

        [Header("SO Channels (optional)")]
        [Tooltip("Written every frame with the current Intensity01. Lets SO-driven reactors observe intensity without referencing this component.")]
        [SerializeField] private FloatVariable intensityVariable;

        [Tooltip("Raised when intensity crosses above the threshold.")]
        [SerializeField] private GameEvent thresholdExceededChannel;

        [Tooltip("Raised when intensity falls back below the rearm threshold.")]
        [SerializeField] private GameEvent thresholdReleasedChannel;

        [Header("Events")]
        [Tooltip("Invoked every frame with the current 0..1 intensity. Wire to material params, scale curves, etc.")]
        [SerializeField] private UnityEvent<float> intensityEvent;

        [Tooltip("Invoked once when intensity crosses above the threshold.")]
        [SerializeField] private UnityEvent thresholdExceededEvent;

        [Tooltip("Invoked once when intensity falls back below the rearm threshold.")]
        [SerializeField] private UnityEvent thresholdReleasedEvent;

        //==================== LIFECYCLE =====================
        private void Update()
        {
            if (buffer == null || buffer.Length != sampleWindow)
                buffer = new float[sampleWindow];

            float rms = mode switch
            {
                SourceMode.AudioListener => SampleListener(),
                SourceMode.AudioSources  => SampleSources(),
                SourceMode.Microphone    => SampleMicrophone(),
                _ => 0f,
            };

            rawRms = rms;

            float target = Mathf.Clamp01(rms * sensitivity);
            float tau = target > intensity01 ? attackTime : decayTime;
            float k = tau <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / tau);
            intensity01 = Mathf.Lerp(intensity01, target, k);

            PublishIntensity();
            EvaluateThreshold();
        }

        //==================== PRIVATE =====================
        private float[] buffer;

        private void PublishIntensity()
        {
            OnIntensity?.Invoke(intensity01);
            intensityEvent?.Invoke(intensity01);
            if (intensityVariable != null) intensityVariable.Value = intensity01;
        }

        private void EvaluateThreshold()
        {
            if (!above && intensity01 >= threshold)
            {
                above = true;
                OnThresholdExceeded?.Invoke();
                thresholdExceededEvent?.Invoke();
                if (thresholdExceededChannel != null) thresholdExceededChannel.Raise();
            }
            else if (above && intensity01 <= rearmThreshold)
            {
                above = false;
                OnThresholdReleased?.Invoke();
                thresholdReleasedEvent?.Invoke();
                if (thresholdReleasedChannel != null) thresholdReleasedChannel.Raise();
            }
        }

        private float SampleListener()
        {
            AudioListener.GetOutputData(buffer, channel);
            return Rms(buffer);
        }

        private float SampleSources()
        {
            if (sources == null || sources.Length == 0) return 0f;

            float sumSq = 0f;
            int counted = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null || !src.isPlaying) continue;
                src.GetOutputData(buffer, channel);
                float r = Rms(buffer);
                sumSq += r * r;
                counted++;
            }
            return counted == 0 ? 0f : Mathf.Sqrt(sumSq);
        }

        private float SampleMicrophone()
        {
            if (microphone == null) return 0f;
            int count = microphone.GetLatestSamples(buffer, channel);
            return count == 0 ? 0f : Rms(buffer);
        }

        private static float Rms(float[] buf)
        {
            float sum = 0f;
            for (int i = 0; i < buf.Length; i++) sum += buf[i] * buf[i];
            return Mathf.Sqrt(sum / buf.Length);
        }
    }
}
