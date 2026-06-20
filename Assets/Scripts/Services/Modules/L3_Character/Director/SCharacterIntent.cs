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
        public readonly EBodyForm DesiredBodyForm;

        // ── Actions ──
        public readonly bool JumpRequested;
        public readonly bool FirstSkillRequested;
        public readonly bool SecondSkillRequested;

        // ── Override ──
        public readonly bool OverrideMovementVelocity;
        public readonly Vector3 ExternalMovementVelocity;

        public bool HasMovement => DesiredGait != EMovementGait.Idle;

        public SCharacterIntent(
            Vector3 locomotionHeading,
            Vector3 aimDirection,
            EMovementGait desiredGait,
            EPosture desiredPosture,
            EBodyForm desiredBodyForm = EBodyForm.Relax,
            bool jumpRequested = false,
            bool firstSkillRequested = false,
            bool secondSkillRequested = false,
            Vector3 externalMovementVelocity = default,
            bool overrideMovementVelocity = false)
        {
            LocomotionHeading = locomotionHeading.sqrMagnitude > Mathf.Epsilon
                ? locomotionHeading.normalized
                : Vector3.forward;
            AimDirection = aimDirection.sqrMagnitude > Mathf.Epsilon
                ? aimDirection.normalized
                : Vector3.forward;
            DesiredGait = desiredGait;
            DesiredPosture = desiredPosture;
            DesiredBodyForm = desiredBodyForm;
            JumpRequested = jumpRequested;
            FirstSkillRequested = firstSkillRequested;
            SecondSkillRequested = secondSkillRequested;
            ExternalMovementVelocity = externalMovementVelocity;
            OverrideMovementVelocity = overrideMovementVelocity;
        }

        public static SCharacterIntent None => new(
            Vector3.forward, Vector3.forward,
            EMovementGait.Idle, EPosture.Standing, EBodyForm.Relax,
            false, false, false);
    }
}
