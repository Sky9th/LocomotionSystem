using System;

[Serializable]
public struct SLocomotionState
{
    public SLocomotionState(SCharacterMotor motor, SCharacterDiscrete discrete)
    {
        Motor = motor;
        Discrete = discrete;
    }

    public SCharacterMotor Motor { get; }
    public SCharacterDiscrete Discrete { get; }

    public static SLocomotionState Default => new(SCharacterMotor.Default, SCharacterDiscrete.Default);
}
