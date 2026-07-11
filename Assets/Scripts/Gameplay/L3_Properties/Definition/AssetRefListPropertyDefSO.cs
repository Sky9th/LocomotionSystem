using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDust.Gameplay.Properties
{
    [CreateAssetMenu(fileName = "AssetRefListDefinition", menuName = "RedDust/Properties/Asset Ref List Definition")]
    public class AssetRefListPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.AssetRefList; }

        [Header("Constraint")] public string AssetTypeConstraint;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return Array.Empty<UnityEngine.Object>();
            if (isRaw) return ResolveGuids(ParseGuidList((string)rawValue));
            return ResolveGuids(rawValue);
        }

        /// <summary>将 JSON 数组字符串 "[...]" 反序列化为 GUID 数组后逐条 Load。</summary>
        private UnityEngine.Object[] ResolveGuids(object value)
        {
            if (value is UnityEngine.Object[] oa) return oa;
            if (value is string[] ga)
            {
                var r = new List<UnityEngine.Object>();
                foreach (var g in ga)
                {
                    var o = AssetRefPropertyDefSO.Load(g, AssetTypeConstraint);
                    if (o) r.Add(o);
                    else Debug.LogWarning($"[PropertyTable] AssetRefList: GUID '{g}' failed to resolve");
                }
                return r.ToArray();
            }
            return Array.Empty<UnityEngine.Object>();
        }

        /// <summary>
        /// 解析 OverridesJson 中的 GUID 数组字符串。
        /// 格式与 RdTagList 一致：JsonUtility.FromJson 包裹在 Items 键下。
        /// </summary>
        private static string[] ParseGuidList(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try { return JsonUtility.FromJson<GuidListWrap>($"{{\"Items\":{raw}}}")?.Items ?? Array.Empty<string>(); }
            catch (Exception e) { Debug.LogWarning($"[PropertyTable] Parse AssetRefList: {e.Message}"); return Array.Empty<string>(); }
        }

        [Serializable] private class GuidListWrap { public string[] Items; }

        [Serializable] public struct JsonData { public string id, description, assetTypeConstraint; public bool isDeprecated; }
    }
}
