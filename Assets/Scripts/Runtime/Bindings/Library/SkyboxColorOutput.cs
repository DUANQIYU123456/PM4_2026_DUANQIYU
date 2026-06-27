using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// Drives a color parameter of a "Skybox/Procedural" material — Sky Tint or Ground Color — by lerping
    /// between two colors with the 0..1 master signal. Targets the assigned skybox Material, or
    /// RenderSettings.skybox when none is set. This edits the material ASSET (skybox materials are not
    /// instanced), so the value is captured and reverted on play-exit by BindingResetHook.
    /// </summary>
    [BindMenu("Skybox/Procedural Color")]
    [System.Serializable]
    public class SkyboxColorOutput : BindTarget
    {
        public enum Param { SkyTint, GroundColor }

        [Tooltip("Skybox material to drive (a Skybox/Procedural material). Empty = RenderSettings.skybox.")]
        [SerializeField] private Material skybox;
        [Tooltip("Which procedural-skybox color to drive.")]
        [SerializeField] private Param param = Param.SkyTint;
        [Tooltip("Color at signal 0 (after this card's curve / output range).")]
        [SerializeField] private Color colorA = new Color(0.5f, 0.5f, 0.5f);
        [Tooltip("Color at signal 1 (after this card's curve / output range).")]
        [SerializeField] private Color colorB = Color.white;

        private Material _mat;
        private Color _original;

        public override string DisplayName => "Skybox Color";
        public override Color CardColor => new Color(0.55f, 0.75f, 0.95f);
        public override bool AltersState => true;

        private string Prop => param == Param.SkyTint ? "_SkyTint" : "_GroundColor";

        protected override void OnCapture()
        {
            _mat = skybox ? skybox : RenderSettings.skybox;
            if (_mat && _mat.HasProperty(Prop)) _original = _mat.GetColor(Prop);
        }

        protected override void OnRestore() { if (_mat && _mat.HasProperty(Prop)) _mat.SetColor(Prop, _original); }

        protected override void OnApply(float value)
        {
            if (_mat && _mat.HasProperty(Prop)) _mat.SetColor(Prop, Color.LerpUnclamped(colorA, colorB, value));
        }
    }
}
