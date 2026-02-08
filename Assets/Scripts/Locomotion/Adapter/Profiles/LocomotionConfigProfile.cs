using UnityEngine;

/// <summary>
/// Shared configuration for LocomotionAgent motion, head look, and turn behaviour.
/// Keeps runtime logic in the agent while allowing multiple characters to share tuning.
/// </summary>
[CreateAssetMenu(
    fileName = "LocomotionConfigProfile",
    menuName = "Locomotion/Locomotion Config Profile")]
public class LocomotionConfigProfile : ScriptableObject
{
    [Header("Motion Settings")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float acceleration = 5f;

    [Header("Head Look Limits")]
    [SerializeField, Range(0f, 90f)] private float maxHeadYawDegrees = 75f;
    [SerializeField, Range(0f, 90f)] private float maxHeadPitchDegrees = 75f;
    [SerializeField, Min(0f)] private float headLookSmoothingSpeed = 540f;

    [Header("Turn Settings")]
    [SerializeField, Range(0f, 180f)] private float turnEnterAngle = 65f;
    [SerializeField, Range(0f, 180f)] private float turnExitAngle = 20f;
    [SerializeField, Min(0f)] private float turnDebounceDuration = 0.25f;
    [SerializeField, Range(0f, 45f)] private float lookStabilityAngle = 2f;
    [SerializeField, Min(0f)] private float lookStabilityDuration = 0.15f;
    [SerializeField, Range(0f, 25f)] private float turnCompletionAngle = 5f;

    [Header("Walk Turn Settings")]
    [SerializeField, Min(0f)] private float walkTurnSpeed = 360f;

    public float MoveSpeed => moveSpeed;
    public float Acceleration => acceleration;
    public float MaxHeadYawDegrees => maxHeadYawDegrees;
    public float MaxHeadPitchDegrees => maxHeadPitchDegrees;
    public float HeadLookSmoothingSpeed => headLookSmoothingSpeed;
    public float TurnEnterAngle => turnEnterAngle;
    public float TurnExitAngle => turnExitAngle;
    public float TurnDebounceDuration => turnDebounceDuration;
    public float LookStabilityAngle => lookStabilityAngle;
    public float LookStabilityDuration => lookStabilityDuration;
    public float TurnCompletionAngle => turnCompletionAngle;
    public float WalkTurnSpeed => walkTurnSpeed;
}
