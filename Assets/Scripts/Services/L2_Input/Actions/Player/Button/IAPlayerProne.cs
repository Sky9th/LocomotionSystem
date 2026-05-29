using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    /// <summary>
    /// Translates the "Prone" input action into a structured prone intent for the player.
    /// The action never touches physics directly; it simply reports structured data back
    /// to the InputManager for further dispatch.
    /// </summary>
    [CreateAssetMenu(menuName = "Inputs/Player/IA Player Prone")]
    public class IAPlayerProne : InputActionHandler
    {
        protected override void Execute(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            bool rawInput = context.ReadValueAsButton();
            SIActionProne intent = SIActionProne.CreateEvent(rawInput, context.phase);
            eventDispatcher.Publish(intent);
        }
    }
}
