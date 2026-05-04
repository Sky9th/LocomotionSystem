using System;
using UnityEngine;
using Game.Character.Components;
using Game.Character.Config;

namespace Game.Character.Kinematic
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

        internal SCharacterKinematic Evaluate(CharacterProfile profile, Vector3 viewForward, float deltaTime)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var position = actorTransform.position;
            var bodyForward = actorTransform.forward;

            var lookDirection = CharacterHeadLook.Evaluate(viewForward, modelRoot, actorTransform, profile);

            var groundContact = EvaluateGroundContactAndApplyConstraints(profile, deltaTime, ref position);

            var heading = CharacterHeadLook.EvaluatePlanarHeading(viewForward, actorTransform);
            CharacterObstacleDetection.TryDetectForwardObstacle(
                position, heading,
                profile.obstacleProbeVerticalOffset, profile.obstacleProbeDistance,
                profile.obstacleLayerMask, profile.obstacleMaxClimbHeight,
                profile.maxGroundSlopeAngle, out var obstacle);

            return new SCharacterKinematic(position, bodyForward, heading, lookDirection, groundContact, obstacle);
        }

        private SGroundContact EvaluateGroundContactAndApplyConstraints(
            CharacterProfile profile, float deltaTime, ref Vector3 position)
        {
            var contact = EvaluateStableGroundContact(profile, position, deltaTime);
            characterRig.FreezePositionY(profile.enableGroundLocking && contact.IsGrounded);

            if (profile.enableGroundLocking && contact.IsGrounded)
            {
                var pos = actorTransform.position;
                pos.y = contact.ContactPoint.y + profile.groundLockVerticalOffset;
                characterRig.SetGroundedY(pos.y);
                position = pos;
            }
            else position = actorTransform.position;

            return contact;
        }

        private SGroundContact EvaluateStableGroundContact(CharacterProfile profile, Vector3 position, float deltaTime)
        {
            var offset = profile.groundDetectVerticalOffset;
            var halfExt = profile.groundStandBoxHalfExtents;

            var contact = CharacterGroundDetection.EvaluateGroundContact(
                position,
                position + Vector3.up * offset,
                Mathf.Max(0f, profile.groundRayLength),
                position + Vector3.up * offset,
                halfExt, halfExt.y + offset,
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
