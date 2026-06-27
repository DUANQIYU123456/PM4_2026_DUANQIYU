// ============================================================================
// SurfaceRenderer — abstract base for camera-rendered surfaces: a surface that
// displays a secondary camera's render of the scene. Concrete subclasses decide
// only WHERE the capture camera goes (and how it projects / culls):
//
//   PortalRenderer   — rigid re-emission through an in→out anchor pair (a door).
//   MirrorRenderer   — reflection across the surface plane (a true mirror).
//   MonitorRenderer  — a fixed camera elsewhere, shown UV-mapped (a CCTV screen).
//
// The base owns the invariant machinery, exactly like Spawner/Sensor/Recorder:
//   • a screen-sized RenderTexture (lifecycle + resolution scale),
//   • the per-frame render hook (only the player camera, never recursion),
//   • copying projection + an optional oblique near plane,
//   • pushing the RT onto the surface material via a MaterialPropertyBlock.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Ludocore
{
    /// <summary>Abstract base: drives a capture camera into a RenderTexture and
    /// displays it on a surface. Subclasses override the small hook set below to
    /// decide where the camera goes and how it projects.</summary>
    [ExecuteAlways]
    public abstract class SurfaceRenderer : MonoBehaviour
    {
        //==================== CAMERAS =====================
        [Header("Cameras")]
        [Tooltip("Main player camera whose render we react to. Falls back to Camera.main.")]
        [SerializeField] protected Camera playerCamera;

        [Tooltip("Disabled camera that renders into the surface RT each frame. No AudioListener.")]
        [FormerlySerializedAs("portalCamera")]
        [SerializeField] protected Camera captureCamera;

        //==================== SURFACE =====================
        [Header("Surface")]
        [Tooltip("The surface renderer that displays the RT (e.g. a quad).")]
        [FormerlySerializedAs("portalSurface")]
        [SerializeField] protected Renderer surface;

        [Tooltip("Texture property on the surface material that receives the RT.")]
        [SerializeField] protected string textureProperty = "_MainTex";

        //==================== RENDER TEXTURE =====================
        [Header("Render Texture")]
        [Range(0.25f, 1f)]
        [Tooltip("Resolution multiplier vs. screen size. 1 = native, 0.5 = half-res for cheaper rendering.")]
        [SerializeField] protected float resolutionScale = 1f;

        //==================== STATE =====================
        private RenderTexture _rt;
        private int _rtWidth;
        private int _rtHeight;
        private MaterialPropertyBlock _mpb;
        private int _texPropId;

        //==================== LIFECYCLE =====================
        protected virtual void OnEnable()
        {
            _texPropId = Shader.PropertyToID(textureProperty);
            _mpb ??= new MaterialPropertyBlock();

            if (captureCamera) captureCamera.enabled = false;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected virtual void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            ReleaseRT();
        }

        //==================== RENDER HOOK =====================
        private void OnBeginCameraRendering(ScriptableRenderContext _, Camera cam)
        {
            if (!Active) return;

            var pcam = ResolvePlayerCamera();
            if (cam != pcam) return;            // only react to the player camera
            if (cam == captureCamera) return;   // never recurse into ourselves

            EnsureRT();

            // 1. Where does the capture camera go this frame? (extrinsics)
            ComputeCameraPose(pcam, out var pos, out var rot);
            captureCamera.transform.SetPositionAndRotation(pos, rot);

            // 2. Projection (intrinsics): match the player, or keep our own lens.
            if (CopyPlayerProjection)
                captureCamera.projectionMatrix = pcam.projectionMatrix;

            // 3. Subclass may override the view matrix outright (mirrors need this —
            //    a reflection isn't a rigid transform).
            PostConfigure(pcam);

            // 4. Optional oblique near plane (clip geometry past the exit / surface).
            ApplyClipPlaneIfAny();

            // 5. Reflections reverse winding; flip culling around the render only.
            bool flip = FlipCulling;
            if (flip) GL.invertCulling = true;
            captureCamera.Render();
            if (flip) GL.invertCulling = false;

            // 6. Push the result onto the surface.
            ApplyToSurface();
        }

        //==================== SUBCLASS HOOKS =====================
        /// <summary>Where the capture camera should sit this frame, in world space.</summary>
        protected abstract void ComputeCameraPose(Camera playerCam, out Vector3 pos, out Quaternion rot);

        /// <summary>True = copy the player's projection (portal/mirror). False = keep the
        /// capture camera's own lens / FOV (monitor).</summary>
        protected virtual bool CopyPlayerProjection => true;

        /// <summary>True for reflections — flips triangle winding so the mirrored render
        /// isn't inside-out.</summary>
        protected virtual bool FlipCulling => false;

        /// <summary>Hook after pose + projection are set, before the oblique plane. Override
        /// to install a custom worldToCameraMatrix (mirrors).</summary>
        protected virtual void PostConfigure(Camera playerCam) { }

        /// <summary>Optional oblique near-clip plane in WORLD space. Return false for none.</summary>
        protected virtual bool TryGetClipPlane(out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = default;
            return false;
        }

        /// <summary>Base requires a camera + surface; subclasses can require more.</summary>
        protected virtual bool Active => captureCamera && surface;

        //==================== SHARED STEPS =====================
        private void ApplyClipPlaneIfAny()
        {
            if (!TryGetClipPlane(out var worldPoint, out var worldNormal)) return;

            Matrix4x4 worldToCam = captureCamera.worldToCameraMatrix;
            Vector3 nCam = worldToCam.MultiplyVector(worldNormal).normalized;
            Vector3 pCam = worldToCam.MultiplyPoint(worldPoint);
            Vector4 clipPlaneCam = new Vector4(nCam.x, nCam.y, nCam.z, -Vector3.Dot(nCam, pCam));

            captureCamera.projectionMatrix = captureCamera.CalculateObliqueMatrix(clipPlaneCam);
        }

        private void ApplyToSurface()
        {
            surface.GetPropertyBlock(_mpb);
            _mpb.SetTexture(_texPropId, _rt);
            surface.SetPropertyBlock(_mpb);
        }

        //==================== RT MANAGEMENT =====================
        private void EnsureRT()
        {
            int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * resolutionScale));
            int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

            if (_rt && _rtWidth == w && _rtHeight == h) return;

            ReleaseRT();
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR)
            {
                name = $"SurfaceRT_{name}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _rt.Create();
            _rtWidth = w;
            _rtHeight = h;

            captureCamera.targetTexture = _rt;
        }

        private void ReleaseRT()
        {
            if (!_rt) return;
            if (captureCamera && captureCamera.targetTexture == _rt) captureCamera.targetTexture = null;
            _rt.Release();
            if (Application.isPlaying) Destroy(_rt); else DestroyImmediate(_rt);
            _rt = null;
        }

        //==================== HELPERS =====================
        protected Camera ResolvePlayerCamera() => playerCamera ? playerCamera : Camera.main;
    }
}
