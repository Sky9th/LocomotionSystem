using UnityEngine;
using RedDust.Gameplay.Character;
using RedDust.Gameplay.Character.Animation;
using RedDust.Gameplay.Character.Kinematic;

namespace RedDust.Gameplay.Character.Locomotion
{
    internal sealed class Stance
    {
        /// <summary>转向判定进入角（度）。绝对值超过此角度进入转向状态。</summary>
        private const float TurnEnterAngle = 65f;

        /// <summary>转向完成角（度）。绝对值低于此角度退出转向状态。</summary>
        private const float TurnCompletionAngle = 5f;

        private bool isTurning;

        internal SCharacterDiscrete Evaluate(
            in SCharacterMotor motor, in SCharacterKinematic kin,
            in SCharacterInputState input, EMovementGait gait,
            LocomotionAnimationSetSO animSet, float motionSpeedScale, float dt)
        {
            var phase = EvaluatePhase(in kin, in motor);
            var posture = input.DesiredPosture;
            var turning = EvaluateTurning(in motor, in kin, dt, phase);

            var nativeSpeed = animSet?.GetNativeSpeed(gait) ?? 0f;
            var effectiveMaxSpeed = nativeSpeed * motionSpeedScale;
            return new SCharacterDiscrete(phase, posture, gait, turning, motionSpeedScale, effectiveMaxSpeed);
        }

        private static ELocomotionPhase EvaluatePhase(in SCharacterKinematic kin, in SCharacterMotor motor)
        {
            if (!kin.GroundContact.IsGrounded) return ELocomotionPhase.Airborne;
            var v = motor.ActualPlanarVelocity; v.y = 0f;
            return v.sqrMagnitude <= Vector3.kEpsilon
                ? ELocomotionPhase.GroundedIdle : ELocomotionPhase.GroundedMoving;
        }

        private bool EvaluateTurning(in SCharacterMotor motor, in SCharacterKinematic kin,
            float dt, ELocomotionPhase phase)
        {
            if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
            { isTurning = false; return false; }

            var absAngle = Mathf.Abs(motor.TurnAngle);
            var wantsTurn = absAngle >= TurnEnterAngle;
            var turnDone = absAngle <= TurnCompletionAngle;

            if (!isTurning && wantsTurn) isTurning = true;
            else if (isTurning && turnDone) isTurning = false;

            return isTurning;
        }
    }
}
