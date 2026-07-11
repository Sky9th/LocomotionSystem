using UnityEngine;

namespace RedDust.Services.Input
{
    /// <summary>
    /// 单轴输入载荷。
    /// </summary>
    public readonly struct SFloatInputPayload
    {
        public readonly float Value;
        public bool HasInput => Mathf.Abs(Value) > Mathf.Epsilon;

        public SFloatInputPayload(float value)
        {
            Value = value;
        }
    }
}
