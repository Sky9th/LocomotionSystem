namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// High-level locomotion controller abstraction.
    ///
    /// Represents a complete locomotion rule set for a given
    /// archetype (Human, Zombie, etc.) and exposes a single
    /// entry point to evaluate discrete locomotion state.
    /// </summary>
    internal interface ILocomotionController
    {
        SLocomotionDiscreteState UpdateDiscreteState(in LocomotionStateContext context);

        ELocomotionState CurrentPhase { get; }
        EPostureState CurrentPosture { get; }
        EMovementGait CurrentGait { get; }
        ELocomotionCondition CurrentCondition { get; }
    }
}
