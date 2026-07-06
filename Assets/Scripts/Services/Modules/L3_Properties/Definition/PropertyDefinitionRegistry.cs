using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RedDust.Properties
{
    /// <summary>
    /// Global PropertyDefSO lookup. Populated by PropertyBootTask during boot preload
    /// via <see cref="Initialize"/>. Callers receive null if not yet initialized.
    /// </summary>
    public static class PropertyDefinitionRegistry
    {
        private static Dictionary<string, PropertyDefSO> _dict;
        private static bool _initialized;

        public static PropertyDefSO FindById(string id)
        {
            if (!_initialized)
            {
                Debug.LogWarning("[PropertyRegistry] Not initialized yet. Call Initialize() during preload first.");
                return null;
            }
            _dict.TryGetValue(id, out var def);
            return def;
        }

        public static bool Contains(string id)
        {
            if (!_initialized) return false;
            return _dict.ContainsKey(id);
        }

        /// <summary>
        /// Populate the registry at runtime. Called once by LoadingOrchestrator during preload.
        /// Idempotent — clears and rebuilds on subsequent calls (safe for domain reload).
        /// </summary>
        public static void Initialize(List<PropertyDefSO> defs)
        {
            _dict = new Dictionary<string, PropertyDefSO>();
            _initialized = true;

            if (defs == null) return;

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                if (_dict.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"[PropertyRegistry] Duplicate Id: {def.Id} — skipping.");
                    continue;
                }
                _dict[def.Id] = def;
            }
        }

#if UNITY_EDITOR
        /// <summary>编辑器下强制重建（资产变更后调用）。</summary>
        public static void Invalidate()
        {
            _initialized = false;
            _dict = null;
        }
#endif
    }
}
