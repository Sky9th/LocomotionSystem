using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 按钮型输入事件 SO。暴露 IsPressed / IsRequested / IsReleased。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Events/Input/Button Event", fileName = "ButtonEventSO")]
    public class ButtonInputEventSO : InputEventBase
    {
        /// <summary>当前帧是否按住</summary>
        public bool IsPressed { get; private set; }

        /// <summary>本帧刚按下（边沿触发）</summary>
        public bool IsRequested { get; private set; }

        /// <summary>本帧刚松开（边沿触发）</summary>
        public bool IsReleased { get; private set; }

        /// <summary>帧末归零边沿信号。IsPressed 保留（电平持续）。</summary>
        public override void ClearFrameSignals()
        {
            IsRequested = false;
            IsReleased = false;
        }

        protected override void OnPerformed(InputAction.CallbackContext ctx)
        {
            IsPressed = true;
            IsRequested = true;
            IsReleased = false;
            Raise();
        }

        protected override void OnCanceled(InputAction.CallbackContext ctx)
        {
            IsPressed = false;
            IsRequested = false;
            IsReleased = true;
            Raise();
        }
    }
}
