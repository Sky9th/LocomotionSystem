using UnityEngine;

namespace RedDust.Properties
{
    [CreateAssetMenu(fileName = "RTagDefinition", menuName = "RedDust/Properties/rTag Definition")]
    public class RTagPropertyDefSO : PropertyDefSO
    {
        private void OnEnable() { Type = PropertyType.rTag; }

        [Header("Default")] public string DefaultValue;

        public override object ComputeWriteValue(object rawValue, bool isRaw, bool isDefault)
        {
            return isDefault ? DefaultValue : (rawValue as string) ?? DefaultValue;
        }
    }
}
