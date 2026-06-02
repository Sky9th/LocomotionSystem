using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 副交互（右键）输入事件。Unity Input System 是发布者。
    /// </summary>
    [CreateAssetMenu(menuName = "RedDust/Input/Secondary Interact Event", fileName = "SecondaryInteractEventSO")]
    public sealed class SecondaryInteractEventSO : InputEvent<bool>
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
