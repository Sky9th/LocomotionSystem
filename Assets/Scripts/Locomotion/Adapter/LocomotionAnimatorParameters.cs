using UnityEngine;

/// <summary>
/// Central registry for Animator parameters consumed by LocomotionAdapter.
/// Edit here when adding/removing animator bindings so every adapter stays consistent.
/// </summary>
internal static class LocomotionAnimatorParameters
{
    public const string Speed = "Speed";
    public const string Acceleration = "Acceleration";
    public const string State = "LocomotionState";
    public const string MoveX = "MoveX";
    public const string MoveY = "MoveY";
    public const string Grounded = "IsGrounded";
    public const string HeadLookX = "HeadLookX";
    public const string HeadLookY = "HeadLookY";
    public const string TurnAngle = "TurnAngle";
    public const string TurnSpeed = "TurnSpeed";
    public const string IsTurning = "IsTurning";

    public static readonly int SpeedHash = HashParameter(Speed);
    public static readonly int AccelerationHash = HashParameter(Acceleration);
    public static readonly int StateHash = HashParameter(State);
    public static readonly int MoveXHash = HashParameter(MoveX);
    public static readonly int MoveYHash = HashParameter(MoveY);
    public static readonly int GroundedHash = HashParameter(Grounded);
    public static readonly int HeadLookXHash = HashParameter(HeadLookX);
    public static readonly int HeadLookYHash = HashParameter(HeadLookY);
    public static readonly int TurnAngleHash = HashParameter(TurnAngle);
    public static readonly int TurnSpeedHash = HashParameter(TurnSpeed);
    public static readonly int IsTurningHash = HashParameter(IsTurning);

    private static int HashParameter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogError("Animator parameter name is missing in LocomotionAnimatorParameters.");
            return -1;
        }

        return Animator.StringToHash(name);
    }
}
