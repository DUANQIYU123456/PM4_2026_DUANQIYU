// ============================================================================
// VideoFocusController — Interaction controller
//
// Listens to a Focusable and drives a VideoPlayer's playback + audio fade.
// While focused: video plays from frame 0, looping, audio fades in to the
// profile's target volume. While unfocused: audio fades out, then the player
// pauses and seeks back to frame 0 so the first frame stays visible.
// Uses DOTween so rapid focus/unfocus events interrupt cleanly. Reads all
// tuning from a swappable VideoFocusProfile asset.
// ============================================================================

using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Drives a VideoPlayer's playback and audio fade based on a Focusable's focus state.</summary>
    public class VideoFocusController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("Focusable whose state drives the video playback")]
        [SerializeField] private Focusable focusable;

        [Tooltip("VideoPlayer to control (must be wired in the inspector)")]
        [SerializeField] private VideoPlayer videoPlayer;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining target volume, fade durations, and easing")]
        [SerializeField] private VideoFocusProfile profile;

        //==================== STATE =====================
        private Tween _tween;
        private float _currentVolume;

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (!focusable || !videoPlayer || !profile) return;

            focusable.OnFocused += HandleFocused;
            focusable.OnUnfocused += HandleUnfocused;

            videoPlayer.isLooping = true;
            ApplyVolume(0f);

            if (videoPlayer.isPrepared)
            {
                SeekToStart();
            }
            else
            {
                videoPlayer.prepareCompleted += HandlePrepared;
                videoPlayer.Prepare();
            }
        }

        private void OnDisable()
        {
            if (focusable)
            {
                focusable.OnFocused -= HandleFocused;
                focusable.OnUnfocused -= HandleUnfocused;
            }

            if (videoPlayer)
            {
                videoPlayer.prepareCompleted -= HandlePrepared;
            }

            _tween?.Kill();
        }

        //==================== HANDLERS =====================
        private void HandlePrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= HandlePrepared;

            // If the player started focusing while we were still preparing,
            // don't stomp the active playback by seeking back to frame 0.
            if (focusable && focusable.IsFocused) return;

            SeekToStart();
        }

        private void HandleFocused()
        {
            if (!videoPlayer || !profile) return;

            videoPlayer.prepareCompleted -= HandlePrepared;
            _tween?.Kill();

            videoPlayer.frame = 0;
            videoPlayer.Play();

            _tween = DOTween.To(() => _currentVolume, ApplyVolume, profile.focusedVolume, profile.fadeInDuration)
                .SetEase(profile.ease);
        }

        private void HandleUnfocused()
        {
            if (!videoPlayer || !profile) return;

            videoPlayer.prepareCompleted -= HandlePrepared;
            _tween?.Kill();

            _tween = DOTween.To(() => _currentVolume, ApplyVolume, 0f, profile.fadeOutDuration)
                .SetEase(profile.ease)
                .OnComplete(StopAndRewind);
        }

        //==================== HELPERS =====================
        private void SeekToStart()
        {
            if (!videoPlayer) return;
            videoPlayer.Pause();
            videoPlayer.frame = 0;
        }

        private void StopAndRewind()
        {
            if (!videoPlayer) return;
            videoPlayer.Pause();
            videoPlayer.frame = 0;
        }

        private void ApplyVolume(float v)
        {
            _currentVolume = v;
            if (!videoPlayer) return;

            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
            {
                videoPlayer.SetDirectAudioVolume(0, v);
            }
            else
            {
                var src = videoPlayer.GetTargetAudioSource(0);
                if (src) src.volume = v;
            }
        }
    }
}

// ============================================================================
// Setup in a scene
//   1. Add a Focusable to the target object (the one the player looks at).
//   2. Add a VideoPlayer (anywhere) and wire its clip + render texture or
//      material binding. Bind an AudioSource via SetTargetAudioSource(0, ...)
//      so the controller can fade its volume — or set the VideoPlayer's audio
//      output mode to Direct and the controller will fade SetDirectAudioVolume
//      on track 0 instead.
//   3. Add this component anywhere and assign the Focusable + VideoPlayer.
//   4. Create a VideoFocusProfile asset (Create > Ludocore > Video Focus
//      Profile) and assign it.
// ============================================================================
