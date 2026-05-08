using Game.Locomotion.Animation.Config;
using Game.Locomotion.Motor;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal static class TurnAngleStepRotationApplier
    {
        public static bool TryApply(
            LocomotionAnimationProfile animationProfile,
            LocomotionMotor motor,
            in SCharacterSnapshot snapshot,
            float deltaTime)
        {
            if (animationProfile == null || motor == null)
            {
                return false;
            }

            float absAngle = Mathf.Abs(snapshot.Motor.TurnAngle);
            if (absAngle <= Mathf.Epsilon)
            {
                return false;
            }

            bool isMoving = snapshot.DiscreteState.Gait != EMovementGait.Idle;
            float turnSpeed = animationProfile.GetTurnSpeed(snapshot.DiscreteState.Posture, snapshot.DiscreteState.Gait, isMoving);
            if (turnSpeed <= 0f)
            {
                return false;
            }

            float maxStep = turnSpeed * deltaTime;
            float step = Mathf.Min(maxStep, absAngle);
            float deltaAngle = Mathf.Sign(snapshot.Motor.TurnAngle) * step;
            motor.ApplyDeltaRotation(Quaternion.AngleAxis(deltaAngle, Vector3.up));
            return true;
        }
    }
}