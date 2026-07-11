using UnityEngine;
using Animancer;
using RedDust.Gameplay.Character.Animation;
using RedDust.Gameplay.Character;
using RedDust.Gameplay.Character.Kinematic;

namespace RedDust.Gameplay.Character.Animation.Drivers.Traversal
{
    internal sealed class TraversalDriver : BaseAnimationDriver
    {
        private Collider obstacleCollider;
        private Vector3 topPoint;
        private bool _isActive;

        private const float ClimbProximityThreshold = 0.3f;
        private const float ClimbFaceAngleThreshold = 0.8f; // cos(36°), 跨引擎标准阈值

        public override int ChannelMask => 1 << 0; // FullBody

        public override void Evaluate(in SCharacterFrameContext ctx, float dt)
        {
            if (_isActive) return;

            var motor = ctx.Motor;
            if (motor.DesiredLocalVelocity.y <= 0.1f) return;

            var phase = ctx.Discrete.Phase;
            if (phase == ELocomotionPhase.Airborne || phase == ELocomotionPhase.Landing)
                return;

            var obs = ctx.Kinematic.ForwardObstacleDetection;
            if (!obs.CanClimb || obs.Distance > ClimbProximityThreshold) return;

            // DesiredLocalVelocity → world direction，用于 dot product 判断正面顶墙 vs 擦墙而过
            var heading = ctx.Kinematic.LocomotionHeading;
            heading.y = 0f;
            if (heading.sqrMagnitude < Mathf.Epsilon) heading = Vector3.forward;
            heading.Normalize();
            var right = Vector3.Cross(Vector3.up, heading);
            var moveDir = (right * motor.DesiredLocalVelocity.x + heading * motor.DesiredLocalVelocity.y).normalized;

            // dot(moveDir, -obsNormal): 1.0=垂直顶入, 0.0=平行滑过, -1.0=背离
            float facePressure = Vector3.Dot(moveDir, -obs.Normal);
            if (facePressure < ClimbFaceAngleThreshold) return;

            var traversalSet = brain?.BuildContext?.TraversalSet;
            if (traversalSet == null) return;

            var climbClip = ResolveClimbClip(traversalSet, obs.ObstacleHeight);
            if (climbClip == null || climbClip.Clip == null) return;

            obstacleCollider = obs.Collider;
            topPoint = obs.TopPoint;

            brain?.SubmitRequest(new AnimationRequest
            {
                Clip = climbClip.Clip,
                FadeIn = 0.1f,
                FadeOut = 0.15f,
                DriverType = EDriverType.Traversal,
                ChannelMask = ChannelMask,
            });
        }

        public override void Drive(in SCharacterFrameContext ctx, float dt) { }

        public override void OnStarted(AnimationRequest request)
        {
            _isActive = true;

            if (request.HasClip && brain != null)
            {
                var state = brain.FullBodyLayer.Play(request.Clip, request.FadeIn);
                state.Time = 0f;
            }

            brain?.CharacterRig?.SetSuppressGroundLock(true);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, true);
            brain?.CharacterRig?.SetKinematic(true);
        }

        public override void OnCompleted()
        {
            _isActive = false;
            brain?.CharacterRig?.SetGroundedY(topPoint.y);
            brain?.CharacterRig?.SetKinematic(false);
            brain?.CharacterRig?.SetSuppressGroundLock(false);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, false);
            obstacleCollider = null;
        }

        public override void OnInterrupted(AnimationRequest by)
        {
            _isActive = false;
            brain?.CharacterRig?.SetKinematic(false);
            brain?.CharacterRig?.SetSuppressGroundLock(false);
            brain?.CharacterRig?.IgnoreCollisionWith(obstacleCollider, false);
            obstacleCollider = null;
        }

        public override void OnResumed() { }

        /// <summary>
        /// 根据障碍物高度选择攀爬动画。阈值与动画绑定，非数据驱动。
        /// </summary>
        private static ClipTransition ResolveClimbClip(LocomotionAnimationSetSO set, float obstacleHeight)
        {
            if (obstacleHeight <= 0.6f) return set.climbUpHalfMeter;
            if (obstacleHeight <= 1.1f) return set.climbUp1meter;
            return set.climbUp2meter;
        }
    }
}
