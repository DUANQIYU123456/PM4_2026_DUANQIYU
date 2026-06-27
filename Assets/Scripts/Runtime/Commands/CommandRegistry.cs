using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ludocore
{
    /// <summary>
    /// SO-channel registry for <see cref="ICommand"/>. Drop the asset into a Command's <c>registry</c> slot
    /// to make it discoverable by <see cref="ICommand.Key"/>. Same SO can hold many commands (broadcast),
    /// and many SOs can coexist for scoped vocabularies (per-NPC, per-zone, per-scene, etc.).
    /// Runtime state — clears on asset load so previous play sessions don't leak.
    /// Iterating <see cref="All"/> exposes the live collection — snapshot via .ToArray() if you may mutate the registry mid-loop.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCommandRegistry", menuName = "Ludocore/Registries/Command Registry")]
    public class CommandRegistry : ScriptableObject
    {
        //==================== STATE =====================
        private readonly HashSet<ICommand> _all = new();
        private readonly Dictionary<string, List<ICommand>> _byKey = new();
        private readonly Dictionary<ICommand, string> _registeredKey = new();

        //==================== OUTPUTS =====================
        public event Action<ICommand> OnRegistered;
        public event Action<ICommand> OnUnregistered;

        //==================== QUERIES =====================
        public IReadOnlyCollection<ICommand> All => _all;
        public IEnumerable<string> Keys => _byKey.Keys;
        public int Count => _all.Count;

        /// <summary>True if at least one Command with this key is registered.</summary>
        public bool Has(string key) => !string.IsNullOrEmpty(key) && _byKey.ContainsKey(key);

        /// <summary>All Commands registered under this key. Empty list if none.</summary>
        public IReadOnlyList<ICommand> Get(string key)
            => !string.IsNullOrEmpty(key) && _byKey.TryGetValue(key, out var list)
                ? list
                : Array.Empty<ICommand>();

        /// <summary>First Command registered under this key, or null.</summary>
        public ICommand GetFirst(string key)
        {
            var list = Get(key);
            return list.Count > 0 ? list[0] : null;
        }

        //==================== INPUTS =====================
        /// <summary>Called by <see cref="Command"/> in Awake. Idempotent — registering the same instance twice is a no-op.</summary>
        public void Register(ICommand command)
        {
            if (command == null) return;
            if (!_all.Add(command)) return;

            string key = command.Key ?? string.Empty;
            _registeredKey[command] = key;
            if (!_byKey.TryGetValue(key, out var list))
                _byKey[key] = list = new List<ICommand>();
            list.Add(command);

            OnRegistered?.Invoke(command);
        }

        /// <summary>Called by <see cref="Command"/> in OnDestroy. Idempotent.</summary>
        public void Unregister(ICommand command)
        {
            if (command == null) return;
            if (!_all.Remove(command)) return;

            if (_registeredKey.TryGetValue(command, out var key))
            {
                _registeredKey.Remove(command);
                if (_byKey.TryGetValue(key, out var list))
                {
                    list.Remove(command);
                    if (list.Count == 0) _byKey.Remove(key);
                }
            }

            OnUnregistered?.Invoke(command);
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
