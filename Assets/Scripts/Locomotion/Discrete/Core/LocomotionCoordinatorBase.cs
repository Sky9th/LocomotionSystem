using UnityEngine;
using Game.Locomotion.Discrete.Core;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Config;
using Game.Locomotion.Input;

namespace Game.Locomotion.Discrete.Core
{
    /// <summary>
    /// Base implementation of <see cref="ILocomotionCoordinator"/>.
    ///
    /// Owns a <see cref="LocomotionGraph"/> and forwards
    /// evaluation requests to it. Concrete archetype controllers
    /// can override <see cref="CreateGraph"/> to customise
    /// how the state machine is built without changing the Agent.
    /// </summary>
    internal abstract class LocomotionCoordinatorBase : ILocomotionCoordinator
    {
        protected readonly LocomotionGraph Graph;

        // Per-controller temporal turn state used by the static
        // turn-in-place evaluation.
        private float lastDesiredYaw;
        private float lookStabilityTimer;
        private bool isTurningState;

        private SLocomotionDiscrete currentState;

        protected LocomotionCoordinatorBase()
        {
            Graph = CreateGraph();

            // Safe default state before the first evaluation.
            currentState = new SLocomotionDiscrete(
                ELocomotionPhase.GroundedIdle,
                EPosture.Standing,
                EMovementGait.Idle,
                ELocomotionCondition.Normal,
                isTurning: false);
        }

        public SLocomotionDiscrete CurrentState => currentState;

        public ELocomotionPhase CurrentPhase => currentState.Phase;
        public EPosture CurrentPosture => currentState.Posture;
        public EMovementGait CurrentGait => currentState.Gait;
        public ELocomotionCondition CurrentCondition => currentState.Condition;

        public SLocomotionDiscrete Evaluate(
            in SLocomotionAgent agent,
            LocomotionProfile profile,
            in SLocomotionInputActions actions,
            float deltaTime)
        {
            SLocomotionDiscrete discrete = EvaluateDiscreteState(in agent, in actions, deltaTime);

            bool isTurning = false;

            if (profile != null)
            {
                isTurning = EvaluateIsTurningInPlace(
                    agent.TurnAngle,
                    agent.LocomotionHeading,
                    profile,
                    deltaTime,
                    in discrete);
            }

            currentState = new SLocomotionDiscrete(
                discrete.Phase,
                discrete.Posture,
                discrete.Gait,
                discrete.Condition,
                isTurning);

            return currentState;
        }

        private bool EvaluateIsTurningInPlace(
            float turnAngle,
            Vector3 locomotionHeading,
            LocomotionProfile profile,
            float deltaTime,
            in SLocomotionDiscrete discreteState)
        {
            UpdateTurnState(
                locomotionHeading,
                profile,
                deltaTime,
                turnAngle,
                ref lastDesiredYaw,
                ref lookStabilityTimer,
                ref isTurningState);

            return isTurningState &&
                (discreteState.Phase == ELocomotionPhase.GroundedIdle ||
                 discreteState.Phase == ELocomotionPhase.GroundedMoving);
        }

        private static void UpdateTurnState(
            Vector3 locomotionHeading,
            LocomotionProfile profile,
            float deltaTime,
            float currentTurnAngle,
            ref float lastDesiredYaw,
            ref float lookStabilityTimer,
            ref bool isTurningState)
        {
            Vector3 desiredForward = locomotionHeading;
            desiredForward.y = 0f;
            if (desiredForward.sqrMagnitude <= Mathf.Epsilon)
            {
                desiredForward = Vector3.forward;
            }

            float desiredYaw = Mathf.Atan2(desiredForward.x, desiredForward.z) * Mathf.Rad2Deg;
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(desiredYaw, lastDesiredYaw));

            float lookStabilityAngle = profile != null ? profile.lookStabilityAngle : 0f;
            float lookStabilityDuration = profile != null ? profile.lookStabilityDuration : 0f;

            float turnEnterAngle = profile != null ? profile.turnEnterAngle : 0f;
            float turnCompletionAngle = profile != null ? profile.turnCompletionAngle : 0f;

            if (yawDelta <= lookStabilityAngle)
            {
                lookStabilityTimer += deltaTime;
            }
            else
            {
                lookStabilityTimer = 0f;
            }
            lastDesiredYaw = desiredYaw;

            float absAngle = Mathf.Abs(currentTurnAngle);

            bool wantsTurn = absAngle >= turnEnterAngle;
            bool lookIsStable = lookStabilityTimer >= lookStabilityDuration;
            bool shouldCompleteTurn = absAngle <= turnCompletionAngle;

            if (!isTurningState && wantsTurn && lookIsStable)
            {
                isTurningState = true;
            }
            else if (isTurningState && shouldCompleteTurn)
            {
                isTurningState = false;
            }
        }

        /// <summary>
        /// Evaluates and caches the discrete locomotion state using
        /// the underlying <see cref="LocomotionGraph"/>.
        /// Can be overridden by derived controllers to customize
        /// how discrete states are produced.
        /// </summary>
        protected virtual SLocomotionDiscrete EvaluateDiscreteState(in SLocomotionAgent agent, in SLocomotionInputActions actions, float deltaTime)
        {
            currentState = Graph.Evaluate(in agent, in actions);
            return currentState;
        }

        /// <summary>
        /// Factory hook used by derived controllers to construct
        /// a specific state machine configuration for this archetype.
        /// </summary>
        protected abstract LocomotionGraph CreateGraph();
    }
}
