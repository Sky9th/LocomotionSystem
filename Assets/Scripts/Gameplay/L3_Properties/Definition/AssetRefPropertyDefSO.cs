using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "AssetRefDefinition", menuName = "RedDust/Properties/Asset Ref Definition")]
    public class AssetRefPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.AssetRef; }

        [Header("Default")] public string DefaultAssetGUID;
        [Header("Constraint")] public string AssetTypeConstraint;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return Load(DefaultAssetGUID, AssetTypeConstraint);
            if (isRaw) return Resolve((string)rawValue);
            return Resolve(rawValue);
        }

        private UnityEngine.Object Resolve(object value)
        {
            if (value is UnityEngine.Object o)
            {
                if (!string.IsNullOrEmpty(AssetTypeConstraint))
                {
                    var et = System.Type.GetType(AssetTypeConstraint);
                    if (et != null && !et.IsInstanceOfType(o))
                    { Debug.LogWarning($"[PropertyTable] AssetRef type mismatch: expected {AssetTypeConstraint}"); return null; }
                }
                return o;
            }
            if (value is string g && !string.IsNullOrEmpty(g)) return Load(g, AssetTypeConstraint);
            return null;
        }

        private static readonly Dictionary<string, UnityEngine.Object> _runtimeCache = new();

        public static UnityEngine.Object Load(string guid, string typeConstraint)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            // cache hit
            if (_runtimeCache.TryGetValue(guid, out var cached))
                return cached;

            // Addressables (Editor + Build + Mod catalogs)
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Object>(guid);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                var obj = handle.Result;
                if (!string.IsNullOrEmpty(typeConstraint))
                {
                    var et = System.Type.GetType(typeConstraint);
                    if (et != null && !et.IsInstanceOfType(obj))
                    { Debug.LogWarning($"[PropertyTable] AssetRef type mismatch: expected {typeConstraint}"); return null; }
                }
                _runtimeCache[guid] = obj;
                return obj;
            }

#if UNITY_EDITOR
            // Editor fallback: AssetDatabase for new assets not yet in built catalog
            var ap = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(ap))
            {
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ap);
                if (obj && !string.IsNullOrEmpty(typeConstraint))
                { var et = System.Type.GetType(typeConstraint); if (et != null && !et.IsInstanceOfType(obj)) { Debug.LogWarning("[PropertyTable] Asset type mismatch"); return null; } }
                _runtimeCache[guid] = obj;
                return obj;
            }
#endif

            Debug.LogWarning($"[PropertyTable] AssetRef load failed for GUID '{guid}'");
            return null;
        }

        [Serializable] public struct JsonData { public string id, description, defaultAssetGUID, assetTypeConstraint; public bool isDeprecated; }
    }
}
