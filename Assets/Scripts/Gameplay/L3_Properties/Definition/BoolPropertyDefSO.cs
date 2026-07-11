using System;
using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "BoolDefinition", menuName = "RedDust/Properties/Bool Definition")]
    public class BoolPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.Bool; }

        [Header("Default")] public bool DefaultValue;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return DefaultValue;
            return isRaw ? bool.Parse((string)rawValue) : SafeBool(rawValue);
        }

        private bool SafeBool(object v) { try { return Convert.ToBoolean(v); } catch { return DefaultValue; } }

        [Serializable] public struct JsonData { public string id, description; public bool isDeprecated, defaultValue; }
    }
}
