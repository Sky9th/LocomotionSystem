using Game.Locomotion.Computation;
using Game.Locomotion.Discrete.Structs;
using Game.Locomotion.Input;

namespace Game.Locomotion.Discrete.Core
{
    /// <summary>
    /// Evaluates traversal intent from the current locomotion probes and input state.
    ///
    /// This graph owns only traversal-specific rule composition and keeps it
    /// separate from the regular discrete locomotion graph.
    /// </summary>
    internal sealed class LocomotionTraversalGraph
    {
        private const float DefaultCommittedDuration = 0.45f;

        private SLocomotionTraversal currentTraversal;
        private float committedTimer;
        private bool clearTerminalStageNextFrame;

        public LocomotionTraversalGraph()
        {
            Reset();
        }

        public SLocomotionTraversal CurrentTraversal => currentTraversal;

        public void Reset()
        {
            currentTraversal = SLocomotionTraversal.None;
            committedTimer = 0f;
            clearTerminalStageNextFrame = false;
        }

        public SLocomotionTraversal Evaluate(
            in SLocomotionMotor motor,
            in SLocomotionInputActions actions,
            in SLocomotionDiscrete discreteState,
            float deltaTime)
        {
            if (clearTerminalStageNextFrame)
            {
                currentTraversal = SLocomotionTraversal.None;
                committedTimer = 0f;
                clearTerminalStageNextFrame = false;
            }

            switch (currentTraversal.Stage)
            {
                case ELocomotionTraversalStage.Idle:
                    return EvaluateIdle(in motor, in actions, in discreteState);

                case ELocomotionTraversalStage.Requested:
                    return EvaluateRequested(in motor, in actions, in discreteState);

                case ELocomotionTraversalStage.Committed:
                    return EvaluateCommitted(deltaTime);

                case ELocomotionTraversalStage.Completed:
                case ELocomotionTraversalStage.Canceled:
                default:
                    return currentTraversal;
            }
        }

        private SLocomotionTraversal EvaluateIdle(
            in SLocomotionMotor motor,
            in SLocomotionInputActions actions,
            in SLocomotionDiscrete discreteState)
        {
            if (!TryBuildTraversalRequest(in motor, in actions, in discreteState, out SLocomotionTraversal requestedTraversal))
            {
                currentTraversal = SLocomotionTraversal.None;
                return currentTraversal;
            }

            currentTraversal = requestedTraversal;
            return currentTraversal;
        }

        private SLocomotionTraversal EvaluateRequested(
            in SLocomotionMotor motor,
            in SLocomotionInputActions actions,
            in SLocomotionDiscrete discreteState)
        {
            if (!TryBuildTraversalRequest(in motor, in actions, in discreteState, out SLocomotionTraversal requestedTraversal))
            {
                return SetTerminalStage(ELocomotionTraversalStage.Canceled);
            }

            currentTraversal = new SLocomotionTraversal(
                requestedTraversal.Type,
                ELocomotionTraversalStage.Committed,
                requestedTraversal.ObstacleHeight,
                requestedTraversal.ObstaclePoint,
                requestedTraversal.TargetPoint,
                requestedTraversal.FacingDirection);
            committedTimer = 0f;

            return currentTraversal;
        }

        private SLocomotionTraversal EvaluateCommitted(float deltaTime)
        {
            committedTimer += deltaTime;
            if (committedTimer < DefaultCommittedDuration)
            {
                return currentTraversal;
            }

            return SetTerminalStage(ELocomotionTraversalStage.Completed);
        }

        private bool TryBuildTraversalRequest(
            in SLocomotionMotor motor,
            in SLocomotionInputActions actions,
            in SLocomotionDiscrete discreteState,
            out SLocomotionTraversal traversal)
        {
            traversal = SLocomotionTraversal.None;

            if (discreteState.Phase != ELocomotionPhase.GroundedIdle
                && discreteState.Phase != ELocomotionPhase.GroundedMoving)
            {
                return false;
            }

            SButtonInputState jumpButton = actions.JumpAction.Button;
            bool wantsTraversal = jumpButton.IsRequested || jumpButton.IsPressed;
            if (!wantsTraversal)
            {
                return false;
            }

            SForwardObstacleDetection obstacle = motor.ForwardObstacleDetection;
            if (!obstacle.CanClimb)
            {
                return false;
            }

            traversal = new SLocomotionTraversal(
                ELocomotionTraversalType.Climb,
                ELocomotionTraversalStage.Requested,
                obstacle.ObstacleHeight,
                obstacle.Point,
                obstacle.TopPoint,
                obstacle.Direction);

            return true;
        }

        private SLocomotionTraversal SetTerminalStage(ELocomotionTraversalStage stage)
        {
            currentTraversal = new SLocomotionTraversal(
                currentTraversal.Type,
                stage,
                currentTraversal.ObstacleHeight,
                currentTraversal.ObstaclePoint,
                currentTraversal.TargetPoint,
                currentTraversal.FacingDirection);
            committedTimer = 0f;
            clearTerminalStageNextFrame = true;

            return currentTraversal;
        }
    }
}