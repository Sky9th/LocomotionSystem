using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates the "Look" action into a normalized SPlayerLookIAction payload.
/// Keeps camera/locomotion systems decoupled from raw device specifics.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/Player/IA Player Look")]
public class IAPlayerLook : InputActionHandler
{
    [Header("Processing")]
    [SerializeField] private float sensitivity = 1f;
    [SerializeField] private bool invertY = true;

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        Vector2 delta = context.ReadValue<Vector2>() * sensitivity;
        if (invertY)
        {
            delta.y = -delta.y;
        }

        SLookIAction intent = new SLookIAction(delta);
        eventDispatcher.Publish(intent);
    }

    protected override bool OnSupportsState(EGameState state)
    {
        return state == EGameState.Playing;
    }
}
