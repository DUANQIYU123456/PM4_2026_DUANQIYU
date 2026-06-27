using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Ludocore
{
    /// <summary>Hands the player between FREE first-person control and REPLAY of recorded clips, and
    /// back to FREE from any point along the timeline ("start free from point X"). Also drives
    /// recording in place.
    ///
    /// Modular by channel: each lane is an independent recorder+player pair (e.g. BODY = root
    /// position+yaw, LOOK = camera-target pitch). Toggle a channel's 'enabled' flag to include or
    /// exclude it from record / replay / scrub without unassigning references. Put the body channel
    /// first — its player reports Progress for sliders.
    ///
    /// The hard part this solves: a first-person controller and a player clip both want the same
    /// transform. During replay we disable the controller AND its CharacterController (an enabled
    /// CharacterController fights transform writes). On handoff we re-enable the CharacterController
    /// (which commits the replayed position) and resync the controller's cached look-pitch so the
    /// camera doesn't snap. Yaw lives on the transform itself, so it needs no sync.
    ///
    /// Decoupled by design: the controller is referenced as a Behaviour and its private pitch field
    /// is found by name via reflection, so this has no compile-time dependency on Starter Assets and
    /// survives package reimports.</summary>
    public class PlaybackControl : MonoBehaviour, IScrubbable
    {
        public enum Mode { Free, Recording, Replay }

        /// <summary>One record+playback lane. Toggle 'enabled' to include/exclude it without
        /// unassigning references.</summary>
        [System.Serializable]
        public class Channel
        {
            [Tooltip("Label for readability in the inspector (e.g. 'body', 'look').")]
            public string label = "channel";

            [Tooltip("Include this channel in record / replay / scrub.")]
            public bool enabled = true;

            [Tooltip("Recorder for this lane. Optional if you only ever play a pre-made clip.")]
            public TransformRecorder recorder;

            [Tooltip("Player for this lane.")]
            public TransformPlayer player;
        }

        //==================== SCENE REFERENCES =====================
        [Header("Player Control")]
        [Tooltip("The first-person controller component (e.g. StarterAssets FirstPersonController). Toggled on/off.")]
        [SerializeField] private Behaviour firstPersonController;

        [Tooltip("The player's CharacterController. Auto-found on the controller's GameObject if left empty.")]
        [SerializeField] private CharacterController characterController;

        [Tooltip("Also disable the CharacterController during replay. OFF (recommended) keeps its collider " +
                 "active so the player still fires triggers/detections while the clip drives it — the " +
                 "movement SCRIPT is disabled either way, so it won't fight the playback. Turn ON only if " +
                 "you see the player snapping or sticking during playback.")]
        [SerializeField] private bool disableCharacterController;

        [Tooltip("Optional input component to also disable during replay (e.g. StarterAssetsInputs), " +
                 "so it stops capturing the cursor while you scrub.")]
        [SerializeField] private Behaviour inputComponent;

        [Tooltip("The look-pitch target (head/eye transform pitch is applied to). Needed only if a LOOK " +
                 "channel is enabled and you want a seamless handoff back to free play.")]
        [SerializeField] private Transform cameraTarget;

        [Tooltip("Private pitch field on the controller to resync on handoff. StarterAssets uses '_cinemachineTargetPitch'.")]
        [SerializeField] private string pitchFieldName = "_cinemachineTargetPitch";

        //==================== CHANNELS =====================
        [Header("Channels")]
        [Tooltip("Each lane is an independent recorder+player. Put the BODY (root position+yaw) first; " +
                 "add a LOOK lane (camera-target pitch) for full view replay. Uncheck a channel to leave it out.")]
        [SerializeField] private List<Channel> channels = new();

        [Header("Replay")]
        [Min(0.0001f)]
        [Tooltip("Playback speed magnitude. Play() uses +this, PlayReverse() uses -this.")]
        [SerializeField] private float replaySpeed = 1f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private Mode mode = Mode.Free;

        private FieldInfo _pitchField;

        public Mode CurrentMode => mode;
        public float Progress
        {
            get { var p = PrimaryPlayer(); return p ? p.Progress : 0f; }
        }

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (!characterController && firstPersonController)
                characterController = firstPersonController.GetComponent<CharacterController>();

            if (firstPersonController && !string.IsNullOrEmpty(pitchFieldName))
            {
                _pitchField = firstPersonController.GetType().GetField(
                    pitchFieldName, BindingFlags.NonPublic | BindingFlags.Instance);

                if (_pitchField == null)
                    Debug.LogWarning(
                        $"{name}: pitch field '{pitchFieldName}' not found on " +
                        $"{firstPersonController.GetType().Name}. Look handoff will snap. " +
                        "Disable the look channel, or fix the field name.", this);
            }
        }

        //==================== RECORDING =====================
        [ContextMenu("Begin Recording")]
        public void BeginRecording()
        {
            SetFreeControl(true);            // record while playing normally
            mode = Mode.Recording;
            foreach (var c in channels)
                if (c.enabled && c.recorder) c.recorder.StartRecording();
        }

        [ContextMenu("End Recording")]
        public void EndRecording()
        {
            foreach (var c in channels)
                if (c.recorder) c.recorder.StopRecording();
            mode = Mode.Free;
        }

        //==================== REPLAY =====================
        /// <summary>Take control away from the first-person controller so clips can drive the transform.</summary>
        [ContextMenu("Enter Replay")]
        public void EnterReplay()
        {
            if (mode == Mode.Replay) return;
            SetFreeControl(false);           // disable the movement script so it stops fighting the clip
            mode = Mode.Replay;
        }

        // IScrubbable: a slider / FloatVariable / MIDI knob can drive the whole player rig uniformly.
        // Engage = take the player off live control (enter replay); Release = hand it back from here.
        public void Engage() => EnterReplay();
        public void Release() => ResumeFreeFromHere();

        [ContextMenu("Play")]
        public void Play() => PlayDirected(+1f);

        [ContextMenu("Play Reverse")]
        public void PlayReverse() => PlayDirected(-1f);

        [ContextMenu("Pause")]
        public void Pause()
        {
            foreach (var c in channels)
                if (c.enabled && c.player) c.player.Pause();
        }

        /// <summary>Scrub all enabled channels to a normalized [0,1] position. Enters replay if needed.
        /// Wire this to a UI slider's OnValueChanged for back-and-forth scrubbing.</summary>
        public void Scrub(float normalized)
        {
            EnterReplay();
            foreach (var c in channels)
                if (c.enabled && c.player) c.player.Scrub(normalized);
        }

        //==================== HANDOFF =====================
        /// <summary>Stop replaying and hand control back to the first-person controller from wherever
        /// the cursor currently sits — "start free from point X".</summary>
        [ContextMenu("Resume Free From Here")]
        public void ResumeFreeFromHere()
        {
            // Stop driving (releases in place; the root has no Rigidbody so this just halts the clips).
            foreach (var c in channels)
                if (c.player) c.player.Stop();

            SyncControllerPitch();           // so the camera doesn't snap to the controller's stale pitch
            SetFreeControl(true);            // re-enabling the CharacterController accepts the replayed position
            mode = Mode.Free;
        }

        //==================== PRIVATE =====================
        private void PlayDirected(float sign)
        {
            EnterReplay();
            float s = Mathf.Max(0.0001f, replaySpeed) * sign;
            foreach (var c in channels)
            {
                if (!c.enabled || !c.player) continue;
                c.player.Speed = s;
                c.player.Play();
            }
        }

        /// <summary>The first enabled channel with a player — its Progress drives the UI slider.</summary>
        private TransformPlayer PrimaryPlayer()
        {
            foreach (var c in channels)
                if (c.enabled && c.player) return c.player;
            return null;
        }

        private void SetFreeControl(bool on)
        {
            // Disabling the movement SCRIPT is what stops the fight with the clip — it's the script's
            // CharacterController.Move()/input that competes, not the controller component itself.
            // The CharacterController is left ENABLED by default so its collider stays in the physics
            // scene and the player keeps firing triggers/detections while the clip drives it.
            if (firstPersonController) firstPersonController.enabled = on;
            if (inputComponent) inputComponent.enabled = on;
            if (disableCharacterController && characterController) characterController.enabled = on;
        }

        private void SyncControllerPitch()
        {
            if (_pitchField == null || firstPersonController == null || !cameraTarget) return;
            float pitch = NormalizeAngle(cameraTarget.localEulerAngles.x);
            _pitchField.SetValue(firstPersonController, pitch);
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            else if (a < -180f) a += 360f;
            return a;
        }
    }
}
