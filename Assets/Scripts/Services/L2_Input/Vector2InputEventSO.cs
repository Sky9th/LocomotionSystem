using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 双轴连续输入事件 SO。覆盖移动 (WASD/左摇杆) 和视角 (鼠标/右摇杆)。
    /// </summary>
    public class Vector2InputEventSO : InputEventBase
    {
        /// <summary>当前帧输入值</summary>
        public Vector2 CurrentValue { get; private set; }

        /// <summary>是否有有效输入</summary>
        public bool HasInput => CurrentValue.sqrMagnitude > Mathf.Epsilon;

        protected override void OnPerformed(InputAction.CallbackContext ctx)
        {
            CurrentValue = ctx.ReadValue<Vector2>();
            Raise();
        }

        protected override void OnCanceled(InputAction.CallbackContext ctx)
        {
            CurrentValue = Vector2.zero;
            Raise();
        }
    }
}
