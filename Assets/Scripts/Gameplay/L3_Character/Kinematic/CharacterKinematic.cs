using RedDust.Core.Modules;
using UnityEngine;
using RedDust.Gameplay.Character;
using RedDust.Core.Events;

namespace RedDust.Gameplay.Character.Kinematic
{
    internal sealed class CharacterKinematic : ModuleChild
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

        internal SCharacterKinematic Evaluate(in SCharacterInputState input, float deltaTime)
        {
            var props = ctx.Properties;
            var groundSystem = ctx.GroundSystemConfig;
            var rig = ctx.Rig;
            var position = ctx.Root.position;
            var bodyForward = ctx.Root.forward;

            // 朝向：路径优先 → 模型朝向
            var pf = ctx.Pathfinding;
            bool hasActivePath = pf != null && pf.HasActivePath;
            var locomotionHeading = hasActivePath && pf.DesiredVelocity.sqrMagnitude > Mathf.Epsilon
                ? pf.DesiredVelocity.normalized : ctx.ModelRoot.forward;

            // 瞄准：从 InputState（PlayerService/AIService 写入）
            Vector3 aimDirection;
            if (input.HasAimPoint)
            {
                var dir = input.AimPoint - ctx.ModelRoot.position;
                dir.y = 0f;
                aimDirection = dir.sqrMagnitude > Mathf.Epsilon ? dir.normalized : ctx.ModelRoot.forward;
            }
            else aimDirection = ctx.ModelRoot.forward;

            // Head Look IK 延后（俯视角游戏优先级低）。将来用 Animation Rigging MultiAimConstraint 实现。
            var lookDirection = Vector2.zero;

            var maxSlopeAngle = props.GetFloat(CharacterConst.PropertyPath.Movement.MaxSlopeAngle);
            var groundContact = EvaluateGroundContactAndApplyConstraints(maxSlopeAngle, groundSystem, deltaTime, ref position, rig);
            CharacterObstacleDetection.TryDetectForwardObstacle(
                position, locomotionHeading,
                props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleProbeVertical),
                props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleProbeDistance),
                groundSystem.obstacleLayerMask,
                props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleMinClimb),
                props.GetFloat(CharacterConst.PropertyPath.Body.ObstacleMaxClimb),
                maxSlopeAngle, out var obstacle);

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
