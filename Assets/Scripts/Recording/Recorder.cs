using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Abstract base for time recorders. Owns the sampling cadence, the elapsed clock,
    /// and a max-duration safety cap. Subclasses say WHAT to sample by overriding
    /// OnBeginRecording / RecordFrame / OnEndRecording.
    ///
    /// A recorder writes into a RecordingClip asset; one or more Players can later read that
    /// clip back. Sampling happens at a fixed rate (default 30 Hz) — sample timestamps are
    /// implicit from their index in the clip.</summary>
    public abstract class Recorder : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Sampling")]
        [Tooltip("Samples per second. 30 Hz is plenty for transforms.")]
        [Min(1f)]
        [SerializeField] protected float sampleRate = 30f;

        [Tooltip("Hard cap on recording length in seconds. Stops automatically when reached. " +
                 "Set high if you want recordings driven by manual Stop only.")]
        [Min(0.1f)]
        [SerializeField] protected float maxDuration = 30f;

        [Tooltip("Begin recording automatically when this component is enabled.")]
        [SerializeField] protected bool startOnEnable;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] protected bool isRecording;
        [ReadOnly, SerializeField] protected float elapsed;
        [ReadOnly, SerializeField] protected int sampleCount;

        protected float nextSampleElapsed;

        public bool IsRecording => isRecording;
        public float Elapsed => elapsed;
        public int SampleCount => sampleCount;
        public float SampleInterval => sampleRate > 0f ? 1f / sampleRate : 0f;
        public float Progress => maxDuration > 0f ? Mathf.Clamp01(elapsed / maxDuration) : 0f;

        //==================== LIFECYCLE =====================
        protected virtual void OnEnable()
        {
            if (startOnEnable) StartRecording();
        }

        protected virtual void OnDisable()
        {
            if (isRecording) StopRecording();
        }

        protected virtual void Update()
        {
            if (!isRecording) return;

            float interval = SampleInterval;
            if (interval <= 0f) return; // guard: a zero interval would spin the catch-up loop forever

            elapsed += Time.deltaTime;

            // Catch-up loop in case a frame was long enough to span multiple sample boundaries.
            while (elapsed >= nextSampleElapsed)
            {
                RecordFrame();
                sampleCount++;
                OnSampled?.Invoke(nextSampleElapsed);
                sampledEvent?.Invoke(nextSampleElapsed);
                nextSampleElapsed += interval;
            }

            if (elapsed >= maxDuration) StopRecording();
        }

        //==================== INPUTS =====================
        /// <summary>Begin a fresh recording. Resolves targets, resets the clip, and captures t=0.</summary>
        [ContextMenu("Start Recording")]
        public void StartRecording()
        {
            if (isRecording) return;

            if (!OnBeginRecording())
            {
                Debug.LogWarning($"{name}: {GetType().Name} cannot start — subclass refused.", this);
                return;
            }

            isRecording = true;
            elapsed = 0f;
            sampleCount = 0;
            nextSampleElapsed = 0f;

            // Capture the initial state at t = 0 immediately so the clip starts from the
            // pre-event configuration (e.g. the unbroken sphere).
            RecordFrame();
            sampleCount++;
            OnSampled?.Invoke(0f);
            sampledEvent?.Invoke(0f);
            nextSampleElapsed = SampleInterval;

            OnStarted?.Invoke();
            startedEvent?.Invoke();
        }

        /// <summary>Stop recording and finalize the clip's duration.</summary>
        [ContextMenu("Stop Recording")]
        public void StopRecording()
        {
            if (!isRecording) return;

            isRecording = false;
            OnEndRecording(elapsed);

            OnStopped?.Invoke(elapsed);
            stoppedEvent?.Invoke(elapsed);
        }

        //==================== PRIVATE =====================
        /// <summary>Subclass hook called at the start of StartRecording.
        /// Resolve targets, call clip.BeginRecording, and return true if ready. Return false to abort.</summary>
        protected abstract bool OnBeginRecording();

        /// <summary>Capture one frame's worth of data into the clip.</summary>
        protected abstract void RecordFrame();

        /// <summary>Subclass hook called at the end of StopRecording. Finalize the clip here.</summary>
        protected abstract void OnEndRecording(float finalDuration);

        //==================== OUTPUTS =====================
        public event Action OnStarted;
        public event Action<float> OnSampled;
        public event Action<float> OnStopped;

        [Header("Events")]
        [Tooltip("Fired once when recording begins.")]
        [SerializeField] private UnityEvent startedEvent;

        [Tooltip("Fired each time a sample is captured. Passes the elapsed time of that sample.")]
        [SerializeField] private UnityEvent<float> sampledEvent;

        [Tooltip("Fired once when recording stops (manually or by hitting maxDuration). Passes total duration.")]
        [SerializeField] private UnityEvent<float> stoppedEvent;
    }
}
