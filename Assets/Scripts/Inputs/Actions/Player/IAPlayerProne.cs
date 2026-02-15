using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

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

        bool rawInput = context.ReadValue<bool>();
        SPlayerProneIAction intent = new SPlayerProneIAction(rawInput);
        eventDispatcher.Publish(intent);
    }
}
