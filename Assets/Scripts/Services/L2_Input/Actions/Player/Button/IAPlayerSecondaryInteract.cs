using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.GameInput
{
    [CreateAssetMenu(menuName = "Inputs/Player/IA Player SecondaryInteract")]
    public class IAPlayerSecondaryInteract : InputActionHandler
    {
        protected override void Execute(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            bool rawInput = context.ReadValueAsButton();
            SIActionSecondaryInteract intent = SIActionSecondaryInteract.CreateEvent(rawInput, context.phase);
            eventDispatcher.Publish(intent);
        }
    }
}
