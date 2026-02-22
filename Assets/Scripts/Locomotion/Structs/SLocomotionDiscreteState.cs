using System;

/// <summary>
/// Bundles high-level locomotion state, posture, gait and condition
/// into a single immutable value type.
/// </summary>
[Serializable]
public readonly struct SLocomotionDiscreteState
{
    public SLocomotionDiscreteState(
        ELocomotionState state,
        EPostureState posture,
        EMovementGait gait,
        ELocomotionCondition condition)
    {
        State = state;
        Posture = posture;
        Gait = gait;
        Condition = condition;
    }

    public ELocomotionState State { get; }
    public EPostureState Posture { get; }
    public EMovementGait Gait { get; }
    public ELocomotionCondition Condition { get; }
}