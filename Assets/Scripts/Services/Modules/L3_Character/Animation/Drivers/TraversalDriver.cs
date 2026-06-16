using UnityEngine;
using Animancer;
using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation.Drivers
{
    public sealed class TraversalDriver : BaseAnimationDriver
    {
        private Collider obstacleCollider;
        private Vector3 topPoint;

        public override int ChannelMask => 1 << 0; // FullBody

        public override void Evaluate(in CharacterFrameContext ctx, float dt)
        {
            var aliasProfile = brain?.BuildContext?.AnimationAlias;
            if (aliasProfile == null) return;

            if (!ctx.Intent.JumpRequested) return;

            var phase = ctx.Discrete.Phase;
            if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
                return;

            var obstacle = ctx.Kinematic.ForwardObstacleDetection;
            if (!obstacle.CanClimb) return;

            var alias = ResolveClimbAlias(aliasProfile, obstacle.ObstacleHeight);
            if (alias == null) return;

            brain?.SubmitRequest(this, new AnimationRequest
            {
                Alias = alias,
                Tags = 0x01,
                Resistance = 10,
                FadeIn = 0.1f,
                FadeOut = 0.15f,
                OnComplete = OnCompleteBehavior.Resume,
                OnInterrupted = OnInterruptedBehavior.Resume,
                ChannelMask = 1 << 0
            });
            obstacleCollider = obstacle.Collider;
            topPoint = obstacle.TopPoint;
        }

        public override void Drive(in CharacterFrameContext ctx, float dt) { }

        public override void OnStarted()
        {
            brain?.CharacterRig?.SetSuppressGroundLock(true);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, true);
            brain?.CharacterRig?.SetKinematic(true);
        }

        public override void OnCompleted()
        {
            brain?.CharacterRig?.SetGroundedY(topPoint.y);
            brain?.CharacterRig?.SetKinematic(false);
            brain?.CharacterRig?.SetSuppressGroundLock(false);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, false);
            obstacleCollider = null;
        }

        public override void OnInterrupted(AnimationRequest by)
        {
            brain?.CharacterRig?.SetKinematic(false);
            brain?.CharacterRig?.SetSuppressGroundLock(false);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, false);
            obstacleCollider = null;
        }

        public override void OnResumed() { }

        private static StringAsset ResolveClimbAlias(AnimationClipSetSO aliasProfile, float obstacleHeight)
        {
            if (obstacleHeight <= 0.6f) return aliasProfile.ClimbUpHalfMeter;
            if (obstacleHeight <= 1.1f) return aliasProfile.ClimbUp1meter;
            return aliasProfile.ClimbUp2meter;
        }
    }
}
