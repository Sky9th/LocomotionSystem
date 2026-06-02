using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    [CreateAssetMenu(menuName = "RedDust/Input/Prone Event", fileName = "ProneEventSO")]
    public sealed class ProneInputEventSO : InputEvent<bool>
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
