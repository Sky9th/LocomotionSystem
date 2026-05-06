using UnityEngine;
using Animancer;
using Game.Character.Animation.Requests;
using Game.Locomotion.Animation.Config;

namespace Game.Character.Animation.Drivers
{
    public sealed class TraversalDriver : BaseCharacterAnimationDriver
    {
        [SerializeField] private LocomotionAliasProfile aliasProfile;

        private Collider obstacleCollider;

        public override int ChannelMask => 1 << 0; // FullBody

        public override void Evaluate(in SCharacterSnapshot snapshot, float dt)
        {
            if (aliasProfile == null) return;

            if (!snapshot.Input.JumpAction.Button.IsRequested) return;

            var phase = snapshot.Locomotion.Discrete.Phase;
            if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
                return;

            var obstacle = snapshot.Kinematic.ForwardObstacleDetection;
            if (!obstacle.CanClimb) return;

            var alias = ResolveClimbAlias(obstacle.ObstacleHeight);
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
        }

        public override void Drive(in SCharacterSnapshot snapshot, float dt) { }

        // TODO: 攀爬动画的Y轴位移不足以让胶囊体完全越过障碍物，
        // 胶囊体仍会被挤出回弹。后续方案：攀爬期间临时缩小胶囊体高度
        // 或切换为 isKinematic 彻底脱离物理。
        public override void OnStarted()
        {
            brain?.CharacterRig?.SetSuppressGroundLock(true);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, true);
        }

        public override void OnCompleted()
        {
            brain?.CharacterRig?.SetSuppressGroundLock(false);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, false);
            obstacleCollider = null;
        }

        public override void OnInterrupted(AnimationRequest by)
        {
            brain?.CharacterRig?.SetSuppressGroundLock(false);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, false);
            obstacleCollider = null;
        }

        public override void OnResumed() { }

        private StringAsset ResolveClimbAlias(float obstacleHeight)
        {
            //if (obstacleHeight <= 0.6f) return aliasProfile.ClimbUp0_5meter;
            if (obstacleHeight <= 1.1f) return aliasProfile.ClimbUp1meter;
            return aliasProfile.ClimbUp2meter;
        }
    }
}
