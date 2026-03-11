using System;
using UnityEngine;
using Game.Locomotion.Computation;
using Game.Locomotion.Config;
using Game.Locomotion.Input;

namespace Game.Locomotion.Motor
{
    /// <summary>
    /// Motor module for <see cref="LocomotionAgent"/>.
    ///
    /// Owns per-agent kinematic state (smoothed planar velocity) and applies
    /// Transform-level corrections such as root-motion alignment and
    /// ground locking.
    /// </summary>
    internal sealed class LocomotionMotor
    {
        private readonly Transform actorTransform;
        private readonly Transform modelRoot;
        private readonly Rigidbody actorRigidbody;

        private SGroundContact previousRawGroundContact;
        private SGroundContact previousGroundContact;

        private Vector3 currentVelocity;
        private Vector2 currentLocalVelocity;

        internal Vector3 CurrentVelocity => currentVelocity;
        internal Vector2 CurrentLocalVelocity => currentLocalVelocity;

        internal LocomotionMotor(Transform actorTransform, Transform modelRoot, LocomotionProfile profile)
        {
            this.actorTransform = actorTransform;
            this.modelRoot = modelRoot;
            actorRigidbody = actorTransform.GetComponent<Rigidbody>();

            Reset();
        }

        public void ApplyDeltaPosition(Vector3 deltaWorldPosition)
        {
            Vector3 currentPosition = actorTransform.position;
            Vector3 targetPosition = currentPosition + deltaWorldPosition;
            actorTransform.position = targetPosition;
        }

        public void ApplyDeltaRotation(Quaternion deltaWorldRotation)
        {
            actorTransform.rotation = actorTransform.rotation * deltaWorldRotation;
        }

        internal void Reset()
        {
            previousRawGroundContact = SGroundContact.None;
            previousGroundContact = SGroundContact.None;

            currentVelocity = Vector3.zero;
            currentLocalVelocity = Vector2.zero;
        }

        internal void UpdateKinematics(
            in SLocomotionInputActions inputActions,
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
            LocomotionProfile profile,
            in SLocomotionInputActions inputActions,
            Vector3 viewForward,
            float deltaTime)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Vector3 position = actorTransform.position;
            Vector3 bodyForward = actorTransform.forward;
            Vector3 locomotionHeading = LocomotionHeadLook.EvaluatePlanarHeading(viewForward, actorTransform);

            UpdateKinematics(
                inputActions,
                profile,
                locomotionHeading,
                deltaTime,
                out Vector2 desiredLocalVelocity,
                out Vector3 desiredVelocity);

            Vector2 lookDirection = LocomotionHeadLook.Evaluate(
                viewForward,
                modelRoot,
                actorTransform,
                profile);

            float turnAngle = LocomotionKinematics.ComputeSignedPlanarTurnAngle(
                bodyForward,
                locomotionHeading);

            SGroundContact stabilizedGroundContact = EvaluateGroundContactAndApplyConstraints(
                profile,
                position,
                deltaTime,
                out position);

            LocomotionObstacleDetection.TryDetectForwardObstacle(
                position,
                locomotionHeading,
                profile.obstacleProbeVerticalOffset,
                profile.obstacleProbeDistance,
                profile.obstacleLayerMask,
                profile.obstacleMaxClimbHeight,
                profile.maxGroundSlopeAngle,
                out SForwardObstacleDetection forwardObstacleDetection);

            return new SLocomotionMotor(
                position,
                desiredLocalVelocity,
                desiredVelocity,
                currentLocalVelocity,
                currentVelocity,
                currentVelocity.magnitude,
                locomotionHeading,
                bodyForward,
                lookDirection,
                stabilizedGroundContact,
                forwardObstacleDetection,
                turnAngle,
                isLeftFootOnFront: false);
        }

