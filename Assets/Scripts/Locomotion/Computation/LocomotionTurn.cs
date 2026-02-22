using UnityEngine;
using Game.Locomotion.Animation.Config;

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
        private bool isTurning;
        private float turnStateCooldown;
        private float lastDesiredYaw;
        private float lookStabilityTimer;

        public float TurnAngle => turnAngle;
        public bool IsTurningInPlace => isTurning;

        /// <summary>
        /// Computes the signed turn delta in degrees for this frame given
        /// a maximum turn speed in degrees per second and the frame's
        /// deltaTime. The result will never exceed the remaining angle
        /// towards the desired heading.
        ///
        /// Call <see cref="Update"/> first each frame so that the
        /// internal <see cref="TurnAngle"/> reflects the latest
        /// desired heading before invoking this method.
        /// </summary>
        public float EvaluateTurnDelta(float maxTurnSpeed, float deltaTime)
        {
            float absAngle = Mathf.Abs(turnAngle);
            if (absAngle <= Mathf.Epsilon || maxTurnSpeed <= 0f || deltaTime <= 0f)
            {
                return 0f;
            }

            float maxStep = maxTurnSpeed * deltaTime;
            float step = Mathf.Min(maxStep, absAngle);
            return Mathf.Sign(turnAngle) * step;
        }

        /// <summary>
        /// Evaluates the current planar turn angle and updates the
        /// internal "turn in place" state machine based on the
        /// supplied context.
        /// </summary>
        public void Evaluate(
            Vector3 bodyForward,
            Vector3 locomotionHeading,
            LocomotionAnimationProfile config,
            float deltaTime,
            in SLocomotionDiscreteState discreteState,
            out float turnAngle,
            out bool isTurningOut)
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
                ref isTurning,
                ref turnStateCooldown,
                ref lastDesiredYaw,
                ref lookStabilityTimer);

            // Only treat the character as turning in place when they are
            // effectively idle on the ground. This keeps the concept out
            // of the top-level phase enum while still giving animation a
            // clear signal to branch on.
            isTurningOut =
                isTurning &&
                (discreteState.State == ELocomotionState.GroundedIdle ||
                discreteState.State == ELocomotionState.GroundedMoving);
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
            LocomotionAnimationProfile config,
            float deltaTime,
            ref bool isTurning,
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
            float lookStabilityAngle = config.lookStabilityAngle;
            float lookStabilityDuration = config.lookStabilityDuration;
            float turnEnterAngle = config.turnEnterAngle;
            float turnCompletionAngle = config.turnCompletionAngle;
            float turnDebounceDuration = config.turnDebounceDuration;

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

            if (!isTurning && wantsTurn && turnStateCooldown <= 0f && lookIsStable)
            {
                isTurning = true;
                turnStateCooldown = turnDebounceDuration;
            }
            else if (isTurning && shouldCompleteTurn)
            {
                isTurning = false;
                turnStateCooldown = turnDebounceDuration;
            }
        }
    }
}
