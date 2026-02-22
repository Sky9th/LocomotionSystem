using UnityEngine;
using Game.Locomotion.State.Core;
using Game.Locomotion.Computation;

namespace Game.Locomotion.State.Controllers
{
    /// <summary>
    /// Base implementation of <see cref="ILocomotionStateController"/>.
    ///
    /// Owns a <see cref="LocomotionStateMachine"/> and forwards
    /// evaluation requests to it. Concrete archetype controllers
    /// can override <see cref="CreateStateMachine"/> to customise
    /// how the state machine is built without changing the Agent.
    /// </summary>
    internal abstract class LocomotionStateControllerBase : ILocomotionStateController
    {
        protected readonly LocomotionStateMachine StateMachine;

        private SLocomotionDiscreteState currentState;

        private readonly LocomotionTurn turnHelper = new LocomotionTurn();

        protected LocomotionStateControllerBase()
        {
            StateMachine = CreateStateMachine();

            // Safe default state before the first evaluation.
            currentState = new SLocomotionDiscreteState(
                ELocomotionState.GroundedIdle,
                EPostureState.Standing,
                EMovementGait.Idle,
                ELocomotionCondition.Normal);
        }

        public SLocomotionDiscreteState CurrentState => currentState;

        public ELocomotionState CurrentPhase => currentState.State;
        public EPostureState CurrentPosture => currentState.Posture;
        public EMovementGait CurrentGait => currentState.Gait;
        public ELocomotionCondition CurrentCondition => currentState.Condition;

        public float CurrentTurnAngle { get; private set; }
        public bool IsTurningInPlace { get; private set; }
        public bool IsTurningInWalk { get; private set; }
        public bool IsTurningInRun { get; private set; }
        public bool IsTurningInSprint { get; private set; }

        public SLocomotionDiscreteState UpdateDiscreteState(in SLocomotionStateContext context, float deltaTime)
        {
            currentState = StateMachine.Evaluate(in context);

            // Update turning state based on the evaluated discrete state
            // and the directional information contained in the context.
            if (context.Config != null)
            {
                turnHelper.Evaluate(
                    context.BodyForward,
                    context.LocomotionHeading,
                    context.Config,
                    deltaTime,
                    in currentState,
                    out float turnAngle,
                    out bool isTurning);

                CurrentTurnAngle = turnAngle;
                IsTurningInPlace = currentState.Gait == EMovementGait.Idle && isTurning;
                IsTurningInWalk = currentState.Gait == EMovementGait.Walk && isTurning;
                IsTurningInRun = currentState.Gait == EMovementGait.Run && isTurning;
                IsTurningInSprint = currentState.Gait == EMovementGait.Sprint && isTurning;
            }

            return currentState;
        }

        /// <summary>
        /// Factory hook used by derived controllers to construct
        /// a specific state machine configuration for this archetype.
        /// </summary>
        protected abstract LocomotionStateMachine CreateStateMachine();
    }
}