        private SGroundContact EvaluateGroundContactAndApplyConstraints(
            LocomotionProfile profile,
            Vector3 actorPosition,
            float deltaTime,
            out Vector3 correctedPosition)
        {
            SGroundContact stabilizedGroundContact = EvaluateStableGroundContact(
                profile,
                actorPosition,
                deltaTime);

            bool enableGroundLocking = profile.enableGroundLocking;
            UpdateFreezePositionY(enableGroundLocking && stabilizedGroundContact.IsGrounded);

            if (enableGroundLocking && stabilizedGroundContact.IsGrounded)
            {
                // Ground locking follows the stabilized ground state so debounce and snapping stay consistent.
                Vector3 groundedPosition = actorTransform.position;
                groundedPosition.y = stabilizedGroundContact.ContactPoint.y + profile.groundLockVerticalOffset;
                actorTransform.position = groundedPosition;
                correctedPosition = groundedPosition;
            }
            else
            {
                correctedPosition = actorTransform.position;
            }

            return stabilizedGroundContact;
        }

        private SGroundContact EvaluateStableGroundContact(
            LocomotionProfile profile,
            Vector3 actorPosition,
            float deltaTime)
        {
            float maxSlopeAngle = profile.maxGroundSlopeAngle;
            LayerMask groundLayerMask = profile.groundLayerMask;
            Vector3 standBoxHalfExtents = profile.groundStandBoxHalfExtents;
            float distanceRayLength = Mathf.Max(0f, profile.groundRayLength);
            float detectVerticalOffset = profile.groundDetectVerticalOffset;

            Vector3 rayOrigin = actorPosition + Vector3.up * detectVerticalOffset;
            Vector3 standProbeOrigin = rayOrigin;
            float standProbeDistance = standBoxHalfExtents.y + detectVerticalOffset;

            SGroundContact rawGroundContact = LocomotionGroundDetection.EvaluateGroundContact(
                actorPosition,
                rayOrigin,
                distanceRayLength,
                standProbeOrigin,
                standBoxHalfExtents,
                standProbeDistance,
                groundLayerMask,
                maxSlopeAngle);

            rawGroundContact = AccumulateGroundContactStateDuration(
                rawGroundContact,
                previousRawGroundContact,
                deltaTime);

            SGroundContact stabilizedGroundContact = StabilizeGroundContact(
                rawGroundContact,
                previousGroundContact,
                profile.groundReacquireDebounceDuration,
                deltaTime);

            previousRawGroundContact = rawGroundContact;
            previousGroundContact = stabilizedGroundContact;
            return stabilizedGroundContact;
        }

        private static SGroundContact AccumulateGroundContactStateDuration(
            in SGroundContact currentGroundContact,
            in SGroundContact previousGroundContact,
            float deltaTime)
        {
            float stateDuration = currentGroundContact.IsGrounded == previousGroundContact.IsGrounded
                ? previousGroundContact.StateDuration + Mathf.Max(0f, deltaTime)
                : 0f;

            return currentGroundContact.WithStateDuration(stateDuration);
        }

        private SGroundContact StabilizeGroundContact(
            in SGroundContact rawGroundContact,
            in SGroundContact previousStableGroundContact,
            float reacquireDebounceDuration,
            float deltaTime)
        {
            bool canReacquireGround = reacquireDebounceDuration <= 0f
                || previousStableGroundContact.IsGrounded
                || previousStableGroundContact.StateDuration >= reacquireDebounceDuration;

            SGroundContact candidateGroundContact = rawGroundContact.IsGrounded && canReacquireGround
                ? rawGroundContact
                : rawGroundContact.WithIsGrounded(false);

            return AccumulateGroundContactStateDuration(
                candidateGroundContact,
                previousStableGroundContact,
                deltaTime);
        }

        private void UpdateFreezePositionY(bool isGrounded)
        {
            if (actorRigidbody == null)
            {
                return;
            }

            RigidbodyConstraints constraints = actorRigidbody.constraints;
            if (isGrounded)
            {
                constraints |= RigidbodyConstraints.FreezePositionY;
            }
            else
            {
                constraints &= ~RigidbodyConstraints.FreezePositionY;
            }

            if (constraints != actorRigidbody.constraints)
            {
                actorRigidbody.constraints = constraints;
            }
        }

    }
}
