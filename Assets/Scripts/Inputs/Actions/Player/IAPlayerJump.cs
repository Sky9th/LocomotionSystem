using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates the "Jump" input action into a structured jump intent for the player.
/// The action never touches physics directly; it simply reports structured data back
/// to the InputManager for further dispatch.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/Player/IA Player Jump")]
public class IAPlayerJump : InputActionHandler
{
    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        bool rawInput = context.ReadValue<bool>();
        SPlayerJumpIAction intent = new SPlayerJumpIAction(rawInput);
        eventDispatcher.Publish(intent);
    }
}
