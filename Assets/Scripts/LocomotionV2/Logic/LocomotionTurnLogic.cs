using UnityEngine;

namespace Game.Locomotion.Logic
{
    /// <summary>
    /// Turning logic for Locomotion v2.
    ///
    /// Computes a signed yaw angle between the character's current
    /// body forward and the desired locomotion heading, and drives
    /// a simple in-place turn state machine based on configuration.
    /// </summary>
    internal static class LocomotionTurnLogic
    {
        internal static float EvaluateTurnAngle(Vector3 bodyForward, Vector3 locomotionHeading)
        {
            Vector3 bodyFlat = bodyForward;
            Vector3 headingFlat = locomotionHeading;
            bodyFlat.y = 0f;
            headingFlat.y = 0f;

            if (bodyFlat.sqrMagnitude <= Mathf.Epsilon || headingFlat.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            bodyFlat.Normalize();
            headingFlat.Normalize();

            return Vector3.SignedAngle(bodyFlat, headingFlat, Vector3.up);
        }

        internal static void UpdateTurnState(
            float currentTurnAngle,
            Vector3 locomotionHeading,
            LocomotionConfigProfile config,
            float deltaTime,
            ref bool isTurningInPlace,
            ref float turnStateCooldown,
            ref float lastDesiredYaw,
            ref float lookStabilityTimer)
        {
            Vector3 desiredForward = locomotionHeading;
            desiredForward.y = 0f;
            if (desiredForward.sqrMagnitude <= Mathf.Epsilon)
            {
                desiredForward = Vector3.forward;
            }

            float desiredYaw = Mathf.Atan2(desiredForward.x, desiredForward.z) * Mathf.Rad2Deg;
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(desiredYaw, lastDesiredYaw));

            float lookStabilityAngle = config != null ? config.LookStabilityAngle : 0f;
            float lookStabilityDuration = config != null ? config.LookStabilityDuration : 0f;
            float turnEnterAngle = config != null ? config.TurnEnterAngle : 0f;
            float turnCompletionAngle = config != null ? config.TurnCompletionAngle : 0f;
            float turnDebounceDuration = config != null ? config.TurnDebounceDuration : 0f;

            if (yawDelta <= lookStabilityAngle)
            {
                lookStabilityTimer += deltaTime;
            }
            else
            {
                lookStabilityTimer = 0f;
            }
            lastDesiredYaw = desiredYaw;

            if (turnStateCooldown > 0f)
            {
                turnStateCooldown -= deltaTime;
            }

            float absAngle = Mathf.Abs(currentTurnAngle);

            bool wantsTurn = absAngle >= turnEnterAngle;
            bool lookIsStable = lookStabilityTimer >= lookStabilityDuration;
            bool shouldCompleteTurn = absAngle <= turnCompletionAngle;

            if (!isTurningInPlace && wantsTurn && turnStateCooldown <= 0f && lookIsStable)
            {
                isTurningInPlace = true;
                turnStateCooldown = turnDebounceDuration;
            }
            else if (isTurningInPlace && shouldCompleteTurn)
            {
                isTurningInPlace = false;
                turnStateCooldown = turnDebounceDuration;
            }
        }
    }
}
