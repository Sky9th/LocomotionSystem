using UnityEngine;
using Game.Locomotion.State.Core;
using Game.Locomotion.Computation;
using Game.Locomotion.Config;

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

        // Per-controller turn state used by the static
        // LocomotionTurn computation helper.
        private SLocomotionTurnState turnState;

        private SLocomotionDiscreteState currentState;

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

        public SLocomotionStateFrame Evaluate(in SLocomotionStateContext context, float deltaTime)
        {
            SLocomotionDiscreteState discrete = EvaluateDiscreteState(in context, deltaTime);

            float turnAngle = 0f;
            bool isTurning = false;

            if (context.Profile != null)
            {
                LocomotionTurn.Evaluate(
                    ref turnState,
                    context.BodyForward,
                    context.LocomotionHeading,
                    context.Profile,
                    deltaTime,
                    in discrete,
                    out turnAngle,
                    out isTurning);
            }

            return new SLocomotionStateFrame(discrete, turnAngle, isTurning);
        }

        /// <summary>
        /// Evaluates and caches the discrete locomotion state using
        /// the underlying <see cref="LocomotionStateMachine"/>.
        /// Can be overridden by derived controllers to customize
        /// how discrete states are produced.
        /// </summary>
        protected virtual SLocomotionDiscreteState EvaluateDiscreteState(in SLocomotionStateContext context, float deltaTime)
        {
            currentState = StateMachine.Evaluate(in context);
            return currentState;
        }

        /// <summary>
        /// Factory hook used by derived controllers to construct
        /// a specific state machine configuration for this archetype.
        /// </summary>
        protected abstract LocomotionStateMachine CreateStateMachine();
    }
}
