using UnityEngine;

namespace Game.Locomotion.Logic
{
    /// <summary>
    /// Encapsulates the turning state and update logic for a locomotion agent.
    ///
    /// Stores the current signed turn angle and whether the character is
    /// currently performing an in-place turn, along with internal timers
    /// that mirror the legacy LocomotionAgent.Motion behaviour.
    /// </summary>
    internal sealed class LocomotionTurnState
    {
        public float TurnAngle { get; private set; }
        public bool IsTurningInPlace { get; private set; }

        private float turnStateCooldown;
        private float lastDesiredYaw;
        private float lookStabilityTimer;

        public void Reset()
        {
            TurnAngle = 0f;
            IsTurningInPlace = false;
            turnStateCooldown = 0f;
            lastDesiredYaw = 0f;
            lookStabilityTimer = 0f;
        }

        public void Update(
            Vector3 bodyForward,
            Vector3 locomotionHeading,
            LocomotionConfigProfile config,
            float deltaTime)
        {
            // Compute the current signed planar turn angle.
            TurnAngle = LocomotionTurnLogic.EvaluateTurnAngle(bodyForward, locomotionHeading);

            // Drive the in-place turn state using the shared logic.
            bool isTurning = IsTurningInPlace;
            float cooldown = turnStateCooldown;
            float desiredYaw = lastDesiredYaw;
            float stabilityTimer = lookStabilityTimer;

            LocomotionTurnLogic.UpdateTurnState(
                TurnAngle,
                locomotionHeading,
                config,
                deltaTime,
                ref isTurning,
                ref cooldown,
                ref desiredYaw,
                ref stabilityTimer);

            IsTurningInPlace = isTurning;
            turnStateCooldown = cooldown;
            lastDesiredYaw = desiredYaw;
            lookStabilityTimer = stabilityTimer;
        }
    }
}
