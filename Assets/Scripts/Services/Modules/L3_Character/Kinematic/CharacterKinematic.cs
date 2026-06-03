using System;
using UnityEngine;
using RedDust.Character;

namespace RedDust.Character.Kinematic
{
    internal sealed class CharacterKinematic
    {
        private readonly Transform actorTransform;
        private readonly Transform modelRoot;
        private readonly CharacterRig characterRig;

        private SGroundContact previousRawGroundContact;
        private SGroundContact previousGroundContact;

        internal CharacterKinematic(Transform actorTransform, Transform modelRoot, CharacterRig characterRig)
        {
            this.actorTransform = actorTransform;
            this.modelRoot = modelRoot;
            this.characterRig = characterRig;
        }

        internal void Reset()
        {
            previousRawGroundContact = SGroundContact.None;
            previousGroundContact = SGroundContact.None;
        }

        internal SCharacterKinematic Evaluate(KinematicProfileSO profile, Vector3 locomotionHeading,
            Vector3 aimDirection, float deltaTime)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var position = actorTransform.position;
            var bodyForward = actorTransform.forward;

            var lookDirection = CharacterHeadLook.Evaluate(aimDirection, modelRoot, actorTransform, profile);

            var groundContact = EvaluateGroundContactAndApplyConstraints(profile, deltaTime, ref position);
            CharacterObstacleDetection.TryDetectForwardObstacle(
                position, locomotionHeading,
                profile.obstacleProbeVerticalOffset, profile.obstacleProbeDistance,
                profile.obstacleLayerMask, profile.obstacleMinClimbHeight, profile.obstacleMaxClimbHeight,
                profile.maxGroundSlopeAngle, out var obstacle);

            return new SCharacterKinematic(position, bodyForward, locomotionHeading, lookDirection, groundContact, obstacle);
        }

        private SGroundContact EvaluateGroundContactAndApplyConstraints(
            KinematicProfileSO profile, float deltaTime, ref Vector3 position)
        {
            var contact = EvaluateStableGroundContact(profile, position, deltaTime);

            if (characterRig.SuppressGroundLock)
            {
                position = actorTransform.position;
                return contact.WithIsGrounded(true);
            }

            characterRig.FreezePositionY(profile.enableGroundLocking && contact.IsGrounded);

            if (contact.IsGrounded && profile.enableGroundLocking && contact.DistanceToGround < profile.groundLockMaxDistance)
            {
                var newY = contact.ContactPoint.y + profile.groundLockVerticalOffset;
                characterRig.SetGroundedY(newY);
                characterRig.ZeroVelocity();
                position.y = newY;
            }
            else position = actorTransform.position;

            return contact;
        }

        private SGroundContact EvaluateStableGroundContact(KinematicProfileSO profile, Vector3 position, float deltaTime)
        {
            var contact = CharacterGroundDetection.EvaluateGroundContact(
                position, profile.groundProbeHeight, profile.groundProbeRadius,
                profile.groundLayerMask, profile.maxGroundSlopeAngle);

            contact = Accumulate(contact, previousRawGroundContact, deltaTime);
            contact = Stabilize(contact, previousGroundContact, profile.groundReacquireDebounceDuration, deltaTime);

            previousRawGroundContact = contact;
            previousGroundContact = contact;
            return contact;
        }

        private static SGroundContact Accumulate(in SGroundContact cur, in SGroundContact prev, float dt)
            => cur.WithStateDuration(cur.IsGrounded == prev.IsGrounded
                ? prev.StateDuration + Mathf.Max(0f, dt) : 0f);

        private SGroundContact Stabilize(in SGroundContact raw, in SGroundContact prevStable,
            float debounce, float dt)
        {
            var canReacquire = debounce <= 0f || prevStable.IsGrounded || prevStable.StateDuration >= debounce;
            var candidate = raw.IsGrounded && canReacquire ? raw : raw.WithIsGrounded(false);
            return Accumulate(candidate, prevStable, dt);
        }
    }
}
