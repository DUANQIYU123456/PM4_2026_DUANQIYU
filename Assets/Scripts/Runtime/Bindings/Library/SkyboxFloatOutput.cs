using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Drives one scalar parameter of a "Skybox/Procedural" material — Exposure, Atmosphere Thickness,
    /// Sun Size, or Sun Size Convergence. Targets the assigned skybox Material, or RenderSettings.skybox
    /// when none is set. This edits the material ASSET (skybox materials are not instanced), so the value is
    /// captured and reverted on play-exit by BindingResetHook. Add one card per parameter you want to drive.
    /// </summary>
    [BindMenu("Skybox/Procedural Float")]
    [System.Serializable]
    public class SkyboxFloatOutput : BindTarget
    {
        public enum Param { Exposure, AtmosphereThickness, SunSize, SunSizeConvergence }

        [Tooltip("Skybox material to drive (a Skybox/Procedural material). Empty = RenderSettings.skybox.")]
        [SerializeField] private Material skybox;
        [Tooltip("Which procedural-skybox scalar to drive. Native ranges: Exposure 0..8, Atmosphere 0..5, " +
                 "Sun Size 0..1, Sun Convergence 1..10. Set this card's output range to match.")]
        [SerializeField] private Param param = Param.Exposure;

        private Material _mat;
        private float _original;

        public SkyboxFloatOutput() : base(0f, 2f) { }

        public override string DisplayName => "Skybox Float";
        public override Color CardColor => new Color(0.55f, 0.75f, 0.95f);
        public override bool AltersState => true;

        private string Prop => param switch
        {
            Param.Exposure            => "_Exposure",
            Param.AtmosphereThickness => "_AtmosphereThickness",
            Param.SunSize             => "_SunSize",
            Param.SunSizeConvergence  => "_SunSizeConvergence",
            _                         => "_Exposure"
        };

        protected override void OnCapture()
        {
            _mat = skybox ? skybox : RenderSettings.skybox;
            if (_mat && _mat.HasProperty(Prop)) _original = _mat.GetFloat(Prop);
        }

        protected override void OnRestore() { if (_mat && _mat.HasProperty(Prop)) _mat.SetFloat(Prop, _original); }

        protected override void OnApply(float value)
        {
            if (_mat && _mat.HasProperty(Prop)) _mat.SetFloat(Prop, value);
        }
    }
}
