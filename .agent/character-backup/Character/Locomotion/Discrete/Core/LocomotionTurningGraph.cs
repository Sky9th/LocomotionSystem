using UnityEngine;
using Game.Locomotion.Config;
using Game.Locomotion.Discrete.Structs;

namespace Game.Locomotion.Discrete.Core
{
    /// <summary>
    /// Evaluates turn-in-place state as a dedicated temporal graph.
    ///
    /// The graph owns the short-lived turning memory required to stabilize
    /// desired facing direction across frames.
    /// </summary>
    internal sealed class LocomotionTurningGraph
    {
        private float lastDesiredYaw;
        private float lookStabilityTimer;
        private bool isTurningState;

        public bool CurrentState => isTurningState;

        public void Reset()
        {
            lastDesiredYaw = 0f;
            lookStabilityTimer = 0f;
            isTurningState = false;
        }

        public bool Evaluate(
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
    }
}