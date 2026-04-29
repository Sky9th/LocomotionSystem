using System;

[Serializable]
public readonly struct SLocomotionDiscrete
{
    public SLocomotionDiscrete(ELocomotionPhase phase, EPosture posture,
        EMovementGait gait, ELocomotionCondition condition, bool isTurning)
    {
        Phase = phase;
        Posture = posture;
        Gait = gait;
        Condition = condition;
        IsTurning = isTurning;
    }

    public ELocomotionPhase Phase { get; }
    public EPosture Posture { get; }
    public EMovementGait Gait { get; }
    public ELocomotionCondition Condition { get; }
    public bool IsTurning { get; }

    public static SLocomotionDiscrete CreateActionControlled(in SLocomotionDiscrete source)
        => new(ELocomotionPhase.GroundedIdle, source.Posture, EMovementGait.Idle, source.Condition, false);

    public static SLocomotionDiscrete Default => new(
        ELocomotionPhase.GroundedIdle, EPosture.Standing, EMovementGait.Idle, ELocomotionCondition.Normal, false);
}
