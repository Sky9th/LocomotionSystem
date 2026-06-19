using System;
using UnityEngine;
using RedDust.Character;
using RedDust.Core;

namespace RedDust.Character.Kinematic
{
    internal sealed class CharacterKinematic : Module
    {
        private readonly CharacterBuildContext ctx;

        private SGroundContact previousRawGroundContact;
        private SGroundContact previousGroundContact;

        internal CharacterKinematic(CharacterBuildContext ctx, ModuleRegistry registry) : base(registry)
        {
            this.ctx = ctx;
        }

        internal void Reset()
        {
            previousRawGroundContact = SGroundContact.None;
            previousGroundContact = SGroundContact.None;
        }

        internal SCharacterKinematic Evaluate(Vector3 locomotionHeading,
            Vector3 aimDirection, float deltaTime)
        {
            var profile = ctx.KinematicProfile;
            if (profile == null) throw new InvalidOperationException("CharacterBuildContext.KinematicProfile is null");

            var rig = ctx.Rig;
            var position = ctx.Root.position;
            var bodyForward = ctx.Root.forward;

            var lookDirection = CharacterHeadLook.Evaluate(aimDirection, ctx.ModelRoot, ctx.Root, profile);

            var groundContact = EvaluateGroundContactAndApplyConstraints(profile, deltaTime, ref position, rig);
            CharacterObstacleDetection.TryDetectForwardObstacle(
                position, locomotionHeading,
                profile.obstacleProbeVerticalOffset, profile.obstacleProbeDistance,
                profile.obstacleLayerMask, profile.obstacleMinClimbHeight, profile.obstacleMaxClimbHeight,
                profile.maxGroundSlopeAngle, out var obstacle);

            return new SCharacterKinematic(position, bodyForward, locomotionHeading, lookDirection, groundContact, obstacle);
        }

        private SGroundContact EvaluateGroundContactAndApplyConstraints(
            KinematicProfileSO profile, float deltaTime, ref Vector3 position, CharacterRig rig)
        {
            var contact = EvaluateStableGroundContact(profile, position, deltaTime);

            if (rig.SuppressGroundLock)
            {
                position = ctx.Root.position;
                return contact.WithIsGrounded(true);
            }

            rig.FreezePositionY(profile.enableGroundLocking && contact.IsGrounded);

            if (contact.IsGrounded && profile.enableGroundLocking && contact.DistanceToGround < profile.groundLockMaxDistance)
            {
                var newY = contact.ContactPoint.y + profile.groundLockVerticalOffset;
                rig.SetGroundedY(newY);
                rig.ZeroVelocity();
                position.y = newY;
            }
            else position = ctx.Root.position;

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
