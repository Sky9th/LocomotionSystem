using System;

[Serializable]
public struct SLocomotionState
{
    public SLocomotionState(SLocomotionMotor motor, SLocomotionDiscrete discrete)
    {
        Motor = motor;
        Discrete = discrete;
    }

    public SLocomotionMotor Motor { get; }
    public SLocomotionDiscrete Discrete { get; }

    public static SLocomotionState Default => new(SLocomotionMotor.Default, SLocomotionDiscrete.Default);
}
