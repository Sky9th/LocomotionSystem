namespace Game.Locomotion.LegacyControl
{
    /// <summary>
    /// Base implementation of ILocomotionController.
    ///
    /// Owns a LocomotionStateMachine and forwards evaluation
    /// requests to it. Concrete archetype controllers can
    /// override the way the state machine is created or
    /// extended without changing the Agent.
    /// </summary>
    internal abstract class LocomotionControllerBase : ILocomotionController
    {
        protected readonly LocomotionStateMachine StateMachine;

        protected LocomotionControllerBase()
        {
            StateMachine = CreateStateMachine();
        }

        public ELocomotionState CurrentPhase => StateMachine.LocomotionState;
        public EPostureState CurrentPosture => StateMachine.Posture;
        public EMovementGait CurrentGait => StateMachine.Gait;
        public ELocomotionCondition CurrentCondition => StateMachine.Condition;

        public SLocomotionDiscreteState UpdateDiscreteState(in LocomotionStateContext context)
        {
            // Initial version simply mirrors the existing
            // LocomotionStateMachine API. Later we can pass
            // the full context into the machine once it is
            // refactored to use state layers.
            return StateMachine.Evaluate(
                context.Velocity,
                context.GroundContact,
                context.Config);
        }

        protected abstract LocomotionStateMachine CreateStateMachine();
    }
}
