using UnityEngine;

namespace RedDust.Services.Input
{
    /// <summary>
    /// 双轴输入载荷。
    /// </summary>
    public readonly struct SVector2InputPayload
    {
        public readonly Vector2 Value;
        public bool HasInput => Value.sqrMagnitude > Mathf.Epsilon;

        public SVector2InputPayload(Vector2 value)
        {
            Value = value;
        }
    }
}
