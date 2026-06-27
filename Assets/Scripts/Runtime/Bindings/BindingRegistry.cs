using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// SO-channel registry for <see cref="IBinding"/>. Drop the asset into a Binding's <c>registry</c> slot
    /// to make it discoverable by <see cref="IBinding.Key"/>. Same SO can hold many bindings (broadcast),
    /// and many SOs can coexist for scoped vocabularies (per-NPC, per-zone, per-scene, etc.).
    /// Runtime state — clears on asset load so previous play sessions don't leak.
    /// Iterating <see cref="All"/> exposes the live collection — snapshot via .ToArray() if you may mutate the registry mid-loop.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBindingRegistry", menuName = "Ludocore/Registries/Binding Registry")]
    public class BindingRegistry : ScriptableObject
    {
        //==================== STATE =====================
        private readonly HashSet<IBinding> _all = new();
        private readonly Dictionary<string, List<IBinding>> _byKey = new();
        private readonly Dictionary<IBinding, string> _registeredKey = new();

        //==================== OUTPUTS =====================
        public event Action<IBinding> OnRegistered;
        public event Action<IBinding> OnUnregistered;

        //==================== QUERIES =====================
        public IReadOnlyCollection<IBinding> All => _all;
        public IEnumerable<string> Keys => _byKey.Keys;
        public int Count => _all.Count;

        /// <summary>True if at least one Binding with this key is registered.</summary>
        public bool Has(string key) => !string.IsNullOrEmpty(key) && _byKey.ContainsKey(key);

        /// <summary>All Bindings registered under this key. Empty list if none.</summary>
        public IReadOnlyList<IBinding> Get(string key)
            => !string.IsNullOrEmpty(key) && _byKey.TryGetValue(key, out var list)
                ? list
                : Array.Empty<IBinding>();

        /// <summary>First Binding registered under this key, or null.</summary>
        public IBinding GetFirst(string key)
        {
            var list = Get(key);
            return list.Count > 0 ? list[0] : null;
        }

        //==================== INPUTS =====================
        /// <summary>Called by <see cref="Binding"/> in Awake. Idempotent — registering the same instance twice is a no-op.</summary>
        public void Register(IBinding binding)
        {
            if (binding == null) return;
            if (!_all.Add(binding)) return;

            string key = binding.Key ?? string.Empty;
            _registeredKey[binding] = key;
            if (!_byKey.TryGetValue(key, out var list))
                _byKey[key] = list = new List<IBinding>();
            list.Add(binding);

            OnRegistered?.Invoke(binding);
        }

        /// <summary>Called by <see cref="Binding"/> in OnDestroy. Idempotent.</summary>
        public void Unregister(IBinding binding)
        {
            if (binding == null) return;
            if (!_all.Remove(binding)) return;

            if (_registeredKey.TryGetValue(binding, out var key))
            {
                _registeredKey.Remove(binding);
                if (_byKey.TryGetValue(key, out var list))
                {
                    list.Remove(binding);
                    if (list.Count == 0) _byKey.Remove(key);
                }
            }

            OnUnregistered?.Invoke(binding);
        }

        //==================== LIFECYCLE =====================
        // Clears runtime state and stale subscribers on asset load — same pattern as FloatVariable.
        private void OnEnable()
        {
            _all.Clear();
            _byKey.Clear();
            _registeredKey.Clear();
            OnRegistered = null;
            OnUnregistered = null;
        }
    }
}
