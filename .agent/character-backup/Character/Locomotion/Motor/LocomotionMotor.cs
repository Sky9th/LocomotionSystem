using System;
using UnityEngine;
using Game.Character.Probes;
using Game.Locomotion.Computation;
using Game.Locomotion.Config;
using Game.Character.Input;

namespace Game.Locomotion.Motor
{
    public sealed class LocomotionMotor
    {
        private readonly Transform actorTransform;

        private Vector3 currentVelocity;
        private Vector2 currentLocalVelocity;

        internal Vector3 CurrentVelocity => currentVelocity;
        internal Vector2 CurrentLocalVelocity => currentLocalVelocity;

        internal LocomotionMotor(Transform actorTransform)
        {
            this.actorTransform = actorTransform;

            Reset();
        }

        public void ApplyDeltaPosition(Vector3 deltaWorldPosition)
        {
            actorTransform.position += deltaWorldPosition;
        }

        public void ApplyDeltaRotation(Quaternion deltaWorldRotation)
        {
            actorTransform.rotation *= deltaWorldRotation;
        }

        internal void Reset()
        {
            currentVelocity = Vector3.zero;
            currentLocalVelocity = Vector2.zero;
        }

        private void UpdateKinematics(
            in SCharacterInputActions inputActions,
            LocomotionProfile profile,
            Vector3 locomotionHeading,
            float deltaTime,
            out Vector2 desiredLocalVelocity,
            out Vector3 desiredWorldVelocity)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            float moveSpeed = profile.moveSpeed;
            SMoveIAction moveAction = inputActions.MoveAction.Equals(SMoveIAction.None)
                ? inputActions.LastMoveAction
                : inputActions.MoveAction;

            desiredLocalVelocity = LocomotionKinematics.ComputeDesiredPlanarVelocity(moveAction, moveSpeed);

            float acceleration = profile.acceleration;
            currentLocalVelocity = LocomotionKinematics.SmoothVelocity(
                currentLocalVelocity,
                desiredLocalVelocity,
                acceleration,
                deltaTime);

            desiredWorldVelocity = LocomotionKinematics.ConvertLocalToWorldPlanarVelocity(
                desiredLocalVelocity,
                locomotionHeading);

            currentVelocity = LocomotionKinematics.ConvertLocalToWorldPlanarVelocity(
                currentLocalVelocity,
                locomotionHeading);
        }

        internal SLocomotionMotor Evaluate(
            in SCharacterKinematic kinematic,
            LocomotionProfile profile,
            in SCharacterInputActions inputActions,
            Vector3 viewForward,
            float deltaTime)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Vector3 locomotionHeading = CharacterHeadLook.EvaluatePlanarHeading(viewForward, actorTransform);

            UpdateKinematics(
                inputActions,
                profile,
                locomotionHeading,
                deltaTime,
                out Vector2 desiredLocalVelocity,
                out Vector3 desiredVelocity);

            float turnAngle = LocomotionKinematics.ComputeSignedPlanarTurnAngle(
                kinematic.BodyForward,
                locomotionHeading);

            return new SLocomotionMotor(
                desiredLocalVelocity,
                desiredVelocity,
                currentLocalVelocity,
                currentVelocity,
                currentVelocity.magnitude,
                locomotionHeading,
                turnAngle,
                isLeftFootOnFront: false);
        }
    }
}
