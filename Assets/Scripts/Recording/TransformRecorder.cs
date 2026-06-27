using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Records the pose timeline of one Transform — either the target itself or its
    /// direct children — into a TransformClip asset.
    ///
    /// Typical use: drop on a "shatterRoot" GameObject whose children are fragments. Start
    /// recording at the moment of breaking; the children's tumbling paths get captured.
    /// Later, a TransformPlayer aimed at the same root can replay or unshatter them.
    ///
    /// World space is the safe default: fragments that get unparented during the recording
    /// (typical for Rigidbody shrapnel) are still tracked correctly because their world
    /// positions remain meaningful.</summary>
    public class TransformRecorder : Recorder
    {
        public enum TargetMode
        {
            /// <summary>Record only the root transform.</summary>
            Self,

            /// <summary>Record each direct child of the root. The child list is snapshotted at
            /// the moment recording starts — adding/removing children mid-recording has no effect.</summary>
            Children
        }

        //==================== CONFIG =====================
        [Header("Target")]
        [Tooltip("Transform to record. Defaults to this GameObject if empty.")]
        [SerializeField] private Transform root;

        [Tooltip("Self = record the root only. Children = record each direct child of the root.")]
        [SerializeField] private TargetMode mode = TargetMode.Self;

        [Tooltip("Coordinate space for captured samples. World survives reparenting.")]
        [SerializeField] private Space space = Space.World;

        [Header("Output")]
        [Tooltip("Clip asset to write into. Will be cleared and overwritten on StartRecording.")]
        [SerializeField] private TransformClip clip;

        //==================== STATE =====================
        private readonly List<Transform> _tracked = new();

        public TransformClip Clip => clip;
        public int TrackedCount => _tracked.Count;

        //==================== PRIVATE =====================
        protected override bool OnBeginRecording()
        {
            if (!clip)
            {
                Debug.LogWarning($"{name}: TransformRecorder has no clip assigned.", this);
                return false;
            }

            ResolveTargets();

            if (_tracked.Count == 0)
            {
                Debug.LogWarning($"{name}: TransformRecorder resolved zero targets ({mode}).", this);
                return false;
            }

            var labels = new List<string>(_tracked.Count);
            for (int i = 0; i < _tracked.Count; i++)
                labels.Add(_tracked[i] ? _tracked[i].name : $"track_{i}");

            clip.BeginRecording(sampleRate, _tracked.Count, space, labels);
            return true;
        }

        protected override void RecordFrame()
        {
            for (int i = 0; i < _tracked.Count; i++)
            {
                var t = _tracked[i];
                if (!t)
                {
                    // Target was destroyed — repeat the previous sample so the track stays aligned.
                    var prev = clip.Tracks[i].Count > 0
                        ? clip.Tracks[i].samples[clip.Tracks[i].Count - 1]
                        : TransformSample.Identity;
                    clip.AppendSample(i, prev);
                    continue;
                }

                TransformSample sample = space == Space.World
                    ? new TransformSample
                    {
                        position = t.position,
                        rotation = t.rotation,
                        scale    = t.localScale
                    }
                    : new TransformSample
                    {
                        position = t.localPosition,
                        rotation = t.localRotation,
                        scale    = t.localScale
                    };

                clip.AppendSample(i, sample);
            }
        }

        protected override void OnEndRecording(float finalDuration)
        {
            if (clip) clip.EndRecording(finalDuration);

#if UNITY_EDITOR
            // In editor: mark the asset dirty so the recorded data persists when leaving play mode.
            if (clip)
            {
                UnityEditor.EditorUtility.SetDirty(clip);
            }
#endif
        }

        //==================== PRIVATE =====================
        private void ResolveTargets()
        {
            _tracked.Clear();

            var r = root ? root : transform;

            switch (mode)
            {
                case TargetMode.Self:
                    _tracked.Add(r);
                    break;

                case TargetMode.Children:
                    for (int i = 0; i < r.childCount; i++)
                        _tracked.Add(r.GetChild(i));
                    break;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor convenience: persist the in-memory clip back to the asset on disk.</summary>
        [ContextMenu("Save Clip Asset")]
        private void SaveClipAsset()
        {
            if (!clip) return;
            UnityEditor.EditorUtility.SetDirty(clip);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"{name}: saved clip '{clip.name}' ({clip.TrackCount} tracks, {clip.Duration:F2}s).", clip);
        }
#endif
    }
}
