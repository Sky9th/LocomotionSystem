using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    [CreateAssetMenu(menuName = "RedDust/Input/Stand Event", fileName = "StandEventSO")]
    public sealed class StandInputEventSO : InputEvent<bool>
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
