using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Why a Player released scene control. Passed to OnStop so a subclass can react
    /// differently to a natural finish vs. an explicit Stop vs. being disabled — e.g. keep a
    /// recomposed object held in place only when the clip finished on its own.</summary>
    public enum PlaybackStopReason
    {
        Completed, // reached the end (or start, when reversing) without looping
        Stopped,   // Stop() was called explicitly
        Disabled   // the component was disabled or destroyed
    }

    /// <summary>Abstract base for clip playback. Owns the timeline cursor (currentTime),
    /// play/pause/stop, speed, looping, and normalized scrubbing. Subclasses say WHAT to drive
    /// by overriding ApplyAt(t).
    ///
    /// Reverse playback is first-class: PlayReverse() runs the clip from end to start — the core
    /// "recompose a shattered object / rewind an event" effect — and fires OnReachedStart when it
    /// lands on t=0. PlayForward() is the mirror.
    ///
    /// Progress is exposed in [0,1] to plug into UI sliders, FloatVariables, or other normalized inputs.</summary>
    public abstract class Player : MonoBehaviour, IScrubbable
    {
        //==================== CONFIG =====================
        [Header("Playback")]
        [Tooltip("Signed playback speed. 1 = real time, 2 = double speed, -1 = reverse. " +
                 "PlayForward()/PlayReverse() set the direction for you, preserving magnitude.")]
        [SerializeField] protected float speed = 1f;

        [Tooltip("Restart at the other end when playback runs off the timeline.")]
        [SerializeField] protected bool loop;

        [Tooltip("Begin playing automatically when this component is enabled.")]
        [SerializeField] protected bool playOnEnable;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] protected bool isPlaying;
        [ReadOnly, SerializeField] protected float currentTime;

        protected bool _resolved;
        private bool _active; // scene control acquired: OnPlay has run and OnStop is pending

        public bool IsPlaying => isPlaying;
        public float CurrentTime => currentTime;
        public float Speed { get => speed; set => speed = value; }
        public float Duration => GetClipDuration();
        public float Progress => Duration > 0f ? Mathf.Clamp01(currentTime / Duration) : 0f;

        //==================== LIFECYCLE =====================
        protected virtual void OnEnable()
        {
            if (playOnEnable) Play();
        }

        protected virtual void OnDisable()
        {
            // Release scene control even if we were only Paused (isPlaying == false, _active == true).
            if (_active) StopInternal(PlaybackStopReason.Disabled);
        }

        protected virtual void Update()
        {
            if (!isPlaying) return;
            float dur = Duration;
            if (dur <= 0f) return;

            currentTime += Time.deltaTime * speed;

            bool ended = false;

            if (currentTime > dur)
            {
                if (loop) currentTime -= dur;
                else { currentTime = dur; ended = true; }
            }
            else if (currentTime < 0f)
            {
                if (loop) currentTime += dur;
                else { currentTime = 0f; ended = true; }
            }

            ApplyAt(currentTime);
            OnProgress?.Invoke(Progress);
            progressEvent?.Invoke(Progress);

            if (ended) StopInternal(PlaybackStopReason.Completed);
        }

        //==================== INPUTS =====================
        /// <summary>Start (or resume) playback in the current speed direction. If the cursor is parked
        /// at the end the speed is heading away from, rewinds to the other end so Play just runs again.</summary>
        [ContextMenu("Play")]
        public void Play()
        {
            float dur = Duration;
            if (dur <= 0f) return;
            if (!EnsureResolved()) return;
            if (isPlaying) return;

            if (!_active)
            {
                // Fresh start (not a resume from Pause). Auto-rewind if the cursor is parked at
                // the end the speed is heading away from, then acquire scene control once.
                if (speed >= 0f && currentTime >= dur) currentTime = 0f;
                else if (speed < 0f && currentTime <= 0f) currentTime = dur;

                OnPlay();
                _active = true;
            }

            isPlaying = true;
            OnStarted?.Invoke();
            startedEvent?.Invoke();
        }

        /// <summary>Play forwards (toward the clip end) at the current speed magnitude.</summary>
        [ContextMenu("Play Forward")]
        public void PlayForward()
        {
            speed = SpeedMagnitude();
            Play();
        }

        /// <summary>Play in reverse (toward the clip start) at the current speed magnitude — the
        /// recompose / rewind effect. Finishing at t=0 fires OnReachedStart (and OnCompleted).</summary>
        [ContextMenu("Play Reverse")]
        public void PlayReverse()
        {
            speed = -SpeedMagnitude();
            Play();
        }

        /// <summary>Pause playback. Cursor stays where it is.</summary>
        [ContextMenu("Pause")]
        public void Pause()
        {
            isPlaying = false;
        }

        /// <summary>Stop playback and release scene control (e.g. hand rigidbodies back to physics
        /// where they currently are), then reset the cursor to 0. Does NOT snap the scene back to the
        /// t=0 pose — call ScrubToStart() before Stop() if you want the visual reset too.</summary>
        [ContextMenu("Stop")]
        public void Stop()
        {
            StopInternal(PlaybackStopReason.Stopped);
            currentTime = 0f;
        }

        /// <summary>Take the target off any competing live driver (e.g. set Rigidbodies kinematic) so it
        /// can be scrubbed in isolation, WITHOUT starting time-based playback. No-op when nothing
        /// competes (no Rigidbody). Idempotent. Pairs 1:1 with Release.</summary>
        [ContextMenu("Engage")]
        public void Engage()
        {
            if (Duration <= 0f) return;
            if (!EnsureResolved()) return;
            if (_active) return;

            OnPlay();
            _active = true;
        }

        /// <summary>Hand the target back to its live driver (e.g. restore dynamic physics). Pairs with
        /// Engage. Safe to call when not engaged.</summary>
        [ContextMenu("Release")]
        public void Release()
        {
            if (!_active) return;
            OnStop(PlaybackStopReason.Stopped);
            _active = false;
        }

        /// <summary>Jump to a normalized position [0,1] without changing play/pause state.</summary>
        public void Scrub(float normalized)
        {
            if (Duration <= 0f) return;
            if (!EnsureResolved()) return;

            currentTime = Mathf.Clamp01(normalized) * Duration;
            ApplyAt(currentTime);
            OnProgress?.Invoke(Progress);
            progressEvent?.Invoke(Progress);
        }

        /// <summary>Jump to an absolute time in seconds without changing play/pause state.</summary>
        public void SetTime(float seconds)
        {
            if (Duration <= 0f) return;
            if (!EnsureResolved()) return;

            currentTime = Mathf.Clamp(seconds, 0f, Duration);
            ApplyAt(currentTime);
            OnProgress?.Invoke(Progress);
            progressEvent?.Invoke(Progress);
        }

        /// <summary>Convenience: snap to the start (the initial pre-event state).</summary>
        [ContextMenu("Scrub To Start")]
        public void ScrubToStart() => Scrub(0f);

        /// <summary>Convenience: snap to the end of the clip.</summary>
        [ContextMenu("Scrub To End")]
        public void ScrubToEnd() => Scrub(1f);

        //==================== PRIVATE =====================
        /// <summary>Current speed magnitude, defaulting to 1 if speed is ~0.</summary>
        private float SpeedMagnitude()
        {
            float m = Mathf.Abs(speed);
            return m < 0.0001f ? 1f : m;
        }

        private void StopInternal(PlaybackStopReason reason)
        {
            isPlaying = false;

            // Release scene control exactly once per acquisition (pairs 1:1 with OnPlay),
            // so a Pause→Play→Stop cycle can't re-capture or double-restore physics state.
            if (_active)
            {
                OnStop(reason);
                _active = false;
            }

            if (reason == PlaybackStopReason.Completed)
            {
                OnCompleted?.Invoke();
                completedEvent?.Invoke();

                // Distinguish which end we finished at. Reverse finishing at the start is the
                // "recompose done" moment; forward finishing at the end is "replay done".
                if (speed >= 0f)
                {
                    OnReachedEnd?.Invoke();
                    reachedEndEvent?.Invoke();
                }
                else
                {
                    OnReachedStart?.Invoke();
                    reachedStartEvent?.Invoke();
                }
            }
        }

        private bool EnsureResolved()
        {
            if (_resolved) return true;
            _resolved = ResolveTargets();
            return _resolved;
        }

        /// <summary>Re-run target resolution on next Play/Scrub. Call this if the scene changed.</summary>
        public void InvalidateTargets() => _resolved = false;

        /// <summary>Subclass: read the assigned clip's duration. Returns 0 if no clip.</summary>
        protected abstract float GetClipDuration();

        /// <summary>Subclass: gather the scene objects this player will drive. Return false to abort.</summary>
        protected abstract bool ResolveTargets();

        /// <summary>Subclass: apply the clip's state at time t to the resolved targets.</summary>
        protected abstract void ApplyAt(float t);

        /// <summary>Subclass hook called once when playback acquires scene control — on a fresh
        /// Play (after targets resolved), but NOT when resuming from Pause. Pairs 1:1 with OnStop.
        /// E.g. freeze rigidbodies for a drive-originals player.</summary>
        protected virtual void OnPlay() { }

        /// <summary>Subclass hook called once when playback releases scene control — on Stop,
        /// natural completion, or disable, but NOT on Pause. Pairs 1:1 with OnPlay. The reason lets a
        /// subclass keep control on a natural finish (e.g. hold a recomposed object together).</summary>
        protected virtual void OnStop(PlaybackStopReason reason) { }

        //==================== OUTPUTS =====================
        public event Action OnStarted;
        public event Action<float> OnProgress;
        public event Action OnCompleted;
        public event Action OnReachedEnd;
        public event Action OnReachedStart;

        [Header("Events")]
        [Tooltip("Fired when playback starts (Play / PlayForward / PlayReverse).")]
        [SerializeField] private UnityEvent startedEvent;

        [Tooltip("Fired each frame during playback or on Scrub. Passes normalized progress [0,1].")]
        [SerializeField] private UnityEvent<float> progressEvent;

        [Tooltip("Fired when playback finishes at EITHER end (forward end or reverse start), without looping.")]
        [SerializeField] private UnityEvent completedEvent;

        [Tooltip("Fired when FORWARD playback reaches the clip end.")]
        [SerializeField] private UnityEvent reachedEndEvent;

        [Tooltip("Fired when REVERSE playback reaches the clip start — e.g. a shattered object " +
                 "finished recomposing.")]
        [SerializeField] private UnityEvent reachedStartEvent;
    }
}
