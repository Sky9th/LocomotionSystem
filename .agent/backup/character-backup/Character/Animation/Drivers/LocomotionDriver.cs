using Animancer;
using Game.Character.Animation.Components;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;
using Game.Locomotion.Animation.Config;
using Game.Locomotion.Animation.Core;
using Game.Locomotion.Animation.Layers;
using Game.Locomotion.Animation.Layers.Base;
using Game.Locomotion.Config;
using Game.Locomotion.Motor;

namespace Game.Character.Animation.Drivers
{
    public sealed class LocomotionDriver : ICharacterAnimationDriver
    {
        private readonly LocomotionMotor motor;
        private readonly LocomotionProfile locomotionProfile;
        private readonly LocomotionAliasProfile alias;
        private readonly LocomotionAnimationProfile animationProfile;

        private CharacterAnimationController controller;
        private LocomotionAnimationController locomotionAnimCtrl;
        private bool isActive = true;

        public ECharacterAnimationChannel Channel => ECharacterAnimationChannel.FullBody;
        public EAnimationInterruption Priority => EAnimationInterruption.Locomotion;
        public ECharacterAnimationRequestMode Mode => ECharacterAnimationRequestMode.Continuous;

        public LocomotionDriver(
            LocomotionMotor motor,
            LocomotionProfile locomotionProfile,
            LocomotionAliasProfile alias,
            LocomotionAnimationProfile animationProfile)
        {
            this.motor = motor;
            this.locomotionProfile = locomotionProfile;
            this.alias = alias;
            this.animationProfile = animationProfile;
        }

        public void Initialize(CharacterAnimationController controller)
        {
            this.controller = controller;
        }

        public void Update(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            EnsureInitialized();
            if (locomotionAnimCtrl == null)
            {
                return;
            }

            GameContext context = GameContext.Instance;
            if (context == null)
            {
                return;
            }

            if (!context.TryGetSnapshot(out SCharacterSnapshot snapshot))
            {
                return;
            }

            locomotionAnimCtrl.UpdateAnimations(snapshot, deltaTime);
        }

        public CharacterAnimationRequest BuildRequest()
        {
            return null;
        }

        public void OnInterrupted()
        {
            isActive = false;
        }

        public void OnResumed()
        {
            isActive = true;
        }

        private void EnsureInitialized()
        {
            if (locomotionAnimCtrl != null)
            {
                return;
            }

            if (controller == null || motor == null || locomotionProfile == null)
            {
                return;
            }

            NamedAnimancerComponent animancer = controller.Animancer;
            AnimancerLayer fullBodyLayer = controller.GetFullBodyLayer();
            AnimancerLayer headLookLayer = controller.GetHeadLookLayer();
            AnimancerLayer footLayer = controller.GetFootLayer();

            if (animancer == null || fullBodyLayer == null)
            {
                return;
            }

            locomotionAnimCtrl = new LocomotionAnimationController(
                animancer,
                alias,
                locomotionProfile,
                animationProfile,
                motor,
                new BaseLayer(fullBodyLayer),
                new HeadLookLayer(headLookLayer),
                new FootLayer(footLayer));
        }
    }
}
