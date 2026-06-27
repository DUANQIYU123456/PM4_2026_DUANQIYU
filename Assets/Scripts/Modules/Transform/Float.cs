using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Perpetual low-gravity floating drift. Adds smooth, non-repeating Perlin-noise
    /// offsets to position and rotation so the object reads as weightlessly hovering
    /// in place rather than mechanically bobbing. Drift is centred on the rest pose
    /// captured at Awake.
    /// </summary>
    public class Float : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Max drift distance per axis (metres). Y is the buoyant up/down; keep X and Z smaller.")]
        [SerializeField] private Vector3 positionAmplitude = new Vector3(0.25f, 0.6f, 0.25f);

        [Tooltip("Max tilt per axis (degrees). X/Z read as listing, Y as a slow yaw.")]
        [SerializeField] private Vector3 rotationAmplitude = new Vector3(3f, 2f, 3f);

        [Tooltip("How fast the drift evolves. Lower = slower, dreamier. 0.1-0.4 sells 'gravity is off'.")]
        [Min(0f)]
        [SerializeField] private float speed = 0.25f;

        [Tooltip("Drift in local space (recommended; works under a moving or rotated parent).")]
        [SerializeField] private bool useLocalSpace = true;

        [Tooltip("Start floating automatically when enabled.")]
        [SerializeField] private bool autoPlay = true;

        [Header("Desync")]
        [Tooltip("Give each instance a unique phase so multiple floaters don't move in lockstep.")]
        [SerializeField] private bool randomizeSeed = true;

        [Tooltip("Manual phase offset, used when Randomize Seed is off.")]
        [SerializeField] private float seed;

        //==================== STATE =====================
        // Per-axis sample lines through the noise field (fractional, decorrelated so axes never sync).
        private static readonly Vector3 PosSeeds = new Vector3(11.3f, 53.7f, 97.1f);
        private static readonly Vector3 RotSeeds = new Vector3(149.9f, 211.4f, 307.6f);
        // Slight per-axis frequency variation so no two axes share a period.
        private static readonly Vector3 PosFreq = new Vector3(1f, 0.8f, 1.1f);
        private static readonly Vector3 RotFreq = new Vector3(0.9f, 0.7f, 1.05f);

        private Vector3 _startPos;
        private Quaternion _startRot;
        private float _phase;
        private float _time;
        private bool _playing;

        public bool IsFloating => _playing;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            CaptureRestPose();
            _phase = randomizeSeed ? Random.Range(0f, 1000f) : seed;
        }

        private void OnEnable()
        {
            if (autoPlay) Play();
        }

        private void Update()
        {
            if (!_playing) return;

            _time += Time.deltaTime * speed;
            float t = _time + _phase;

            Vector3 posOffset = new Vector3(
                SignedNoise(t * PosFreq.x, PosSeeds.x) * positionAmplitude.x,
                SignedNoise(t * PosFreq.y, PosSeeds.y) * positionAmplitude.y,
                SignedNoise(t * PosFreq.z, PosSeeds.z) * positionAmplitude.z);

            Quaternion rotOffset = Quaternion.Euler(
                SignedNoise(t * RotFreq.x, RotSeeds.x) * rotationAmplitude.x,
                SignedNoise(t * RotFreq.y, RotSeeds.y) * rotationAmplitude.y,
                SignedNoise(t * RotFreq.z, RotSeeds.z) * rotationAmplitude.z);

            if (useLocalSpace)
            {
                transform.localPosition = _startPos + posOffset;
                transform.localRotation = _startRot * rotOffset;
            }
            else
            {
                transform.position = _startPos + posOffset;
                transform.rotation = _startRot * rotOffset;
            }
        }

        //==================== INPUTS =====================
        /// <summary>Begin (or resume) floating from where the drift left off.</summary>
        [ContextMenu("Play")]
        public void Play() => _playing = true;

        /// <summary>Halt floating, freezing the object at its current drifted pose.</summary>
        public void Stop() => _playing = false;

        /// <summary>Re-capture the current transform as the rest pose the drift centres on.</summary>
        [ContextMenu("Capture Rest Pose")]
        public void CaptureRestPose()
        {
            _startPos = useLocalSpace ? transform.localPosition : transform.position;
            _startRot = useLocalSpace ? transform.localRotation : transform.rotation;
        }

        //==================== PRIVATE =====================
        /// <summary>Perlin noise remapped from [0,1] to [-1,1] so drift is centred on rest.</summary>
        private static float SignedNoise(float t, float line)
        {
            return (Mathf.PerlinNoise(t + line, line) - 0.5f) * 2f;
        }
    }
}
