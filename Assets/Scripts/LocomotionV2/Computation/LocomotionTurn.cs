using UnityEngine;

namespace Game.Locomotion.Computation
{
    /// <summary>
    /// Helper used by the v2 locomotion pipeline to evaluate the planar
    /// turn angle and maintain a lightweight "turn in place" state
    /// machine, keeping this logic out of the Agent and state layers.
    /// </summary>
    internal sealed class LocomotionTurn
    {
        private float turnAngle;
        private bool isTurningInPlace;
        private float turnStateCooldown;
        private float lastDesiredYaw;
        private float lookStabilityTimer;

        public float TurnAngle => turnAngle;
        public bool IsTurningInPlace => isTurningInPlace;

        /// <summary>
        /// Update the internal turning state and return the current turn
        /// angle and whether the character should be considered "turning
        /// in place" given the high-level discrete locomotion state.
        /// </summary>
        public void Update(
            Vector3 bodyForward,
            Vector3 locomotionHeading,
            LocomotionConfigProfile config,
            float deltaTime,
            in SLocomotionDiscreteState discreteState,
            out float turnAngle,
            out bool isTurningInPlaceOutput)
        {
            // Compute the current signed planar turn angle.
            turnAngle = EvaluateTurnAngle(bodyForward, locomotionHeading);
            turnAngle = Mathf.Clamp(turnAngle, -180f, 180f);
            this.turnAngle = turnAngle;

            // Drive the in-place turn state using the same rules as the
            // legacy controller, but implemented locally so the v2
            // pipeline does not depend on Logic/Legacy namespaces.
            UpdateTurnState(
                this.turnAngle,
                locomotionHeading,
                config,
                deltaTime,
                ref isTurningInPlace,
                ref turnStateCooldown,
                ref lastDesiredYaw,
                ref lookStabilityTimer);

            // Only treat the character as turning in place when they are
            // effectively idle on the ground. This keeps the concept out
            // of the top-level phase enum while still giving animation a
            // clear signal to branch on.
            bool isTurningInPlaceIdle =
                isTurningInPlace &&
                discreteState.State == ELocomotionState.GroundedIdle &&
                discreteState.Gait == EMovementGait.Idle;

            isTurningInPlaceOutput = isTurningInPlaceIdle;
        }

        private static float EvaluateTurnAngle(Vector3 bodyForward, Vector3 locomotionHeading)
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

        private static void UpdateTurnState(
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
            float lookStabilityAngle = config.LookStabilityAngle;
            float lookStabilityDuration = config.LookStabilityDuration;
            float turnEnterAngle = config.TurnEnterAngle;
            float turnCompletionAngle = config.TurnCompletionAngle;
            float turnDebounceDuration = config.TurnDebounceDuration;

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
