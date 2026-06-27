using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>
    /// Polymorphic contract for any infinite-lifecycle interaction — continuous coupling between a source signal and an output.
    /// Implemented by <see cref="Binding"/>; composites can implement directly without extending the base.
    /// </summary>
    public interface IBinding
    {
        string Key { get; }
        string Description { get; }
        bool IsActive { get; }
        event Action OnActivated;
        event Action OnDeactivated;
        void Activate();
        void Deactivate();
    }

    /// <summary>
    /// Base for infinite-lifecycle interactions. Pipeline: <c>source.OnChanged → curve → range → envelope → Apply</c>.
    /// Push-based when a <see cref="FloatVariable"/> source is wired (recompute only on change); per-frame pull only
    /// for the Time / SineTime / Random fallback. The envelope step always runs in Update and early-outs when settled.
    /// Inherit, override <see cref="Apply"/>. Optionally override <see cref="Key"/> / <see cref="Description"/> in code.
    /// </summary>
    public abstract class Binding : MonoBehaviour, IBinding
    {
        //==================== CONFIG =====================
        [Header("Trigger")]
        [Tooltip("Optional SO event channel. When raised, the binding becomes enabled. Subscription is set in Awake so a disabled binding can still be woken by an event raise.")]
        [SerializeField] private GameEvent triggerEvent;

        [Header("Registry")]
        [Tooltip("Optional SO registry. If assigned, this binding joins it on Awake (and leaves on OnDestroy), making it discoverable by Key (LLM dispatch, scene-wide queries). Leave empty for bindings wired via direct references only.")]
        [SerializeField] private BindingRegistry registry;

        [Header("Source")]
        [Tooltip("FloatVariable source. The binding subscribes to OnChanged — recompute only fires when the source value changes. Leave empty to use the fallback below.")]
        [SerializeField] private FloatVariable source;
        [Tooltip("Driver to use when no FloatVariable source is wired. Pulled per frame.")]
        [SerializeField] private FallbackSource fallback = FallbackSource.Time;
        [Tooltip("Period in seconds for Time and SineTime fallbacks.")]
        [Min(0.01f)]
        [SerializeField] private float fallbackPeriod = 1f;

        [Header("Shape")]
        [Tooltip("Maps the source's normalized value (within inputRange) to a 0..1 lerp factor between the outputRange endpoints.")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("Portion of the source value that the curve operates on. Source values are remapped to 0..1 across this range.")]
        [SerializeField] private Vector2 inputRange = new Vector2(0f, 1f);
        [Tooltip("Output value range. The curve's 0..1 output is lerped (unclamped) between these endpoints.")]
        [SerializeField] private Vector2 outputRange = new Vector2(0f, 1f);

        [Header("Envelope")]
        [Tooltip("Output units gained per second when rising toward target. 0 = snap (no smoothing).")]
        [Min(0f)]
        [SerializeField] private float attack;
        [Tooltip("Output units lost per second when falling toward target. 0 = snap (no smoothing).")]
        [Min(0f)]
        [SerializeField] private float release;

        //==================== STATE =====================
        [Header("Debug")]
        // Renamed from "target" — a private debug field named "target" collided with the
        // "target" config field that component-driving subclasses (AudioDistortionBinding,
        // Volumyst*, Particle*, LightIntensityBinding, …) declare. Two serialized fields of
        // the same name in one hierarchy is unsupported by Unity serialization.
        [ReadOnly, SerializeField] private float targetValue;
        [ReadOnly, SerializeField] private float current;

        public virtual string Key => DefaultKey();
        public virtual string Description => "";
        public bool IsActive => isActiveAndEnabled;

        //==================== OUTPUTS =====================
        public event Action OnActivated;
        public event Action OnDeactivated;

        [Header("Events")]
        [Tooltip("Fired when the binding becomes enabled.")]
        [SerializeField] private UnityEvent activatedEvent;
        [Tooltip("Fired when the binding becomes disabled.")]
        [SerializeField] private UnityEvent deactivatedEvent;

        //==================== LIFECYCLE =====================
        protected virtual void Awake()
        {
            if (triggerEvent) triggerEvent.OnRaised += Activate;
            if (registry) registry.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (triggerEvent) triggerEvent.OnRaised -= Activate;
            if (registry) registry.Unregister(this);
        }

        protected virtual void OnEnable()
        {
            if (source)
            {
                source.OnChanged += HandleSourceChanged;
                targetValue = Shape(source.Value);
            }
            else
            {
                targetValue = Shape(ReadFallback());
            }
            current = targetValue;
            Apply(current);

            OnActivated?.Invoke();
            activatedEvent?.Invoke();
        }

        protected virtual void OnDisable()
        {
            if (source) source.OnChanged -= HandleSourceChanged;

            OnDeactivated?.Invoke();
            deactivatedEvent?.Invoke();
        }

        //==================== INPUTS =====================
        /// <summary>Enable the binding. Idempotent.</summary>
        [ContextMenu("Activate")]
        public void Activate() => enabled = true;

        /// <summary>Disable the binding. Idempotent.</summary>
        [ContextMenu("Deactivate")]
        public void Deactivate() => enabled = false;

        //==================== UPDATE =====================
        private void Update()
        {
            // Pull fallback per-frame — Time / SineTime / Random / Zero have no OnChanged.
            if (!source) targetValue = Shape(ReadFallback());

            if (Mathf.Approximately(current, targetValue))
            {
                Apply(current);
                return;
            }

            float rate = targetValue > current ? attack : release;
            current = rate <= 0f
                ? targetValue
                : Mathf.MoveTowards(current, targetValue, rate * DeltaTime);

            Apply(current);
        }

        /// <summary>Delta time used for the envelope step. Override to <see cref="Time.unscaledDeltaTime"/> when the binding drives timeScale or runs during pauses.</summary>
        protected virtual float DeltaTime => Time.deltaTime;

        //==================== PRIVATE =====================
        private void HandleSourceChanged(float value) => targetValue = Shape(value);

        private float Shape(float raw)
        {
            float t = inputRange.y > inputRange.x
                ? Mathf.InverseLerp(inputRange.x, inputRange.y, raw)
                : 0f;
            float shaped = curve.Evaluate(t);
            return Mathf.LerpUnclamped(outputRange.x, outputRange.y, shaped);
        }

        private float ReadFallback()
        {
            float p = Mathf.Max(0.01f, fallbackPeriod);
            return fallback switch
            {
                FallbackSource.Time     => (Time.time / p) % 1f,
                FallbackSource.SineTime => (Mathf.Sin(Time.time * (2f * Mathf.PI / p)) + 1f) * 0.5f,
                FallbackSource.Random   => UnityEngine.Random.value,
                _                       => 0f
            };
        }

        //==================== SUBCLASS API =====================
        /// <summary>Apply the shaped, enveloped output value to whatever target this binding drives (light, material, transform, etc.).</summary>
        protected abstract void Apply(float value);

        /// <summary>Default key derived from the class name — strips "NPC" prefix and "Binding" suffix, lowercases. e.g. LightIntensityBinding → "lightintensity".</summary>
        protected virtual string DefaultKey()
        {
            var n = GetType().Name;
            if (n.StartsWith("NPC")) n = n.Substring(3);
            if (n.EndsWith("Binding")) n = n.Substring(0, n.Length - 7);
            return n.ToLowerInvariant();
        }

        public enum FallbackSource
        {
            /// <summary>Sawtooth 0..1 over fallbackPeriod seconds.</summary>
            Time,
            /// <summary>Sinusoid 0..1 with period = fallbackPeriod seconds.</summary>
            SineTime,
            /// <summary>Random 0..1 each frame.</summary>
            Random,
            /// <summary>Constant 0 — useful as a no-op default while authoring.</summary>
            Zero
        }
    }
}
