using Game.Locomotion.Agent;
using Game.Locomotion.Animation.Config;
using UnityEngine;

namespace Game.Locomotion.Animation.Layers.Base
{
    internal static class TurnAngleStepRotationApplier
    {
        public static bool TryApply(
            LocomotionAnimationProfile animationProfile,
            ILocomotionModelRotator modelRotator,
            in SLocomotion snapshot,
            float deltaTime)
        {
            if (animationProfile == null || modelRotator == null)
            {
                return false;
            }

            float absAngle = Mathf.Abs(snapshot.TurnAngle);
            if (absAngle <= Mathf.Epsilon)
            {
                return false;
            }

            bool isMoving = snapshot.Gait != EMovementGait.Idle;
            float turnSpeed = animationProfile.GetTurnSpeed(snapshot.Posture, snapshot.Gait, isMoving);
            if (turnSpeed <= 0f)
            {
                return false;
            }

            float maxStep = turnSpeed * deltaTime;
            float step = Mathf.Min(maxStep, absAngle);
            float deltaAngle = Mathf.Sign(snapshot.TurnAngle) * step;
            modelRotator.RotateModelYaw(deltaAngle);
            return true;
        }
    }
}