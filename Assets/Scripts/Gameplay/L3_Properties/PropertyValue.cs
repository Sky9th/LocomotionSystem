using System;
using UnityEngine;

namespace RedDust.Properties
{
    [Serializable]
    public struct PropertyValue
    {
        public PropertyType Type;
        public string SerializedValue;

        public bool HasValue => !string.IsNullOrEmpty(SerializedValue);

        public static PropertyValue None => new() { SerializedValue = null };
    }
}
