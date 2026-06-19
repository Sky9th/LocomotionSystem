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
            var physique = ctx.Physique;
            var groundSystem = ctx.GroundSystemConfig;

            var rig = ctx.Rig;
            var position = ctx.Root.position;
            var bodyForward = ctx.Root.forward;

            var lookDirection = CharacterHeadLook.Evaluate(aimDirection, ctx.ModelRoot, ctx.Root,
                physique.MaxHeadYaw, physique.MaxHeadPitch);

            var groundContact = EvaluateGroundContactAndApplyConstraints(physique.MaxSlopeAngle, groundSystem, deltaTime, ref position, rig);
            CharacterObstacleDetection.TryDetectForwardObstacle(
                position, locomotionHeading,
                physique.ObstacleProbeVertical, physique.ObstacleProbeDistance,
                groundSystem.obstacleLayerMask, physique.ObstacleMinClimb, physique.ObstacleMaxClimb,
                physique.MaxSlopeAngle, out var obstacle);

            return new SCharacterKinematic(position, bodyForward, locomotionHeading, lookDirection, groundContact, obstacle);
        }

        private SGroundContact EvaluateGroundContactAndApplyConstraints(
            float maxSlopeAngle, GroundSystemConfigSO groundSystem, float deltaTime, ref Vector3 position, CharacterRig rig)
        {
            var contact = EvaluateStableGroundContact(maxSlopeAngle, groundSystem, position, deltaTime);

            if (rig.SuppressGroundLock)
            {
                position = ctx.Root.position;
                return contact.WithIsGrounded(true);
            }

            rig.FreezePositionY(groundSystem.enableGroundLocking && contact.IsGrounded);

            if (contact.IsGrounded && groundSystem.enableGroundLocking && contact.DistanceToGround < groundSystem.groundLockMaxDistance)
            {
                var newY = contact.ContactPoint.y + groundSystem.groundLockVerticalOffset;
                rig.SetGroundedY(newY);
                rig.ZeroVelocity();
                position.y = newY;
            }
            else position = ctx.Root.position;

            return contact;
        }

        private SGroundContact EvaluateStableGroundContact(
            float maxSlopeAngle, GroundSystemConfigSO groundSystem, Vector3 position, float deltaTime)
        {
            var contact = CharacterGroundDetection.EvaluateGroundContact(
                position, groundSystem.probeHeight, groundSystem.probeRadius,
                groundSystem.groundLayerMask, maxSlopeAngle);

            contact = Accumulate(contact, previousRawGroundContact, deltaTime);
            contact = Stabilize(contact, previousGroundContact, groundSystem.groundReacquireDebounceDuration, deltaTime);

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
