using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Publishes a time scale intent that restores gameplay speed to a desired multiplier.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/System/IA Time Resume")]
public class IASystemTimeResume : InputActionHandler
{
    [Header("Time Scale")]
    [SerializeField, Min(0.01f)] private float resumeScale = 1f;

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled || !context.performed)
        {
            return;
        }

        var payload = new STimeScaleIAction(Mathf.Max(0.01f, resumeScale));
        eventDispatcher.Publish(payload);
    }

    protected override bool OnSupportsState(EGameState state)
    {
        return state == EGameState.Playing;
    }
}
