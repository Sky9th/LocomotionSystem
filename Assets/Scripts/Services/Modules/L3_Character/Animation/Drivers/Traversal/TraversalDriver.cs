using UnityEngine;
using Animancer;
using RedDust.Character.Animation;
using RedDust.Character;
using RedDust.Character.Kinematic;

namespace RedDust.Character.Animation.Drivers.Traversal
{
    internal sealed class TraversalDriver : BaseAnimationDriver
    {
        private Collider obstacleCollider;
        private Vector3 topPoint;

        public override int ChannelMask => 1 << 0; // FullBody

        // TODO: migrated to LocomotionAnimationSetSO traversal fields
        public override void Evaluate(in SCharacterFrameContext ctx, float dt)
        {
            // var aliasProfile = brain?.BuildContext?.AnimationAlias;
            // ...
        }

        public override void Drive(in SCharacterFrameContext ctx, float dt) { }

        public override void OnStarted(AnimationRequest request)
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

        // TODO: migrated to LocomotionAnimationSetSO traversal fields
        /* private static StringAsset ResolveClimbAlias(AnimationClipSetSO aliasProfile, float obstacleHeight)
        {
            ...
        } */
    }
}
