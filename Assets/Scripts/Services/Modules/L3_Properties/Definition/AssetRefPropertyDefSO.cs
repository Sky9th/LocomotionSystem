using System;
using System.Collections.Generic;
using UnityEngine;

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
            if (value is UnityEngine.Object o) return o;
            if (value is string g && !string.IsNullOrEmpty(g)) return Load(g, AssetTypeConstraint);
            return null;
        }

        public static UnityEngine.Object Load(string guid, string typeConstraint)
        {
            if (string.IsNullOrEmpty(guid)) return null;
#if UNITY_EDITOR
            var ap = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(ap)) return null;
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ap);
            if (obj && !string.IsNullOrEmpty(typeConstraint))
            { var et = System.Type.GetType(typeConstraint); if (et != null && !et.IsInstanceOfType(obj)) { Debug.LogWarning("[PropertyTable] Asset type mismatch"); return null; } }
            return obj;
#else
            return null;
#endif
        }
    }
}
