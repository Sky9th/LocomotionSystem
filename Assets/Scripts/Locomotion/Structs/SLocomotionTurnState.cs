using System;

/// <summary>
/// Mutable turn state used by the locomotion turn computation.
/// Owned by a single locomotion controller instance.
/// </summary>
[Serializable]
internal struct SLocomotionTurnState
{
    public float TurnAngle;
    public bool IsTurning;
    public float Cooldown;
    public float LastDesiredYaw;
    public float LookStabilityTimer;
}
