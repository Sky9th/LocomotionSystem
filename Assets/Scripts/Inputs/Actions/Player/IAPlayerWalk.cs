using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates the "Move" action map into a world-space locomotion intent. The
/// action never touches physics directly; it simply reports structured data back
/// to the InputManager for further dispatch.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/Player/IA Player Walk")]
public class IAPlayerWalk : InputActionHandler
{

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        bool rawInput = context.ReadValue<bool>();

        SWalkIAction intent = new SWalkIAction(rawInput);

        eventDispatcher.Publish(intent);
    }
}
