using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Ludocore
{
    /// <summary>Owns the shared lighting of the space and remembers its state.
    /// Exposes two high-level operations — Dim() and Brighten() — that drive ceiling emission,
    /// ambient intensity, reflection intensity, skybox exposure, and directional light intensity
    /// together, using values and timing from a swappable LightManagerProfile.
    ///
    /// Controllers don't tween lights themselves — they call Dim/Brighten. The manager is the
    /// single author of the lighting state; the state persists regardless of which controller
    /// triggered the last transition ("the space remembers").
    ///
    /// Per-call profile override: any caller may pass a LightManagerProfile to Dim/Brighten to
    /// retime/retune ONE transition without changing the default. Useful when one effect (e.g.
    /// a teleport blackout) needs different timing than the global lighting feel.</summary>
    public class LightManager : MonoBehaviour
    {
        public enum LightState { Lit, Dim }

        public static LightManager Instance { get; private set; }

        //==================== SCENE REFERENCES =====================
        [Header("Shared Ceiling Material")]
        [Tooltip("Ceiling material whose emission is driven by the manager")]
        [SerializeField] private Material ceilingMaterial;

        [Tooltip("Base emission color — final emission = color * 2^value")]
        [SerializeField, ColorUsage(false, true)]
        private Color ceilingEmissionColor = Color.white;

        [Header("Directional Light")]
        [Tooltip("Directional light whose intensity is driven by the manager")]
        [SerializeField] private Light directionalLight;

        //==================== PROFILE =====================
        [Header("Profile")]
        [Tooltip("Scriptable object defining lit/dim values and transition timing")]
        [SerializeField] private LightManagerProfile profile;

        //==================== EVENTS =====================
        [Header("Events")]
        [Tooltip("Inspector-wireable. Fires when the manager begins transitioning to Dim.")]
        [SerializeField] private UnityEvent onDimmed;

        [Tooltip("Inspector-wireable. Fires when the manager begins transitioning to Lit.")]
        [SerializeField] private UnityEvent onBrightened;

        /// <summary>Code-wireable. Fires when state flips, BEFORE the tween starts —
        /// so subscribers can coordinate their own transitions in lockstep.</summary>
        public event System.Action<LightState> StateChanged;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private LightState currentState = LightState.Lit;

        public LightState Current => currentState;
        public bool IsLit => currentState == LightState.Lit;
        public bool IsDim => currentState == LightState.Dim;

        private float _emission;
        private float _ambient;
        private float _reflection;
        private float _skybox;
        private float _directional;
        private Tween _emissionTween;
        private Tween _ambientTween;
        private Tween _reflectionTween;
        private Tween _skyboxTween;
        private Tween _directionalTween;

        //==================== LIFECYCLE =====================
        private void Awake()
        {
            if (Instance is not null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (ceilingMaterial) ceilingMaterial.EnableKeyword("_EMISSION");
        }

        private void Start()
        {
            if (!profile) return;
            SnapToLit();
        }

        private void OnDestroy()
        {
            KillTweens();
            if (Instance == this) Instance = null;
        }

        //==================== API =====================
        /// <summary>Transition every lighting channel to its Dim value, using the default profile. No-op if already dim.</summary>
        [ContextMenu("Dim")]
        public void Dim() => Dim(null);

        /// <summary>Transition to Dim using <paramref name="overrideProfile"/> if provided, otherwise the default profile.
        /// Use this when a caller (e.g. a teleport blackout) needs different timing than the global lighting state.
        /// No-op if already dim.</summary>
        public void Dim(LightManagerProfile overrideProfile)
        {
            var p = overrideProfile ? overrideProfile : profile;
            if (!p || currentState == LightState.Dim) return;
            currentState = LightState.Dim;
            StateChanged?.Invoke(currentState);
            onDimmed?.Invoke();
            TweenAll(
                p.emissionDim, p.ambientDim, p.reflectionDim,
                p.skyboxExposureDim, p.directionalDim,
                p.dimDuration, p.dimCurve, p.dimDelay
            );
        }

        /// <summary>Transition every lighting channel to its Lit value, using the default profile. No-op if already lit.</summary>
        [ContextMenu("Brighten")]
        public void Brighten() => Brighten(null);

        /// <summary>Transition to Lit using <paramref name="overrideProfile"/> if provided, otherwise the default profile.
        /// No-op if already lit.</summary>
        public void Brighten(LightManagerProfile overrideProfile)
        {
            var p = overrideProfile ? overrideProfile : profile;
            if (!p || currentState == LightState.Lit) return;
            currentState = LightState.Lit;
            StateChanged?.Invoke(currentState);
            onBrightened?.Invoke();
            TweenAll(
                p.emissionLit, p.ambientLit, p.reflectionLit,
                p.skyboxExposureLit, p.directionalLit,
                p.brightenDuration, p.brightenCurve, 0f
            );
        }

        //==================== PRIVATE =====================
        private void SnapToLit()
        {
            ApplyEmission(profile.emissionLit);
            ApplyAmbient(profile.ambientLit);
            ApplyReflection(profile.reflectionLit);
            ApplySkybox(profile.skyboxExposureLit);
            ApplyDirectional(profile.directionalLit);
            currentState = LightState.Lit;
        }

        private void TweenAll(float emission, float ambient, float reflection,
                              float skybox, float directional,
                              float duration, AnimationCurve curve, float delay)
        {
            KillTweens();

            _emissionTween = DOTween.To(() => _emission, ApplyEmission, emission, duration)
                .SetDelay(delay).SetEase(curve);

            _ambientTween = DOTween.To(() => _ambient, ApplyAmbient, ambient, duration)
                .SetDelay(delay).SetEase(curve);

            _reflectionTween = DOTween.To(() => _reflection, ApplyReflection, reflection, duration)
                .SetDelay(delay).SetEase(curve);

            _skyboxTween = DOTween.To(() => _skybox, ApplySkybox, skybox, duration)
                .SetDelay(delay).SetEase(curve);

            _directionalTween = DOTween.To(() => _directional, ApplyDirectional, directional, duration)
                .SetDelay(delay).SetEase(curve);
        }

        private void ApplyEmission(float value)
        {
            _emission = value;
            if (!ceilingMaterial) return;
            ceilingMaterial.SetColor("_EmissionColor", ceilingEmissionColor * Mathf.Pow(2f, value));
        }

        private void ApplyAmbient(float value)
        {
            _ambient = value;
            RenderSettings.ambientIntensity = value;
        }

        private void ApplyReflection(float value)
        {
            _reflection = value;
            RenderSettings.reflectionIntensity = value;
        }

        private void ApplySkybox(float value)
        {
            _skybox = value;
            if (RenderSettings.skybox) RenderSettings.skybox.SetFloat("_Exposure", value);
        }

        private void ApplyDirectional(float value)
        {
            _directional = value;
            if (directionalLight) directionalLight.intensity = value;
        }

        private void KillTweens()
        {
            _emissionTween?.Kill();
            _ambientTween?.Kill();
            _reflectionTween?.Kill();
            _skyboxTween?.Kill();
            _directionalTween?.Kill();
        }
    }
}
