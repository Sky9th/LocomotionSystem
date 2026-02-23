using System;

/// <summary>
/// Combined result of a single locomotion state evaluation step,
/// including the discrete locomotion state and planar turning data.
/// </summary>
[Serializable]
internal readonly struct SLocomotionStateFrame
{
    public readonly SLocomotionDiscreteState DiscreteState;
    public readonly float TurnAngle;
    public readonly bool IsTurning;

    public SLocomotionStateFrame(
        SLocomotionDiscreteState discreteState,
        float turnAngle,
        bool isTurning)
    {
        DiscreteState = discreteState;
        TurnAngle = turnAngle;
        IsTurning = isTurning;
    }
}
