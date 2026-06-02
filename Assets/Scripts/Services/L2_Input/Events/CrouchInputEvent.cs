using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    [CreateAssetMenu(menuName = "RedDust/Input/Crouch Event", fileName = "CrouchEventSO")]
    public sealed class CrouchInputEventSO : InputEvent<bool>
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
