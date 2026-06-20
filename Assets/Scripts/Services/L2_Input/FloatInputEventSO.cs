using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 单轴连续输入事件 SO。覆盖滚轮缩放、扳机键等。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Float Event", fileName = "FloatEventSO")]
    public class FloatInputEventSO : InputEventBase
    {
        /// <summary>当前帧输入值</summary>
        public float CurrentValue { get; private set; }

        /// <summary>是否有有效输入</summary>
        public bool HasInput => Mathf.Abs(CurrentValue) > Mathf.Epsilon;

        protected override void OnPerformed(InputAction.CallbackContext ctx)
        {
            CurrentValue = ctx.ReadValue<float>();
            Raise();
        }

        protected override void OnCanceled(InputAction.CallbackContext ctx)
        {
            CurrentValue = 0f;
            Raise();
        }
    }
}
