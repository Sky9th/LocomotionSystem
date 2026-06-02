using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 冲刺输入事件。Unity Input System 是发布者，订阅者通过 Register/Unregister 收听。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Input/Sprint Event", fileName = "SprintEventSO")]
    public sealed class SprintInputEventSO : InputEvent<bool>
    {
        protected override void OnPerformed(InputAction.CallbackContext ctx)
        {
            Raise(ctx.ReadValueAsButton());
        }

        protected override void OnCanceled(InputAction.CallbackContext ctx)
        {
            Raise(ctx.ReadValueAsButton());
        }
    }
}
