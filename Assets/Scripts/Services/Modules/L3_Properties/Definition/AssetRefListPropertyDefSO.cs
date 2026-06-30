using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "AssetRefListDefinition", menuName = "RedDust/Properties/Asset Ref List Definition")]
    public class AssetRefListPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.AssetRefList; }

        [Header("Constraint")] public string AssetTypeConstraint;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return Array.Empty<UnityEngine.Object>();
            if (isRaw) return ResolveList((string)rawValue);
            return ResolveList(rawValue);
        }

        private UnityEngine.Object[] ResolveList(object value)
        {
            if (value is UnityEngine.Object[] oa) return oa;
            if (value is string[] ga)
            {
                var r = new List<UnityEngine.Object>();
                foreach (var g in ga) { var o = AssetRefPropertyDefSO.Load(g, AssetTypeConstraint); if (o) r.Add(o); else Debug.LogWarning($"[PropertyTable] AssetRefList: GUID '{g}' failed to resolve"); }
                return r.ToArray();
            }
            return Array.Empty<UnityEngine.Object>();
        }
    }
}
