using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Teleports a mannequin within a bounding box and brightens the
    /// ceiling tile(s) directly above it, restoring prior emission on move.</summary>
    public class ManequinController : MonoBehaviour
    {
        //==================== SCENE REFERENCES =====================
        [Header("Scene References")]
        [Tooltip("BoxCollider that defines the teleport region (XZ sampled, mannequin Y preserved)")]
        [SerializeField] private BoxCollider bounds;

        //==================== PROFILE =====================
        [Header("Profile")]
        [SerializeField] private ManequinProfile profile;

        //==================== EVENTS =====================
        [Header("Events")]
        [SerializeField] private UnityEvent onTeleport;

        //==================== STATE =====================
        private Coroutine _timerRoutine;
        private readonly List<(Material material, Color previousEmission)> _lit = new();
        private static readonly Collider[] _hitBuffer = new Collider[32];

        //==================== LIFECYCLE =====================
        private void OnEnable()
        {
            if (profile && profile.useTimer)
                _timerRoutine = StartCoroutine(TimerLoop());
        }

        private void OnDisable()
        {
            StopTimer();
            ReleaseLit();
        }

        private void OnDestroy()
        {
            ReleaseLit();
        }

        //==================== INPUTS =====================
        /// <summary>Release currently lit tiles, pick a random point in the bounds,
        /// teleport, and light the tile(s) above.</summary>
        [ContextMenu("Teleport")]
        public void Teleport()
        {
            if (!profile) return;

            ReleaseLit();

            if (bounds) MoveWithinBounds();
            ApplyRandomYaw();
            LightTilesAbove();

            onTeleport?.Invoke();
        }

        //==================== PRIVATE =====================
        private IEnumerator TimerLoop()
        {
            while (true)
            {
                float min = Mathf.Min(profile.minInterval, profile.maxInterval);
                float max = Mathf.Max(profile.minInterval, profile.maxInterval);
                yield return new WaitForSeconds(Random.Range(min, max));
                Teleport();
            }
        }

        private void StopTimer()
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }
        }

        private void MoveWithinBounds()
        {
            Vector3 local = bounds.center + new Vector3(
                (Random.value - 0.5f) * bounds.size.x,
                0f,
                (Random.value - 0.5f) * bounds.size.z
            );
            Vector3 world = bounds.transform.TransformPoint(local);
            world.y = transform.position.y;
            transform.position = world;
        }

        private void ApplyRandomYaw()
        {
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        private void LightTilesAbove()
        {
            Vector3 center = transform.position + new Vector3(
                0f,
                profile.verticalOffset + profile.detectionHeight * 0.5f,
                0f
            );
            Vector3 halfExtents = new Vector3(
                profile.footprintSize.x * 0.5f,
                profile.detectionHeight * 0.5f,
                profile.footprintSize.y * 0.5f
            );

            int count = Physics.OverlapBoxNonAlloc(
                center, halfExtents, _hitBuffer,
                Quaternion.identity, profile.layerMask,
                QueryTriggerInteraction.Ignore
            );

            var seen = new HashSet<Renderer>();
            for (int i = 0; i < count; i++)
            {
                var col = _hitBuffer[i];
                if (!col) continue;

                var rend = col.GetComponent<Renderer>() ?? col.GetComponentInParent<Renderer>();
                if (!rend || !seen.Add(rend)) continue;

                var mat = rend.material;
                if (!mat) continue;

                mat.EnableKeyword("_EMISSION");
                Color prev = mat.HasProperty("_EmissionColor")
                    ? mat.GetColor("_EmissionColor")
                    : Color.black;
                Color next = profile.emissionColor * Mathf.Pow(2f, profile.brightIntensity);
                mat.SetColor("_EmissionColor", next);
                _lit.Add((mat, prev));
            }
        }

        private void ReleaseLit()
        {
            for (int i = 0; i < _lit.Count; i++)
            {
                var (mat, prev) = _lit[i];
                if (mat) mat.SetColor("_EmissionColor", prev);
            }
            _lit.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            if (!profile) return;

            Vector3 center = transform.position + new Vector3(
                0f,
                profile.verticalOffset + profile.detectionHeight * 0.5f,
                0f
            );
            Vector3 size = new Vector3(
                profile.footprintSize.x,
                profile.detectionHeight,
                profile.footprintSize.y
            );

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
