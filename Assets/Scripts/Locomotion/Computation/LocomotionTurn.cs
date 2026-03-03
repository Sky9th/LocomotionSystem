using UnityEngine;
using Game.Locomotion.Config;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Stateless helper used by the v2 locomotion pipeline to evaluate
    /// planar turning. All temporal data is stored in an external
    /// <see cref="SLocomotionTurnState"/> owned by the caller.
    /// </summary>
    internal static class LocomotionTurn
    {

        /// <summary>
        /// Evaluates the current planar turn angle and updates the
        /// internal "turn in place" state machine based on the
        /// supplied context.
        /// </summary>
        public static void Evaluate(
            ref SLocomotionTurnState state,
            Vector3 bodyForward,
            Vector3 locomotionHeading,
            LocomotionProfile profile,
            float deltaTime,
            in SLocomotionDiscreteState discreteState,
            out float turnAngle,
            out bool isTurningOut)
        {
            // Compute the current signed planar turn angle on the XZ plane.
            Vector3 bodyFlat = bodyForward;
            Vector3 headingFlat = locomotionHeading;
            bodyFlat.y = 0f;
            headingFlat.y = 0f;

            if (bodyFlat.sqrMagnitude <= Mathf.Epsilon || headingFlat.sqrMagnitude <= Mathf.Epsilon)
            {
                state.TurnAngle = 0f;
                turnAngle = 0f;
                isTurningOut = false;
                return;
            }

            bodyFlat.Normalize();
            headingFlat.Normalize();

            float signedAngle = Vector3.SignedAngle(bodyFlat, headingFlat, Vector3.up);
            signedAngle = Mathf.Clamp(signedAngle, -180f, 180f);
            state.TurnAngle = signedAngle;
            turnAngle = signedAngle;

            // Drive the in-place turn state using the same rules as the
            // legacy controller, but implemented locally so the v2
            // pipeline does not depend on Logic/Legacy namespaces.
            UpdateTurnState(
                locomotionHeading,
                profile,
                in discreteState,
                deltaTime,
                ref state);

            // Only treat the character as turning in place when they are
            // effectively idle on the ground. This keeps the concept out
            // of the top-level phase enum while still giving animation a
            // clear signal to branch on.
            isTurningOut =
                state.IsTurning &&
                (discreteState.State == ELocomotionState.GroundedIdle ||
                discreteState.State == ELocomotionState.GroundedMoving);
        }

        private static void UpdateTurnState(
            Vector3 locomotionHeading,
            LocomotionProfile profile,
            in SLocomotionDiscreteState discreteState,
            float deltaTime,
            ref SLocomotionTurnState stateRef)
        {
            float currentTurnAngle = stateRef.TurnAngle;
            Vector3 desiredForward = locomotionHeading;
            desiredForward.y = 0f;
            if (desiredForward.sqrMagnitude <= Mathf.Epsilon)
            {
                desiredForward = Vector3.forward;
            }

            float desiredYaw = Mathf.Atan2(desiredForward.x, desiredForward.z) * Mathf.Rad2Deg;
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(desiredYaw, stateRef.LastDesiredYaw));

            float lookStabilityAngle = profile != null ? profile.lookStabilityAngle : 0f;
            float lookStabilityDuration = profile != null ? profile.lookStabilityDuration : 0f;

            float turnEnterAngle = profile != null ? profile.turnEnterAngle : 0f;
            float turnCompletionAngle = profile != null ? profile.turnCompletionAngle : 0f;

            if (yawDelta <= lookStabilityAngle)
            {
                stateRef.LookStabilityTimer += deltaTime;
            }
            else
            {
                stateRef.LookStabilityTimer = 0f;
            }
            stateRef.LastDesiredYaw = desiredYaw;

            float absAngle = Mathf.Abs(currentTurnAngle);

            bool wantsTurn = absAngle >= turnEnterAngle;
            bool lookIsStable = stateRef.LookStabilityTimer >= lookStabilityDuration;
            bool shouldCompleteTurn = absAngle <= turnCompletionAngle;

            if (!stateRef.IsTurning && wantsTurn && lookIsStable)
            {
                stateRef.IsTurning = true;
            }
            else if (stateRef.IsTurning && shouldCompleteTurn)
            {
                stateRef.IsTurning = false;
            }
        }
    }
}
