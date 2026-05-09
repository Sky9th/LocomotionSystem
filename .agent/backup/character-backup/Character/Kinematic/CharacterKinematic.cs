using System;
using UnityEngine;
using Game.Character.Probes;
using Game.Locomotion.Config;

namespace Game.Character.Kinematic
{
    internal sealed class CharacterKinematic
    {
        private readonly Transform actorTransform;
        private readonly Transform modelRoot;
        private readonly Rigidbody actorRigidbody;

        private SGroundContact previousRawGroundContact;
        private SGroundContact previousGroundContact;

        internal CharacterKinematic(Transform actorTransform, Transform modelRoot, LocomotionProfile profile)
        {
            this.actorTransform = actorTransform;
            this.modelRoot = modelRoot;
            actorRigidbody = actorTransform.GetComponent<Rigidbody>();
            Reset();
        }

        internal void Reset()
        {
            previousRawGroundContact = SGroundContact.None;
            previousGroundContact = SGroundContact.None;
        }

        internal SCharacterKinematic Evaluate(
            LocomotionProfile profile,
            Vector3 viewForward,
            float deltaTime)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Vector3 position = actorTransform.position;
            Vector3 bodyForward = actorTransform.forward;

            Vector2 lookDirection = CharacterHeadLook.Evaluate(
                viewForward,
                modelRoot,
                actorTransform,
                profile);

            SGroundContact stabilizedGroundContact = EvaluateGroundContactAndApplyConstraints(
                profile,
                position,
                deltaTime,
                out position);

            var locomotionHeading = CharacterHeadLook.EvaluatePlanarHeading(viewForward, actorTransform);

            CharacterObstacleDetection.TryDetectForwardObstacle(
                position,
                locomotionHeading,
                profile.obstacleProbeVerticalOffset,
                profile.obstacleProbeDistance,
                profile.obstacleLayerMask,
                profile.obstacleMaxClimbHeight,
                profile.maxGroundSlopeAngle,
                out SForwardObstacleDetection forwardObstacleDetection);

            return new SCharacterKinematic(
                position,
                bodyForward,
                lookDirection,
                stabilizedGroundContact,
                forwardObstacleDetection);
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

            SGroundContact rawGroundContact = CharacterGroundDetection.EvaluateGroundContact(
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
