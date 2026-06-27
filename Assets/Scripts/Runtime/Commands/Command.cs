using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>
    /// Polymorphic contract for any finite-lifecycle interaction — work with a defined Execute → Complete arc.
    /// Implemented by <see cref="Command"/>; composites can implement directly without extending the base.
    /// </summary>
    public interface ICommand
    {
        string Key { get; }
        string Description { get; }
        bool IsActive { get; }
        event Action OnStarted;
        event Action OnCompleted;
        void Execute();
        void Cancel();
    }

    /// <summary>
    /// Base for finite-lifecycle interactions. Inherit, override <see cref="OnExecute"/>, optionally
    /// <see cref="OnCancel"/>, and call <see cref="Complete"/> when work finishes. Optionally override
    /// <see cref="Key"/> / <see cref="Description"/> in code for LLM-friendly identity.
    /// </summary>
    public abstract class Command : MonoBehaviour, ICommand
    {
        //==================== CONFIG =====================
        [Header("Trigger")]
        [Tooltip("Optional SO event channel. When raised, Execute() fires. Multiple commands across multiple GameObjects can subscribe to the same channel — broadcast.")]
        [SerializeField] private GameEvent triggerEvent;

        [Header("Registry")]
        [Tooltip("Optional SO registry. If assigned, this command joins it on Awake (and leaves on OnDestroy), making it discoverable by Key (LLM dispatch, scene-wide queries). Leave empty for commands wired via direct references only.")]
        [SerializeField] private CommandRegistry registry;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private bool isActive;

        public virtual string Key => DefaultKey();
        public virtual string Description => "";
        public bool IsActive => isActive;

        //==================== OUTPUTS =====================
        public event Action OnStarted;
        public event Action OnCompleted;

        [Header("Events")]
        [Tooltip("Fired when the command begins executing.")]
        [SerializeField] private UnityEvent startedEvent;
        [Tooltip("Fired when the command finishes (naturally or via cancel).")]
        [SerializeField] private UnityEvent completedEvent;

        //==================== LIFECYCLE =====================
        protected virtual void Awake()
        {
            if (registry) registry.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (registry) registry.Unregister(this);
        }

        protected virtual void OnEnable()
        {
            if (triggerEvent) triggerEvent.OnRaised += Execute;
        }

        protected virtual void OnDisable()
        {
            if (triggerEvent) triggerEvent.OnRaised -= Execute;
            if (isActive) Cancel();
        }

        //==================== INPUTS =====================
        /// <summary>Start the command. No-op if already active.</summary>
        [ContextMenu("Execute")]
        public void Execute()
        {
            if (isActive) return;

            isActive = true;
            OnStarted?.Invoke();
            startedEvent?.Invoke();

            OnExecute();
        }

        /// <summary>Stop the command early. Routes through <see cref="OnCancel"/> then <see cref="Complete"/>.</summary>
        [ContextMenu("Cancel")]
        public void Cancel()
        {
            if (!isActive) return;
            OnCancel();
            Complete();
        }

        //==================== SUBCLASS API =====================
        /// <summary>Implement the command's effect. For one-shot commands, call <see cref="Complete"/> here. For long-running commands, call it when the work ends.</summary>
        protected abstract void OnExecute();

        /// <summary>Override to release resources / stop side effects when cancelled. Called before <see cref="Complete"/>.</summary>
        protected virtual void OnCancel() { }

        /// <summary>Subclasses call this when their work is done. Idempotent.</summary>
        protected void Complete()
        {
            if (!isActive) return;
            isActive = false;

            OnCompleted?.Invoke();
            completedEvent?.Invoke();
        }

        /// <summary>Default key derived from the class name — strips "NPC" prefix and "Command" suffix, lowercases. e.g. NPCFleeCommand → "flee".</summary>
        protected virtual string DefaultKey()
        {
            var n = GetType().Name;
            if (n.StartsWith("NPC")) n = n.Substring(3);
            if (n.EndsWith("Command")) n = n.Substring(0, n.Length - 7);
            return n.ToLowerInvariant();
        }
    }
}
