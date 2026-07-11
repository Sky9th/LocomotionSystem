using System;
using System.Globalization;
using UnityEngine;

namespace RedDust.Gameplay.Properties
{
    [CreateAssetMenu(fileName = "IntDefinition", menuName = "RedDust/Properties/Int Definition")]
    public class IntPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.Int; }

        [Header("Constraints")] public int Min;
        public int Max = 100;

        [Header("Default")] public int DefaultValue;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return Mathf.Clamp(DefaultValue, Min, Max);
            int i = isRaw ? int.Parse((string)rawValue, CultureInfo.InvariantCulture) : SafeInt(rawValue);
            return Mathf.Clamp(i, Min, Max);
        }

        private int SafeInt(object v) { try { return Convert.ToInt32(v); } catch { return DefaultValue; } }

        [Serializable] public struct JsonData { public string id, description; public bool isDeprecated; public int min, max, defaultValue; }
    }
}
