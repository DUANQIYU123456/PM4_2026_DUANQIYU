using UnityEngine;

namespace Ludocore
{
    /// <summary>NPC fight command: chases the threat via NavMeshChase, fires a fight animator trigger when within stopping distance, completes when the threat escapes chase range.</summary>
    public class NPCFightCommand : Command
    {
        //==================== CONFIG =====================
        [Header("Modules")]
        [Tooltip("NavMeshChase module that re-paths toward the target.")]
        [SerializeField] private NavMeshChase chase;
        [Tooltip("NavMesh motor — used to detect stopping distance and toggle walking animation.")]
        [SerializeField] private NavMeshMotor motor;
        [Tooltip("Animator driving idle / walk / fight states. Optional.")]
        [SerializeField] private Animator animator;

        [Header("Threat")]
        [Tooltip("Tag of the object to chase and fight.")]
        [SerializeField] private string threatTag = "Player";
        [Tooltip("Max horizontal distance before the NPC gives up. Beyond this, the command completes.")]
        [Min(0f)]
        [SerializeField] private float maxChaseRange = 25f;

        [Header("Animation")]
        [Tooltip("Animator trigger fired when the NPC starts walking (out of stopping distance).")]
        [SerializeField] private string walkTrigger = "Walk";
        [Tooltip("Animator trigger fired when the NPC stops walking (within stopping distance or command ends).")]
        [SerializeField] private string idleTrigger = "Idle";
        [Tooltip("Animator trigger fired when entering stopping distance (and again every attackInterval while in range).")]
        [SerializeField] private string fightTrigger = "Fight";
        [Tooltip("Seconds between repeated fight triggers while in stopping distance. 0 = fire once per re-engagement.")]
        [Min(0f)]
        [SerializeField] private float attackInterval = 1.0f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private string resolvedThreat = "—";
        [ReadOnly, SerializeField] private bool inAttackRange;

        private Transform _threat;
        private bool _wasInRange;
        private bool _hasTickedOnce;
        private float _nextAttackTime;

        //==================== LIFECYCLE =====================
        private void Reset()
        {
            if (!chase) chase = GetComponent<NavMeshChase>();
            if (!motor) motor = GetComponent<NavMeshMotor>();
            if (!animator) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!IsActive) return;

            if (!_threat) { Cancel(); return; }
            if (HorizontalDistance(_threat.position, transform.position) > maxChaseRange) { Cancel(); return; }

            bool inRange = motor && motor.HasArrived;
            inAttackRange = inRange;

            if (!_hasTickedOnce || inRange != _wasInRange)
            {
                if (inRange) TriggerIdle();
                else TriggerWalk();
            }

            if (inRange)
            {
                if (!_hasTickedOnce || !_wasInRange)
                {
                    FireFightTrigger();
                    _nextAttackTime = attackInterval > 0f ? Time.time + attackInterval : float.PositiveInfinity;
                }
                else if (Time.time >= _nextAttackTime)
                {
                    FireFightTrigger();
                    _nextAttackTime = Time.time + attackInterval;
                }
            }

            _wasInRange = inRange;
            _hasTickedOnce = true;
        }

        //==================== COMMAND =====================
        protected override void OnExecute()
        {
            _threat = FindNearestWithTag(threatTag);
            if (!_threat || !chase) { Complete(); return; }

            resolvedThreat = _threat.name;
            _wasInRange = false;
            _hasTickedOnce = false;
            _nextAttackTime = 0f;

            chase.StartChase(_threat);
            // Kick path computation this frame so motor.HasArrived isn't stale-true on the first Update tick.
            if (motor) motor.MoveTo(_threat.position);
        }

        protected override void OnCancel()
        {
            if (chase && chase.IsChasing) chase.StopChase();
            TriggerIdle();
            inAttackRange = false;
            _threat = null;
            resolvedThreat = "—";
        }

        //==================== PRIVATE =====================
        private void FireFightTrigger()
        {
            if (!animator || string.IsNullOrEmpty(fightTrigger)) return;
            animator.SetTrigger(fightTrigger);
        }

        private void TriggerWalk()
        {
            if (!animator) return;
            if (!string.IsNullOrEmpty(idleTrigger)) animator.ResetTrigger(idleTrigger);
            if (!string.IsNullOrEmpty(walkTrigger)) animator.SetTrigger(walkTrigger);
        }

        private void TriggerIdle()
        {
            if (!animator) return;
            if (!string.IsNullOrEmpty(walkTrigger)) animator.ResetTrigger(walkTrigger);
            if (!string.IsNullOrEmpty(idleTrigger)) animator.SetTrigger(idleTrigger);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private Transform FindNearestWithTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;

            GameObject[] candidates;
            try { candidates = GameObject.FindGameObjectsWithTag(tag); }
            catch (UnityException) { return null; }

            if (candidates.Length == 0) return null;
            if (candidates.Length == 1) return candidates[0].transform;

            Transform nearest = null;
            float bestSqr = float.PositiveInfinity;
            Vector3 here = transform.position;
            foreach (var go in candidates)
            {
                float d = (go.transform.position - here).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; nearest = go.transform; }
            }
            return nearest;
        }
    }
}
