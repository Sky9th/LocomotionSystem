using UnityEngine;
using Game.Locomotion.Discrete.Core;
using Game.Locomotion.Discrete.Interface;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Config;
using Game.Locomotion.Input;
using Game.Locomotion.Computation;

namespace Game.Locomotion.Discrete.Core
{
    /// <summary>
    /// Base implementation of <see cref="ILocomotionCoordinator"/>.
    ///
    /// Owns a <see cref="LocomotionGraph"/> and forwards
    /// evaluation requests to it. Concrete archetype controllers
    /// provide a graph instance via the base constructor.
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
        private SLocomotionTraversal currentTraversal;

        protected LocomotionCoordinatorBase(LocomotionGraph graph)
        {
            Graph = graph;

            // Safe default state before the first evaluation.
            currentState = SLocomotionDiscrete.Default;
            currentTraversal = SLocomotionTraversal.None;
        }

        public SLocomotionDiscrete CurrentState => currentState;
        public SLocomotionTraversal CurrentTraversal => currentTraversal;

        public ELocomotionPhase CurrentPhase => currentState.Phase;
        public EPosture CurrentPosture => currentState.Posture;
        public EMovementGait CurrentGait => currentState.Gait;
        public ELocomotionCondition CurrentCondition => currentState.Condition;

        public SLocomotionDiscrete Evaluate(
            in SLocomotionMotor motor,
            LocomotionProfile profile,
            in SLocomotionInputActions actions,
            float deltaTime)
        {
            currentState = Graph.Evaluate(in motor, in actions);
            SLocomotionDiscrete discrete = currentState;
            currentTraversal = EvaluateTraversal(in motor, in actions, in discrete);

            bool isTurning = false;

            if (profile != null)
            {
                isTurning = EvaluateIsTurningInPlace(
                    motor.TurnAngle,
                    motor.LocomotionHeading,
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

        private static SLocomotionTraversal EvaluateTraversal(
            in SLocomotionMotor motor,
            in SLocomotionInputActions actions,
            in SLocomotionDiscrete discreteState)
        {
            if (discreteState.Phase != ELocomotionPhase.GroundedIdle
                && discreteState.Phase != ELocomotionPhase.GroundedMoving)
            {
                return SLocomotionTraversal.None;
            }

            SButtonInputState jumpButton = actions.JumpAction.Button;
            bool wantsTraversal = jumpButton.IsRequested || jumpButton.IsPressed;
            if (!wantsTraversal)
            {
                return SLocomotionTraversal.None;
            }

            SForwardObstacleDetection obstacle = motor.ForwardObstacleDetection;
            if (!obstacle.CanClimb)
            {
                return SLocomotionTraversal.None;
            }

            return new SLocomotionTraversal(
                ELocomotionTraversalType.Climb,
                ELocomotionTraversalStage.Requested,
                obstacle.ObstacleHeight,
                obstacle.Point,
                obstacle.TopPoint,
                obstacle.Direction);
        }

        private bool EvaluateIsTurningInPlace(
            float turnAngle,
            Vector3 locomotionHeading,
            LocomotionProfile profile,
            float deltaTime,
            in SLocomotionDiscrete discreteState)
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

            float absAngle = Mathf.Abs(turnAngle);

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

            return isTurningState &&
                (discreteState.Phase == ELocomotionPhase.GroundedIdle ||
                 discreteState.Phase == ELocomotionPhase.GroundedMoving);
        }

        // Note: Previously we had overridable hooks (CreateGraph/EvaluateDiscreteState).
        // With the current single-archetype setup those were redundant, so the
        // graph is injected via the constructor and evaluation is inlined.
    }
}
