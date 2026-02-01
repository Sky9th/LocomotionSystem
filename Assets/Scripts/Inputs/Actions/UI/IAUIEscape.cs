using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Emits a simple IAction whenever the Escape key action is triggered.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/UI/IA Escape")]
public class IAUIEscape : InputActionHandler
{
    [SerializeField] private bool publishOnCanceled;

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (!publishOnCanceled && !context.performed)
        {
            return;
        }

        bool isPressed = context.ReadValueAsButton();
        if (!isPressed && !publishOnCanceled)
        {
            return;
        }

        var iaction = new SUIEscapeIAction(isPressed);
        eventDispatcher.Publish(iaction);
    }
}
