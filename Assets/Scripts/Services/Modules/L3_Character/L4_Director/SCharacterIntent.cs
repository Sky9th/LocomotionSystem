using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Director
{
    public readonly struct SCharacterIntent
    {
        // ── Direction ──
        public readonly Vector3 LocomotionHeading;
        public readonly Vector3 AimDirection;

        // ── Locomotion ──
        public readonly EMovementGait DesiredGait;
        public readonly EPosture DesiredPosture;

        // ── Actions ──
        public readonly bool JumpRequested;

        // ── Speed ──
        public readonly float MovementSpeedMultiplier;

        public bool HasMovement => DesiredGait != EMovementGait.Idle && MovementSpeedMultiplier > 0f;

        public SCharacterIntent(
            Vector3 locomotionHeading,
            Vector3 aimDirection,
            EMovementGait desiredGait,
            EPosture desiredPosture,
            bool jumpRequested,
            float movementSpeedMultiplier = 1f)
        {
            LocomotionHeading = locomotionHeading.sqrMagnitude > Mathf.Epsilon
                ? locomotionHeading.normalized
                : Vector3.forward;
            AimDirection = aimDirection.sqrMagnitude > Mathf.Epsilon
                ? aimDirection.normalized
                : Vector3.forward;
            DesiredGait = desiredGait;
            DesiredPosture = desiredPosture;
            JumpRequested = jumpRequested;
            MovementSpeedMultiplier = Mathf.Clamp01(movementSpeedMultiplier);
        }

        public static SCharacterIntent None => new(
            Vector3.forward, Vector3.forward,
            EMovementGait.Idle, EPosture.Standing, false);
    }
}
