using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "StringDefinition", menuName = "RedDust/Properties/String Definition")]
    public class StringPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.String; }

        [Header("Default")] public string DefaultValue;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            return isDefault ? DefaultValue : (rawValue as string) ?? DefaultValue;
        }
    }
}
