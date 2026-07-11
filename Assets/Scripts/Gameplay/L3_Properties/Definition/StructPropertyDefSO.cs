using System;
using UnityEngine;

namespace RedDust.Gameplay.Properties
{
    [CreateAssetMenu(fileName = "StructDefinition", menuName = "RedDust/Properties/Struct Definition")]
    public class StructPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.Struct; }

        [Header("Type")] public string StructTypeName;
        [Header("Default")] [TextArea(2, 5)] public string DefaultJson = "[]";

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            string json;
            if (isDefault) json = DefaultJson ?? "[]";
            else if (isRaw) json = (string)rawValue ?? "[]";
            else json = rawValue != null ? JsonUtility.ToJson(rawValue) : "[]";

            if (json.TrimStart().StartsWith("["))
                json = $"{{\"Items\":{json}}}";
            return json;
        }

        public override bool TypeMatches<T>()
        {
            if (string.IsNullOrEmpty(StructTypeName)) return true;
            var t = System.Type.GetType(StructTypeName);
            if (t == null)
            {
                Debug.LogError($"[PropertyTable] StructTypeName '{StructTypeName}' could not be resolved.");
                return false;
            }
            if (t != typeof(T))
            {
                Debug.LogError($"[PropertyTable] declared '{t.Name}' but called with '{typeof(T).Name}'.");
                return false;
            }
            return true;
        }

        [Serializable] public struct JsonData { public string id, description, structTypeName, defaultJson; public bool isDeprecated; }
    }
}
