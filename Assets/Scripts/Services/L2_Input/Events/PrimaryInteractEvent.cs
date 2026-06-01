using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// 主交互（左键）输入事件。Unity Input System 是发布者。
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Input/Primary Interact Event", fileName = "PrimaryInteractEvent")]
    public sealed class PrimaryInteractEvent : InputEvent<bool>
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
