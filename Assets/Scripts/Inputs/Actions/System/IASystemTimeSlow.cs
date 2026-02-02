using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Publishes a time scale intent that slows global gameplay speed to a configured multiplier.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/System/IA Time Slow")]
public class IASystemTimeSlow : InputActionHandler
{
    [Header("Time Scale")]
    [SerializeField, Range(0.01f, 1f)] private float slowScale = 0.1f;

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled || !context.performed)
        {
            return;
        }

        var payload = new STimeScaleIAction(Mathf.Max(0.01f, slowScale));
        eventDispatcher.Publish(payload);
    }

    protected override bool OnSupportsState(EGameState state)
    {
        return state == EGameState.Playing;
    }
}
