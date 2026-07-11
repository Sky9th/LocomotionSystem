using System;
using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "RdTagDefinition", menuName = "RedDust/Properties/RdTag Definition")]
    public class RdTagPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.RdTag; }

        [Header("Default")] public string DefaultValue;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            return isDefault ? DefaultValue : (rawValue as string) ?? DefaultValue;
        }

        [Serializable] public struct JsonData { public string id, description, defaultValue; public bool isDeprecated; }
    }
}
