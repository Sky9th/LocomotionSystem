using System;
using System.Globalization;
using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "FloatDefinition", menuName = "RedDust/Properties/Float Definition")]
    public class FloatPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.Float; }

        [Header("Constraints")] public float Min;
        public float Max = 100f;

        [Header("Default")] public float DefaultValue = 100f;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            if (isDefault) return Mathf.Clamp(DefaultValue, Min, Max);
            if (isRaw) return Parse((string)rawValue);
            return SafeFloat(rawValue);
        }

        private float Parse(string raw)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                return Mathf.Clamp(f, Min, Max);
            Debug.LogWarning($"[PropertyTable] Bad float '{raw}'");
            return DefaultValue;
        }

        private float SafeFloat(object v) { try { return Convert.ToSingle(v); } catch { return DefaultValue; } }
    }
}
