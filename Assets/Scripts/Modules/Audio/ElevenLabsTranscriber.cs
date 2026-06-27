using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Ludocore
{
    /// <summary>
    /// Captures from a MicrophoneInput between BeginCapture/EndCaptureAndTranscribe calls,
    /// sends the recorded audio to ElevenLabs Speech-to-Text, and writes the result to a TMP text.
    /// All state is in-memory — no WAV file written, no transcription persisted.
    /// </summary>
    public class ElevenLabsTranscriber : MonoBehaviour
    {
        private const string Endpoint = "https://api.elevenlabs.io/v1/speech-to-text";

        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("MicrophoneInput to drive. This component calls Start/Stop on it.")]
        [SerializeField] private MicrophoneInput microphone;

        [Tooltip("Channel index to extract from the mic clip. 0 is fine for mono mics.")]
        [SerializeField, Range(0, 7)] private int channel;

        [Header("ElevenLabs")]
        [Tooltip("ElevenLabs API key. Leave empty to read from the ELEVENLABS_API_KEY environment variable instead. Avoid committing the key to source control.")]
        [SerializeField] private string apiKey = "";

        [Tooltip("ElevenLabs STT model. 'scribe_v1' is the general-purpose multilingual model.")]
        [SerializeField] private string modelId = "scribe_v1";

        [Tooltip("Optional ISO-639-3 language hint (e.g. 'eng', 'deu'). Leave empty for auto-detect.")]
        [SerializeField] private string languageCode = "";

        [Header("UI")]
        [Tooltip("TMP text to write the transcription into. Optional — also fires onTranscriptionComplete.")]
        [SerializeField] private TMP_Text output;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isCapturing;
        [ReadOnly, SerializeField] private bool isTranscribing;
        [ReadOnly, SerializeField] private int capturedSamples;
        [ReadOnly, SerializeField] private string lastTranscription;

        public bool IsCapturing => isCapturing;
        public bool IsTranscribing => isTranscribing;
        public string LastTranscription => lastTranscription;

        //==================== OUTPUTS =====================
        public event Action OnCaptureStarted;
        public event Action OnTranscriptionStarted;
        public event Action<string> OnTranscriptionComplete;
        public event Action<string> OnError;

        [Header("Events")]
        [SerializeField] private UnityEvent captureStartedEvent;
        [SerializeField] private UnityEvent transcriptionStartedEvent;
        [SerializeField] private UnityEvent<string> transcriptionCompleteEvent;
        [SerializeField] private UnityEvent<string> errorEvent;

        //==================== PRIVATE =====================
        private readonly List<float> samples = new List<float>(48000);
        private float[] chunkScratch;
        private int lastDrainPos;
        private int captureSampleRate;
        private Coroutine drainHandle;

        //==================== PUBLIC API =====================
        public void BeginCapture()
        {
            if (isCapturing) return;
            if (microphone == null) { Fail("MicrophoneInput is not assigned."); return; }

            samples.Clear();
            microphone.StartRecording();
            if (!microphone.IsRecording) { Fail("Microphone failed to start."); return; }

            captureSampleRate = microphone.SampleRate;
            lastDrainPos = Mathf.Max(0, microphone.CurrentSamplePosition);
            isCapturing = true;
            capturedSamples = 0;

            OnCaptureStarted?.Invoke();
            captureStartedEvent?.Invoke();

            drainHandle = StartCoroutine(DrainLoop());
        }

        public void EndCaptureAndTranscribe()
        {
            if (!isCapturing) return;

            if (drainHandle != null) { StopCoroutine(drainHandle); drainHandle = null; }

            // Final drain before the mic destroys its clip.
            DrainOnce();

            microphone.StopRecording();
            isCapturing = false;

            if (samples.Count == 0) { Fail("No audio captured."); return; }

            float[] mono = samples.ToArray();
            samples.Clear();
            StartCoroutine(TranscribeCoroutine(mono, captureSampleRate));
        }

        //==================== DRAIN =====================
        private IEnumerator DrainLoop()
        {
            while (isCapturing && microphone != null && microphone.IsRecording)
            {
                yield return null;
                DrainOnce();
            }
        }

        private void DrainOnce()
        {
            var clip = microphone != null ? microphone.Clip : null;
            if (clip == null) return;

            int pos = microphone.CurrentSamplePosition;
            if (pos < 0 || pos == lastDrainPos) return;

            int clipSamples = clip.samples;
            int channels = clip.channels;
            int target = channel >= 0 && channel < channels ? channel : 0;

            int newFrames = pos >= lastDrainPos
                ? pos - lastDrainPos
                : (clipSamples - lastDrainPos) + pos;

            // Possibly two reads if the new range wraps around the end of the circular clip.
            int firstFrames = Mathf.Min(newFrames, clipSamples - lastDrainPos);
            AppendChunk(clip, lastDrainPos, firstFrames, channels, target);
            int remaining = newFrames - firstFrames;
            if (remaining > 0) AppendChunk(clip, 0, remaining, channels, target);

            lastDrainPos = pos;
            capturedSamples = samples.Count;
        }

        private void AppendChunk(AudioClip clip, int startFrame, int frameCount, int channels, int targetChannel)
        {
            if (frameCount <= 0) return;
            int floatCount = frameCount * channels;
            if (chunkScratch == null || chunkScratch.Length != floatCount)
                chunkScratch = new float[floatCount];

            clip.GetData(chunkScratch, startFrame);

            for (int i = 0; i < frameCount; i++)
                samples.Add(chunkScratch[i * channels + targetChannel]);
        }

        //==================== TRANSCRIBE =====================
        private IEnumerator TranscribeCoroutine(float[] mono, int sampleRate)
        {
            string key = ResolveApiKey();
            if (string.IsNullOrEmpty(key)) { Fail("ElevenLabs API key not set (field empty and ELEVENLABS_API_KEY env var missing)."); yield break; }

            isTranscribing = true;
            OnTranscriptionStarted?.Invoke();
            transcriptionStartedEvent?.Invoke();

            byte[] wav = WavEncoder.EncodePcm16(mono, 1, sampleRate);

            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wav, "speech.wav", "audio/wav"),
                new MultipartFormDataSection("model_id", modelId),
            };
            if (!string.IsNullOrEmpty(languageCode))
                form.Add(new MultipartFormDataSection("language_code", languageCode));

            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] body = UnityWebRequest.SerializeFormSections(form, boundary);

            using var req = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(body) { contentType = "multipart/form-data; boundary=" + Encoding.UTF8.GetString(boundary) };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("xi-api-key", key);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            isTranscribing = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Fail($"HTTP {req.responseCode}: {req.error}\n{req.downloadHandler?.text}");
                yield break;
            }

            string text = "";
            try
            {
                var parsed = JsonUtility.FromJson<TranscriptionResponse>(req.downloadHandler.text);
                text = parsed != null ? parsed.text ?? "" : "";
            }
            catch (Exception e)
            {
                Fail($"Failed to parse response: {e.Message}\n{req.downloadHandler.text}");
                yield break;
            }

            lastTranscription = text;
            if (output != null) output.text = text;
            OnTranscriptionComplete?.Invoke(text);
            transcriptionCompleteEvent?.Invoke(text);
        }

        private string ResolveApiKey()
        {
            if (!string.IsNullOrEmpty(apiKey)) return apiKey;
            try { return Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY"); }
            catch { return null; }
        }

        private void Fail(string message)
        {
            Debug.LogWarning($"[{nameof(ElevenLabsTranscriber)}] {message}", this);
            OnError?.Invoke(message);
            errorEvent?.Invoke(message);
        }

        [Serializable]
        private class TranscriptionResponse
        {
            public string text;
            public string language_code;
            public float language_probability;
        }
    }
}
