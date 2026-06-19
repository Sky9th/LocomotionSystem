using UnityEngine;
using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;

namespace RedDust.Character.Locomotion
{
    internal sealed class Stance
    {
        private bool isTurning;

        // MotionSpeedScale 缓存 —— 仅在 gait/posture 变化时重算
        private float cachedMotionSpeedScale = 1f;
        private EMovementGait cachedGait;
        private EPosture cachedPosture;

        internal SCharacterDiscrete Evaluate(
            in SCharacterMotor motor, in SCharacterKinematic kin,
            in SCharacterIntent intent, LocomotionProfileSO profile,
            KinematicProfileSO kProfile, LocomotionAnimationConfigSO animProfile, float dt)
        {
            var phase = EvaluatePhase(in kin, in motor);
            var gait = intent.DesiredGait;
            var posture = intent.DesiredPosture;
            var turning = EvaluateTurning(in motor, in kin, kProfile, dt, phase);

            if (gait != cachedGait || posture != cachedPosture)
            {
                cachedGait = gait;
                cachedPosture = posture;
                cachedMotionSpeedScale = ComputeBaseSpeedScale(gait, posture, profile, animProfile);
            }

            var motionSpeedScale = cachedMotionSpeedScale; // TODO: terrain / buff 叠加修正
            var effectiveMaxSpeed = profile.GetSpeed(posture, gait) * motionSpeedScale;
            return new SCharacterDiscrete(phase, posture, gait, turning, motionSpeedScale, effectiveMaxSpeed);
        }

        /// <summary>
        /// gaitSpeed / animNativeSpeed。仅在 gait 或 posture 变化时调用。
        /// </summary>
        private static float ComputeBaseSpeedScale(
            EMovementGait gait, EPosture posture,
            LocomotionProfileSO profile, LocomotionAnimationConfigSO animProfile)
        {
            if (profile == null || animProfile == null) return 1f;

            // TODO: migrated animNativeSpeed to LocomotionAnimationSetSO
            // float animNativeSpeed = ...;
            // return animNativeSpeed > 0f ? profile.GetSpeed(posture, gait) / animNativeSpeed : 1f;
            return 1f;
        }

        private static ELocomotionPhase EvaluatePhase(in SCharacterKinematic kin, in SCharacterMotor motor)
        {
            if (!kin.GroundContact.IsGrounded) return ELocomotionPhase.Airborne;
            var v = motor.ActualPlanarVelocity; v.y = 0f;
            return v.sqrMagnitude <= Vector3.kEpsilon
                ? ELocomotionPhase.GroundedIdle : ELocomotionPhase.GroundedMoving;
        }

        private bool EvaluateTurning(in SCharacterMotor motor, in SCharacterKinematic kin,
            KinematicProfileSO kProfile, float dt, ELocomotionPhase phase)
        {
            if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
            { isTurning = false; return false; }

            var absAngle = Mathf.Abs(motor.TurnAngle);
            var wantsTurn = absAngle >= kProfile.turnEnterAngle;
            var turnDone = absAngle <= kProfile.turnCompletionAngle;

            if (!isTurning && wantsTurn) isTurning = true;
            else if (isTurning && turnDone) isTurning = false;

            return isTurning;
        }
    }
}
