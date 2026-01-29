using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates the "Move" action map into a world-space locomotion intent. The
/// action never touches physics directly; it simply reports structured data back
/// to the InputManager for further dispatch.
/// </summary>
[CreateAssetMenu(menuName = "Inputs/Player/Move Action")]
public class PlayerMoveAction : InputActionHandler
{

    [Header("Processing")]
    [SerializeField, Range(0f, 1f)] private float deadZone = 0.15f;
    [SerializeField] private bool normalizeWorldDirection = true;

    protected override void Execute(InputAction.CallbackContext context)
    {
        if (!IsEnabled)
        {
            return;
        }

        Vector2 rawInput = context.ReadValue<Vector2>();

        // Apply deadzone filtering
        if (rawInput.magnitude < deadZone)
        {
            rawInput = Vector2.zero;
        }
        else if (normalizeWorldDirection)
        {
            rawInput = rawInput.normalized;
        }

        Vector3 worldDirection = CalculateWorldDirection(rawInput);
        PlayerMoveIntentStruct intent = new PlayerMoveIntentStruct(rawInput, worldDirection);

        // Dispatch the intent (implementation depends on your EventDispatcher setup)
        Debug.Log($"PlayerMoveAction: Publishing intent {intent}");
        eventDispatcher.Publish(intent);
    }

    private Vector3 CalculateWorldDirection(Vector2 planarInput)
    {
        if (planarInput.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        // Map Vector2 input (X = horizontal, Y = vertical) into world-space X/Z.
        return new Vector3(planarInput.x, 0f, planarInput.y);
    }

}
