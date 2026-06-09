using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RedDust.Properties
{
    /// <summary>
    /// 全局 PropertyDefinition 注册表。编辑器专有——运行时 Resolve 已完成，不需要它。
    /// </summary>
    public static class PropertyDefinitionRegistry
    {
        private static Dictionary<string, PropertyDefSO> _dict;
        private static bool _initialized;

        public static PropertyDefSO FindById(string id)
        {
            EnsureInitialized();
            _dict.TryGetValue(id, out var def);
            return def;
        }

        public static bool Contains(string id)
        {
            EnsureInitialized();
            return _dict.ContainsKey(id);
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _dict = new Dictionary<string, PropertyDefSO>();

#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:PropertyDefSO", new[] { "Assets/Data/Properties/Definitions" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<PropertyDefSO>(path);
                if (def != null && !string.IsNullOrEmpty(def.Id))
                {
                    if (_dict.ContainsKey(def.Id))
                    {
                        Debug.LogWarning($"[PropertyRegistry] Duplicate Id: {def.Id} at {path}");
                        continue;
                    }
                    _dict[def.Id] = def;
                }
            }
#endif
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
