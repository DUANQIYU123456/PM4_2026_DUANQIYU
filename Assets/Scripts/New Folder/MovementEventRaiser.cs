using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Watches a player's CharacterController and raises edge events when it
    /// starts moving and when it stops, exposed in all three reaction styles at once:
    /// a ScriptableObject GameEvent channel, an Inspector UnityEvent, and a C# Action.
    ///
    /// Generic by design — it reads horizontal CharacterController.velocity, so it works
    /// with the StarterAssets FirstPersonController (which drives a CharacterController) or
    /// any other CharacterController-based player. Assign the controller, or leave it empty
    /// to auto-grab one from this GameObject.
    ///
    /// Edge-triggered: "started moving" fires once when horizontal speed rises above
    /// moveThreshold; "stopped" fires once after speed stays below it for stopGraceTime
    /// (the grace debounces brief dips from direction changes or stair-steps).</summary>
    public class MovementEventRaiser : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Source")]
        [Tooltip("Player CharacterController to watch. If empty, auto-grabs one from this GameObject.")]
        [SerializeField] private CharacterController controller;

        [Header("Config")]
        [Tooltip("Horizontal speed (m/s) above which the player counts as moving.")]
        [Min(0f)]
        [SerializeField] private float moveThreshold = 0.1f;

        [Tooltip("Seconds the player must stay below the threshold before 'stopped' fires. " +
                 "Debounces brief velocity dips so the events don't chatter.")]
        [Min(0f)]
        [SerializeField] private float stopGraceTime = 0.1f;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isMoving;

        private float _stoppedTimer;

        public bool IsMoving => isMoving;

        //==================== OUTPUTS =====================
        public event Action OnStartedMoving;
        public event Action OnStopped;

        [Header("Game Event SO")]
        [Tooltip("Raised once when the player starts moving.")]
        [SerializeField] private GameEvent startedMovingGameEvent;

        [Tooltip("Raised once when the player stops.")]
        [SerializeField] private GameEvent stoppedGameEvent;

        [Header("Unity Events")]
        [Tooltip("Invoked once when the player starts moving.")]
        [SerializeField] private UnityEvent startedMovingEvent;

        [Tooltip("Invoked once when the player stops.")]
        [SerializeField] private UnityEvent stoppedEvent;

        //==================== LIFECYCLE =====================
        private void Reset()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Awake()
        {
            if (!controller) controller = GetComponent<CharacterController>();
            if (!controller)
                Debug.LogWarning($"[{nameof(MovementEventRaiser)}:{name}] No CharacterController assigned or found.", this);
        }

        private void Update()
        {
            if (!controller) return;

            bool moving = HorizontalSpeed() > moveThreshold;

            if (moving)
            {
                _stoppedTimer = 0f;
                if (!isMoving) SetMoving(true);
            }
            else if (isMoving)
            {
                // Below threshold — wait out the grace window before declaring a stop.
                _stoppedTimer += Time.deltaTime;
                if (_stoppedTimer >= stopGraceTime) SetMoving(false);
            }
        }

        //==================== PRIVATE =====================
        private float HorizontalSpeed()
        {
            Vector3 v = controller.velocity;
            v.y = 0f;
            return v.magnitude;
        }

        private void SetMoving(bool moving)
        {
            isMoving = moving;

            if (moving)
            {
                OnStartedMoving?.Invoke();
                startedMovingEvent?.Invoke();
                if (startedMovingGameEvent) startedMovingGameEvent.Raise();
            }
            else
            {
                OnStopped?.Invoke();
                stoppedEvent?.Invoke();
                if (stoppedGameEvent) stoppedGameEvent.Raise();
            }
        }
    }
}
