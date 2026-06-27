using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ludocore
{
    /// <summary>Abstract base for all spawners — owns instantiation, parenting, events. Subclasses define WHERE to spawn via GetPosition/GetRotation.</summary>
    public abstract class Spawner : MonoBehaviour
    {
        //==================== CONFIG =====================
        [Header("Config")]
        [Tooltip("Prefab to instantiate when spawning.")]
        [SerializeField] private GameObject prefab;

        [Tooltip("Number of instances to create per Spawn call.")]
        [Min(0)]
        [SerializeField] private int count = 1;

        [Header("Parenting")]
        [Tooltip("Optional parent transform for spawned instances.")]
        [SerializeField] private Transform parent;

        [Header("Rate")]
        [Tooltip("Minimum seconds between Spawn() calls. 0 = no gate.")]
        [Min(0f)]
        [SerializeField] private float cooldown;

        [Header("Auto")]
        [Tooltip("Play automatically when enabled")]
        [SerializeField] private bool autoPlay;

        [Header("Trigger")]
        [Tooltip("Optional event channel. When raised, Spawn() fires. Multiple spawners can subscribe to the same channel — broadcast.")]
        [SerializeField] private GameEvent triggerEvent;

        //==================== STATE =====================
        [Header("Debug")]
        [ReadOnly, SerializeField] private int totalSpawned;

        private float _lastTime = -Mathf.Infinity;

        public int TotalSpawned => totalSpawned;
        public GameObject LastSpawned { get; private set; }
        public bool IsReady => Time.time - _lastTime >= cooldown;
        public float CooldownRatio => cooldown > 0f
            ? Mathf.Clamp01((Time.time - _lastTime) / cooldown) : 1f;

        //==================== OUTPUTS =====================
        public event Action<GameObject> OnSpawned;

        [Header("Events")]
        [Tooltip("Fired after each instance is spawned, passes the new GameObject.")]
        [SerializeField] private UnityEvent<GameObject> spawnedEvent;

        //==================== LIFECYCLE =====================
        protected virtual void OnEnable()
        {
            if (triggerEvent) triggerEvent.OnRaised += Spawn;
            if (autoPlay) Spawn();
        }

        protected virtual void OnDisable()
        {
            if (triggerEvent) triggerEvent.OnRaised -= Spawn;
        }

        //==================== INPUTS =====================
        /// <summary>Spawn the configured number of instances if the cooldown is ready.</summary>
        [ContextMenu("Spawn")]
        public void Spawn() => TrySpawn();

        /// <summary>Try to spawn the configured number. Returns false if still on cooldown or no prefab.</summary>
        public bool TrySpawn()
        {
            if (!prefab) return false;
            if (!IsReady) return false;
            _lastTime = Time.time;

            for (int i = 0; i < count; i++)
                SpawnOne();
            return true;
        }

        /// <summary>Spawn a single instance, ignoring count and cooldown.</summary>
        public void SpawnOne()
        {
            if (!prefab) return;
            Emit(prefab, GetPosition(), GetRotation());
        }

        /// <summary>Spawn a specific prefab, ignoring count and cooldown.</summary>
        public void Spawn(GameObject overridePrefab)
        {
            if (!overridePrefab) return;
            Emit(overridePrefab, GetPosition(), GetRotation());
        }

        //==================== PRIVATE =====================
        /// <summary>Subclasses override to decide where instances spawn.</summary>
        protected virtual Vector3 GetPosition() => transform.position;

        /// <summary>Subclasses override to decide orientation.</summary>
        protected virtual Quaternion GetRotation() => transform.rotation;

        private void Emit(GameObject p, Vector3 pos, Quaternion rot)
        {
            var instance = Instantiate(p, pos, rot, parent);
            LastSpawned = instance;
            totalSpawned++;

            OnSpawned?.Invoke(instance);
            spawnedEvent?.Invoke(instance);
        }
    }
}
