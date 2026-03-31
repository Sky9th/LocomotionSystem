using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates the "Sprint" action map into a world-space locomotion intent. The
/// action never touches physics directly; it simply reports structured data back
/// to the InputManager for further dispatch.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/Player/IA Player Sprint")]
public class IAPlayerSprint : InputActionHandler
{

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        bool isPressed = context.ReadValueAsButton();

        SSprintIAction intent = SSprintIAction.CreateEvent(isPressed, context.phase);

        eventDispatcher.Publish(intent);
    }
}
