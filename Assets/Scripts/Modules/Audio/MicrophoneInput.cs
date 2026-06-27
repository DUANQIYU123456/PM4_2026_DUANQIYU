using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>
    /// Captures audio from a microphone device into a looping AudioClip.
    /// Consumers (AudioAnalyzer, transcription services, etc.) read samples via Clip or GetLatestSamples.
    /// Leave deviceName empty to use the system default mic on Windows and macOS.
    /// </summary>
    public class MicrophoneInput : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Device")]
        [Tooltip("Microphone device name. Leave empty to use the system default. Check Microphone.devices at runtime for available names.")]
        [SerializeField] private string deviceName = "";

        [Tooltip("Sample rate in Hz. 16000 is fine for speech transcription; 44100/48000 for music analysis.")]
        [SerializeField] private int sampleRate = 44100;

        [Tooltip("Length of the rolling capture buffer in seconds. Longer = more history available to consumers.")]
        [SerializeField, Range(1, 30)] private int bufferLengthSec = 1;

        [Header("Lifecycle")]
        [Tooltip("Start recording automatically when this component enables.")]
        [SerializeField] private bool startOnEnable = true;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private string resolvedDevice;
        [ReadOnly, SerializeField] private bool isRecording;
        [ReadOnly, SerializeField] private int currentPosition;

        private AudioClip clip;
        private float[] scratchInterleaved;

        // Unity's Microphone API treats null as "default device" but doesn't reliably accept empty string. Always re-resolve.
        private string DeviceForApi => string.IsNullOrEmpty(resolvedDevice) ? null : resolvedDevice;

        /// <summary>The looping AudioClip the microphone is writing into. Null until recording starts.</summary>
        public AudioClip Clip => clip;

        /// <summary>True while the microphone is actively capturing.</summary>
        public bool IsRecording => isRecording;

        /// <summary>The actual device name being recorded from ("" means system default was used).</summary>
        public string ResolvedDeviceName => resolvedDevice;

        /// <summary>Sample rate the clip was opened at.</summary>
        public int SampleRate => clip != null ? clip.frequency : sampleRate;

        /// <summary>Channel count of the captured clip (typically 1 for mics).</summary>
        public int Channels => clip != null ? clip.channels : 1;

        /// <summary>Current write position in the clip, in samples. -1 if not recording.</summary>
        public int CurrentSamplePosition => isRecording ? Microphone.GetPosition(DeviceForApi) : -1;

        //==================== OUTPUTS =====================
        public event Action OnStarted;
        public event Action OnStopped;

        [Header("Events")]
        [SerializeField] private UnityEvent startedEvent;
        [SerializeField] private UnityEvent stoppedEvent;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (startOnEnable) StartRecording();
        }

        private void OnDisable()
        {
            StopRecording();
        }

        private void Update()
        {
            if (isRecording) currentPosition = Microphone.GetPosition(DeviceForApi);
        }

        //==================== PUBLIC API =====================
        public void StartRecording()
        {
            if (isRecording) return;

            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning($"[{nameof(MicrophoneInput)}] No microphone devices found. Check OS privacy settings (Windows: Settings > Privacy > Microphone; macOS: System Settings > Privacy & Security > Microphone).", this);
                return;
            }

            resolvedDevice = string.IsNullOrEmpty(deviceName) ? null : deviceName;
            clip = Microphone.Start(resolvedDevice, true, bufferLengthSec, sampleRate);

            if (clip == null)
            {
                Debug.LogWarning($"[{nameof(MicrophoneInput)}] Microphone.Start returned null for device '{resolvedDevice ?? "<default>"}'.", this);
                return;
            }

            isRecording = true;
            if (resolvedDevice == null) resolvedDevice = ""; // for inspector readability
            OnStarted?.Invoke();
            startedEvent?.Invoke();
        }

        public void StopRecording()
        {
            if (!isRecording) return;

            var dev = DeviceForApi;
            if (Microphone.IsRecording(dev)) Microphone.End(dev);

            if (clip != null) Destroy(clip);
            clip = null;
            isRecording = false;
            currentPosition = 0;

            OnStopped?.Invoke();
            stoppedEvent?.Invoke();
        }

        /// <summary>
        /// Fills <paramref name="buffer"/> with the most recent samples from the chosen channel.
        /// Returns the number of samples written, or 0 if recording hasn't produced data yet.
        /// Handles wrap-around of the circular capture clip.
        /// </summary>
        public int GetLatestSamples(float[] buffer, int channel)
        {
            if (buffer == null || buffer.Length == 0) return 0;
            if (clip == null || !isRecording) return 0;

            int pos = Microphone.GetPosition(DeviceForApi);
            if (pos <= 0) return 0;

            int clipSamples = clip.samples;
            int channels = clip.channels;
            int needed = Mathf.Min(buffer.Length, clipSamples);
            if (channel < 0 || channel >= channels) channel = 0;

            int start = pos - needed;
            if (start < 0) start += clipSamples;

            int sliceFloats = needed * channels;
            if (scratchInterleaved == null || scratchInterleaved.Length != sliceFloats)
                scratchInterleaved = new float[sliceFloats];

            // Microphone-created clips are circular: GetData with an offset wraps around end-of-clip.
            clip.GetData(scratchInterleaved, start);

            for (int i = 0; i < needed; i++)
                buffer[i] = scratchInterleaved[i * channels + channel];

            return needed;
        }
    }
}
