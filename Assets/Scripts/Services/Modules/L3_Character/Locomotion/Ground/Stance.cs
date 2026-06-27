using UnityEngine;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;

namespace RedDust.Character.Locomotion
{
    internal sealed class Stance
    {
        /// <summary>转向判定进入角（度）。绝对值超过此角度进入转向状态。</summary>
        private const float TurnEnterAngle = 65f;

        /// <summary>转向完成角（度）。绝对值低于此角度退出转向状态。</summary>
        private const float TurnCompletionAngle = 5f;

        private bool isTurning;

        // TODO: 姿势/步态缓存 —— 后续 posture-aware speed 接入 Properties 后，
        // motionSpeedScale 仅在 gait/posture 变化时重算，避免每帧查 Properties。
        private float cachedMotionSpeedScale = 1f;
        private EMovementGait cachedGait;
        private EPosture cachedPosture;

        internal SCharacterDiscrete Evaluate(
            in SCharacterMotor motor, in SCharacterKinematic kin,
            in SCharacterIntent intent, LocomotionAnimationSetSO animSet, float dt)
        {
            var phase = EvaluatePhase(in kin, in motor);
            var gait = intent.DesiredGait;
            var posture = intent.DesiredPosture;
            var turning = EvaluateTurning(in motor, in kin, dt, phase);

            var nativeSpeed = animSet != null ? animSet.GetNativeSpeed(gait) : 0f;
            var motionSpeedScale = 1f; // TODO: Properties 敏捷/负重/姿势/地形修正

            // TODO: 恢复条件守卫 —— gait/posture 变化时重算 motionSpeedScale
            cachedGait = gait;
            cachedPosture = posture;
            cachedMotionSpeedScale = motionSpeedScale;

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
