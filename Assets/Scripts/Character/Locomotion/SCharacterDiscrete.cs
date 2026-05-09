using System;

[Serializable]
public readonly struct SCharacterDiscrete
{
    public SCharacterDiscrete(ELocomotionPhase phase, EPosture posture, EMovementGait gait, bool isTurning)
    {
        Phase = phase;
        Posture = posture;
        Gait = gait;
        IsTurning = isTurning;
    }

    public ELocomotionPhase Phase { get; }
    public EPosture Posture { get; }
    public EMovementGait Gait { get; }
    public bool IsTurning { get; }

    public static SCharacterDiscrete Default => new(
        ELocomotionPhase.GroundedIdle, EPosture.Standing, EMovementGait.Idle, false);
}
