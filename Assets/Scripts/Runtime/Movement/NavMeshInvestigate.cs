using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Walks to a point of interest, looks around, then returns home.</summary>
    [RequireComponent(typeof(NavMeshMotor))]
    public class NavMeshInvestigate : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("How long to look around at the point of interest (seconds)")]
        [Min(0f)]
        [SerializeField] private float lookAroundTime = 3f;

        [Tooltip("Return to the starting position after looking around")]
        [SerializeField] private bool returnHome = true;

        //==================== STATE =====================
        private NavMeshMotor _motor;
        private Vector3 _homePosition;
        private Coroutine _routine;

        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isInvestigating;

        public bool IsInvestigating => isInvestigating;
        public Vector3 HomePosition => _homePosition;

        //==================== OUTPUTS =====================
        public event Action<Vector3> OnInvestigateStarted;
        public event Action OnInvestigateStopped;
        public event Action OnPointReached;
        public event Action OnReturnedHome;

        [Header("Events")]
        [Tooltip("Fired when investigation begins, passes the target position")]
        [SerializeField] private UnityEvent<Vector3> investigateStartedEvent;
        [Tooltip("Fired when investigation is explicitly stopped")]
        [SerializeField] private UnityEvent investigateStoppedEvent;
        [Tooltip("Fired when the investigation point is reached")]
        [SerializeField] private UnityEvent pointReachedEvent;
        [Tooltip("Fired when the agent returns home")]
        [SerializeField] private UnityEvent returnedHomeEvent;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            _motor = GetComponent<NavMeshMotor>();
            _homePosition = transform.position;
        }

        private void OnDisable()
        {
            if (isInvestigating) StopInvestigate();
        }

        //==================== INPUTS =====================
        /// <summary>Investigate a world position.</summary>
        public void Investigate(Vector3 position)
        {
            if (!isInvestigating) _homePosition = transform.position;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(InvestigateRoutine(position));
        }

        /// <summary>Investigate a Transform's current position.</summary>
        public void Investigate(Transform t)
        {
            if (!t) return;
            Investigate(t.position);
        }

        /// <summary>Stop investigation and halt movement.</summary>
        [ContextMenu("Stop Investigate")]
        public void StopInvestigate()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            isInvestigating = false;
            _motor.Stop();
            OnInvestigateStopped?.Invoke();
            investigateStoppedEvent?.Invoke();
        }

        /// <summary>Reset the home position to the current location.</summary>
        [ContextMenu("Set Home Here")]
        public void SetHomeHere()
        {
            _homePosition = transform.position;
        }

        //==================== PRIVATE =====================
        private IEnumerator InvestigateRoutine(Vector3 point)
        {
            isInvestigating = true;
            OnInvestigateStarted?.Invoke(point);
            investigateStartedEvent?.Invoke(point);

            _motor.MoveTo(point);
            while (!_motor.HasArrived) yield return null;

            OnPointReached?.Invoke();
            pointReachedEvent?.Invoke();

            if (lookAroundTime > 0f) yield return new WaitForSeconds(lookAroundTime);

            if (returnHome)
            {
                _motor.MoveTo(_homePosition);
                while (!_motor.HasArrived) yield return null;

                OnReturnedHome?.Invoke();
                returnedHomeEvent?.Invoke();
            }

            isInvestigating = false;
            _routine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 home = Application.isPlaying ? _homePosition : transform.position;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(home, Vector3.one * 0.4f);
        }
    }
}
