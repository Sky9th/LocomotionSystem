namespace Game.Locomotion.State.Core
{
    /// <summary>
    /// Generic interface for a single discrete locomotion state layer.
    ///
    /// Each layer manages one orthogonal dimension (Phase, Posture, Gait, Condition)
    /// and owns its current state value, updated from a read-only
    /// <see cref="LocomotionStateContext"/> every simulation step.
    /// </summary>
    /// <typeparam name="TState">Enum type representing the layer state.</typeparam>
    internal interface ILocomotionStateLayer<TState>
    {
        /// <summary>Current value of this state layer.</summary>
        TState Current { get; }

        /// <summary>Reset the layer to a safe default state.</summary>
        void Reset(TState defaultState);

        /// <summary>
        /// Update the internal state using the supplied context.
        /// Implementations should write into <see cref="Current"/>.
        /// </summary>
        void Update(in LocomotionStateContext context);
    }
}
