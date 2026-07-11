using RedDust.Core.RdTag;
using System;
using UnityEngine;

namespace RedDust.Gameplay.Properties
{
    [CreateAssetMenu(fileName = "RdTagListDefinition", menuName = "RedDust/Properties/RdTag List Definition")]
    public class RdTagListPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.RdTagList; }

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return Array.Empty<string>();
            if (isRaw) return Parse((string)rawValue);
            return rawValue as string[] ?? Array.Empty<string>();
        }

        private static string[] Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            try { return JsonUtility.FromJson<TagListWrap>($"{{\"Items\":{raw}}}")?.Items ?? Array.Empty<string>(); }
            catch (Exception e) { Debug.LogWarning($"[PropertyTable] Parse tag array: {e.Message}"); return Array.Empty<string>(); }
        }

        [Serializable] private class TagListWrap { public string[] Items; }

        [Serializable] public struct JsonData { public string id, description; public bool isDeprecated; }
    }
}
