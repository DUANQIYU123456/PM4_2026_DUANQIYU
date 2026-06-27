using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Plays a TransformClip back onto the originally recorded Transforms — the
    /// "drive originals" mode. Scrub to t=0 to reset to the pre-event state (e.g. the
    /// unbroken sphere); Play with speed=-1 to animate the rewind (the unshatter).
    ///
    /// Target resolution mirrors TransformRecorder: assign the same root + mode and the
    /// track indices line up by child order.
    ///
    /// If the recorded objects are Rigidbodies, leave freezePhysicsDuringPlayback ON —
    /// the player will set them kinematic during playback so transform writes aren't
    /// fought by the physics solver. Physics state is restored on Stop or disable.</summary>
    public class TransformPlayer : Player
    {
        //==================== CONFIG =====================
        [Header("Target")]
        [Tooltip("Transform whose pose(s) are driven by the clip. Defaults to this GameObject if empty.")]
        [SerializeField] private Transform root;

        [Tooltip("Self = drive the root only. Children = drive each direct child of the root.")]
        [SerializeField] private TransformRecorder.TargetMode mode = TransformRecorder.TargetMode.Self;

        [Header("Clip")]
        [Tooltip("The recorded clip to play back.")]
        [SerializeField] private TransformClip clip;

        [Header("Physics")]
        [Tooltip("If any targets have a Rigidbody, set them kinematic during playback so the " +
                 "physics solver doesn't fight the driven transforms. Restored on Stop/disable.")]
        [SerializeField] private bool freezePhysicsDuringPlayback = true;

        [Tooltip("When playback finishes on its own (e.g. a reverse recompose reaching t=0), KEEP the " +
                 "frozen bodies kinematic instead of handing them back to physics — so a reassembled " +
                 "object stays held together. Call ReleaseHeldBodies() to drop them back into physics.")]
        [SerializeField] private bool holdControlOnComplete;

        [Tooltip("While engaged/playing, disable the target objects' OWN colliders so the scrubbed " +
                 "object doesn't push the live world — full 'ghost' isolation (like a dolly cart). " +
                 "Restored when stopped. Off = kinematic bodies still collide with and shove dynamic objects.")]
        [SerializeField] private bool ghostWhileEngaged;

        //==================== STATE =====================
        private readonly List<Transform> _targets = new();
        private readonly List<Rigidbody> _frozenBodies = new();
        private readonly List<bool> _frozenWasKinematic = new();
        private readonly List<Collider> _disabledColliders = new();

        public TransformClip Clip => clip;
        public int TargetCount => _targets.Count;

        //==================== PRIVATE =====================
        protected override float GetClipDuration() => clip ? clip.Duration : 0f;

        protected override bool ResolveTargets()
        {
            _targets.Clear();

            if (!clip)
            {
                Debug.LogWarning($"{name}: TransformPlayer has no clip assigned.", this);
                return false;
            }

            var r = root ? root : transform;

            switch (mode)
            {
                case TransformRecorder.TargetMode.Self:
                    _targets.Add(r);
                    break;

                case TransformRecorder.TargetMode.Children:
                    for (int i = 0; i < r.childCount; i++)
                        _targets.Add(r.GetChild(i));
                    break;
            }

            if (_targets.Count == 0)
            {
                Debug.LogWarning($"{name}: TransformPlayer resolved zero targets ({mode}).", this);
                return false;
            }

            if (_targets.Count != clip.TrackCount)
            {
                Debug.LogWarning(
                    $"{name}: target count ({_targets.Count}) does not match clip track count " +
                    $"({clip.TrackCount}). Extra targets/tracks will be ignored.", this);
            }

            return true;
        }

        protected override void ApplyAt(float t)
        {
            int count = Mathf.Min(_targets.Count, clip.TrackCount);
            bool world = clip.Space == Space.World;

            for (int i = 0; i < count; i++)
            {
                var target = _targets[i];
                if (!target) continue;

                var s = clip.Evaluate(i, t);

                if (world)
                {
                    target.SetPositionAndRotation(s.position, s.rotation);
                }
                else
                {
                    target.localPosition = s.position;
                    target.localRotation = s.rotation;
                }
                target.localScale = s.scale;
            }
        }

        protected override void OnPlay()
        {
            if (freezePhysicsDuringPlayback) FreezeBodies();
            if (ghostWhileEngaged) DisableColliders();
        }

        protected override void OnStop(PlaybackStopReason reason)
        {
            // Re-solidify on any stop — the object becomes collidable again wherever it ends up.
            if (ghostWhileEngaged) RestoreColliders();

            if (!freezePhysicsDuringPlayback) return;

            // On a natural finish, optionally keep the bodies kinematic so a recomposed object
            // (e.g. the reassembled sphere at t=0) stays held in place instead of collapsing.
            if (reason == PlaybackStopReason.Completed && holdControlOnComplete) return;

            ThawBodies();
        }

        /// <summary>Hand any held-kinematic bodies back to physics. Use after a recompose that
        /// finished with holdControlOnComplete enabled, when you want the object to become dynamic again.</summary>
        [ContextMenu("Release Held Bodies")]
        public void ReleaseHeldBodies() => ThawBodies();

        //==================== PRIVATE =====================
        private void FreezeBodies()
        {
            _frozenBodies.Clear();
            _frozenWasKinematic.Clear();

            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                if (!target) continue;

                if (target.TryGetComponent<Rigidbody>(out var rb))
                {
                    _frozenBodies.Add(rb);
                    _frozenWasKinematic.Add(rb.isKinematic);
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void ThawBodies()
        {
            for (int i = 0; i < _frozenBodies.Count; i++)
            {
                var rb = _frozenBodies[i];
                if (!rb) continue;
                rb.isKinematic = _frozenWasKinematic[i];
            }
            _frozenBodies.Clear();
            _frozenWasKinematic.Clear();
        }

        // Ghost isolation: disable only the colliders we find enabled on the targets themselves
        // (not children), and restore exactly those on stop.
        private void DisableColliders()
        {
            _disabledColliders.Clear();
            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                if (!target) continue;

                var cols = target.GetComponents<Collider>();
                for (int c = 0; c < cols.Length; c++)
                {
                    if (!cols[c].enabled) continue;
                    cols[c].enabled = false;
                    _disabledColliders.Add(cols[c]);
                }
            }
        }

        private void RestoreColliders()
        {
            for (int i = 0; i < _disabledColliders.Count; i++)
            {
                var col = _disabledColliders[i];
                if (col) col.enabled = true;
            }
            _disabledColliders.Clear();
        }
    }
}
