using Animancer;
using Game.Character.Animation.Components;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Discrete.Structs;

namespace Game.Character.Animation.Drivers
{
    public sealed class TraversalDriver : ICharacterAnimationDriver
    {
        private readonly LocomotionAliasProfile alias;

        public ECharacterAnimationChannel Channel => ECharacterAnimationChannel.FullBody;
        public EAnimationInterruption Priority => EAnimationInterruption.Traversal;
        public ECharacterAnimationRequestMode Mode => ECharacterAnimationRequestMode.OneShot;

        public TraversalDriver(LocomotionAliasProfile alias)
        {
            this.alias = alias;
        }

        public void Initialize(CharacterAnimationController controller)
        {
        }

        public void Update(float deltaTime)
        {
        }

        public CharacterAnimationRequest BuildRequest()
        {
            if (alias == null)
            {
                return null;
            }

            GameContext context = GameContext.Instance;
            if (context == null)
            {
                return null;
            }

            if (!context.TryGetSnapshot(out SCharacterSnapshot snapshot))
            {
                return null;
            }

            SLocomotionTraversal traversal = snapshot.Traversal;
            if (traversal.Stage != ELocomotionTraversalStage.Requested
                || traversal.Type != ELocomotionTraversalType.Climb)
            {
                return null;
            }

            var climbAlias = ResolveClimbAlias(traversal.ObstacleHeight);
            if (climbAlias == null)
            {
                return null;
            }

            return CharacterAnimationRequest.CreateAlias(
                "Traversal_Climb",
                ECharacterAnimationSource.Traversal,
                climbAlias,
                ECharacterAnimationChannel.FullBody,
                EAnimationInterruption.Traversal,
                0.1f,
                0.15f,
                null);
        }

        public void OnInterrupted()
        {
        }

        public void OnResumed()
        {
        }

        private StringAsset ResolveClimbAlias(float obstacleHeight)
        {
            if (obstacleHeight <= 0.6f)
            {
                return alias.ClimbUp0_5meter;
            }

            if (obstacleHeight <= 1.1f)
            {
                return alias.ClimbUp1meter;
            }

            return alias.ClimbUp2meter;
        }
    }
}
