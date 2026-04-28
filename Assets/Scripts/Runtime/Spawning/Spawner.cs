using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Instantiates prefabs at a point or within a randomized area.</summary>
    public class Spawner : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Prefab to instantiate when spawning.")]
        [SerializeField] private GameObject prefab;
        [Tooltip("Number of instances to create per Spawn call.")]
        [Min(0)]
        [SerializeField] private int count = 1;

        [Header("Position")]
        [Tooltip("Where to spawn. Uses this transform if empty.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Randomization area around spawn point. Zero = exact point.")]
        [SerializeField] private Vector3 areaSize;

        [Header("Rotation")]
        [Tooltip("Use the spawn point's rotation instead of identity.")]
        [SerializeField] private bool useSpawnRotation;
        [Tooltip("Apply a fully random rotation to each spawned instance.")]
        [SerializeField] private bool randomRotation;

        [Header("Parenting")]
        [Tooltip("Optional parent transform for spawned instances.")]
        [SerializeField] private Transform parent;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private int totalSpawned;

        public int TotalSpawned => totalSpawned;
        public GameObject LastSpawned { get; private set; }

        //==================== OUTPUTS =====================
        public event Action<GameObject> OnSpawned;

        [Header("Events")]
        [Tooltip("Fired after each instance is spawned, passes the new GameObject.")]
        [SerializeField] private UnityEvent<GameObject> spawnedEvent;

        //==================== INPUTS =====================
        /// <summary>Spawn the configured number of instances.</summary>
        [ContextMenu("Spawn")]
        public void Spawn()
        {
            if (!prefab) return;

            for (int i = 0; i < count; i++)
                SpawnOne();
        }

        /// <summary>Spawn a single instance, ignoring count.</summary>
        public void SpawnOne()
        {
            if (!prefab) return;

            var instance = Instantiate(prefab, GetPosition(), GetRotation(), parent);
            FixSelfReferences(instance, prefab);
            LastSpawned = instance;
            totalSpawned++;

            OnSpawned?.Invoke(instance);
            spawnedEvent?.Invoke(instance);
        }

        /// <summary>Spawn a specific prefab, overriding the configured one.</summary>
        public void Spawn(GameObject overridePrefab)
        {
            if (!overridePrefab) return;

            var instance = Instantiate(overridePrefab, GetPosition(), GetRotation(), parent);
            FixSelfReferences(instance, overridePrefab);
            LastSpawned = instance;
            totalSpawned++;

            OnSpawned?.Invoke(instance);
            spawnedEvent?.Invoke(instance);
        }

        //==================== PRIVATE =====================
        /// <summary>When a prefab's Spawner references itself, Instantiate remaps that
        /// self-reference to the new instance. This restores it to the original prefab
        /// so replication can chain indefinitely.</summary>
        private static void FixSelfReferences(GameObject instance, GameObject sourcePrefab)
        {
            foreach (var spawner in instance.GetComponentsInChildren<Spawner>())
            {
                if (spawner.prefab == instance)
                    spawner.prefab = sourcePrefab;
            }
        }

        private Vector3 GetPosition()
        {
            var origin = spawnPoint ? spawnPoint.position : transform.position;

            if (areaSize == Vector3.zero) return origin;

            return origin + new Vector3(
                UnityEngine.Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                UnityEngine.Random.Range(-areaSize.y / 2f, areaSize.y / 2f),
                UnityEngine.Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );
        }

        private Quaternion GetRotation()
        {
            if (randomRotation) return UnityEngine.Random.rotation;
            if (useSpawnRotation && spawnPoint) return spawnPoint.rotation;
            return Quaternion.identity;
        }

        //==================== GIZMOS =====================
        private void OnDrawGizmosSelected()
        {
            var origin = spawnPoint ? spawnPoint.position : transform.position;

            if (areaSize != Vector3.zero)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
                Gizmos.DrawCube(origin, areaSize);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(origin, areaSize);
            }
            else
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(origin, 0.15f);
            }
        }
    }
}
