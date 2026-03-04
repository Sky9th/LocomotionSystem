namespace Game.Locomotion.Discrete.Interface
{
    /// <summary>
    /// Generic interface for a single discrete locomotion aspect.
    ///
    /// Each aspect manages one orthogonal dimension (Phase, Posture, Gait, Condition)
    /// and owns its current state value, updated from a read-only
    /// view of agent probes and input intent every simulation step.
    /// </summary>
    /// <typeparam name="TState">Enum type representing the layer state.</typeparam>
    internal interface ILocomotionAspect<TState>
    {
        /// <summary>Current value of this state layer.</summary>
        TState Current { get; }

        /// <summary>Reset the layer to a safe default state.</summary>
        void Reset(TState defaultState);

        /// <summary>
        /// Update the internal state using the supplied data.
        /// Implementations should write into <see cref="Current"/>.
        /// </summary>
        void Update(in SLocomotionAgent agent, in Game.Locomotion.Input.SLocomotionInputActions actions);
    }
}
