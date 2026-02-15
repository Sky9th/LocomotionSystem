using UnityEngine;
using Game.Locomotion.State.Core;
using Game.Locomotion.Computation;

namespace Game.Locomotion.State.Controllers
{
    /// <summary>
    /// Base implementation of <see cref="ILocomotionController"/>.
    ///
    /// Owns a <see cref="LocomotionStateMachine"/> and forwards
    /// evaluation requests to it. Concrete archetype controllers
    /// can override <see cref="CreateStateMachine"/> to customise
    /// how the state machine is built without changing the Agent.
    /// </summary>
    internal abstract class LocomotionControllerBase : ILocomotionController
    {
        protected readonly LocomotionStateMachine StateMachine;

        private SLocomotionDiscreteState currentState;

        private readonly LocomotionTurn turnHelper = new LocomotionTurn();

        protected LocomotionControllerBase()
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

        public SLocomotionDiscreteState UpdateDiscreteState(in LocomotionStateContext context, float deltaTime)
        {
            currentState = StateMachine.Evaluate(in context);

            // Update turning state based on the evaluated discrete state
            // and the directional information contained in the context.
            if (context.Config != null)
            {
                turnHelper.Update(
                    context.BodyForward,
                    context.LocomotionHeading,
                    context.Config,
                    deltaTime,
                    in currentState,
                    out float turnAngle,
                    out bool isTurningInPlace);

                CurrentTurnAngle = turnAngle;
                IsTurningInPlace = isTurningInPlace;
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
