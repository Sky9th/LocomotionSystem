using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedDust.Input
{
    [CreateAssetMenu(menuName = "Inputs/Player/IA Player PrimaryInteract")]
    public class IAPlayerPrimaryInteract : InputActionHandler
    {
        protected override void Execute(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            bool rawInput = context.ReadValueAsButton();
            SIActionPrimaryInteract intent = SIActionPrimaryInteract.CreateEvent(rawInput, context.phase);
            eventDispatcher.Publish(intent);
        }
    }
}
