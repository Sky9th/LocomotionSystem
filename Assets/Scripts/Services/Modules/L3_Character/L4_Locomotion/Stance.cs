using UnityEngine;
using RedDust.Character;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;

namespace RedDust.Character.Locomotion
{
    internal sealed class Stance
    {
        private bool isTurning;

        internal SCharacterDiscrete Evaluate(
            in SCharacterMotor motor, in SCharacterKinematic kin,
            in SCharacterIntent intent, LocomotionProfile profile, float dt)
        {
            var phase = EvaluatePhase(in kin, in motor);
            var gait = intent.DesiredGait;
            var posture = intent.DesiredPosture;
            var turning = EvaluateTurning(in motor, in kin, profile, dt, phase);
            return new SCharacterDiscrete(phase, posture, gait, turning);
        }

        private static ELocomotionPhase EvaluatePhase(in SCharacterKinematic kin, in SCharacterMotor motor)
        {
            if (!kin.GroundContact.IsGrounded) return ELocomotionPhase.Airborne;
            var v = motor.ActualPlanarVelocity; v.y = 0f;
            return v.sqrMagnitude <= Vector3.kEpsilon
                ? ELocomotionPhase.GroundedIdle : ELocomotionPhase.GroundedMoving;
        }

        private bool EvaluateTurning(in SCharacterMotor motor, in SCharacterKinematic kin,
            LocomotionProfile profile, float dt, ELocomotionPhase phase)
        {
            if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
            { isTurning = false; return false; }

            var absAngle = Mathf.Abs(motor.TurnAngle);
            var wantsTurn = absAngle >= profile.turnEnterAngle;
            var turnDone = absAngle <= profile.turnCompletionAngle;

            if (!isTurning && wantsTurn) isTurning = true;
            else if (isTurning && turnDone) isTurning = false;

            return isTurning;
        }
    }
}
