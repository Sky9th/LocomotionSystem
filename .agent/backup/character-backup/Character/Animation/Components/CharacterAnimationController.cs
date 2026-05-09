using UnityEngine;
using Animancer;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;
using Game.Locomotion.Motor;

namespace Game.Character.Animation.Components
{
    public static class AnimationLayerIndex
    {
        public const int FullBody  = 0;
        public const int UpperBody = 1;
        public const int Additive  = 2;
        public const int Facial    = 3;
        public const int HeadLook  = 4;
        public const int Footstep  = 5;
    }

    [DisallowMultipleComponent]
    public sealed class CharacterAnimationController : MonoBehaviour
    {
        public const int TotalLayerCount = 6;

        [Header("Dependencies")]
        [SerializeField] private NamedAnimancerComponent animancer;
        [SerializeField] private Animator animator;

        [Header("Root Motion")]
        [SerializeField] private bool forwardRootMotion = true;
        [SerializeField] private bool applyRootMotionPlanarPositionOnly = true;

        [Header("Masks")]
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField] private AvatarMask additiveMask;
        [SerializeField] private AvatarMask facialMask;
        [SerializeField] private AvatarMask headMask;
        [SerializeField] private AvatarMask footMask;

        private AnimancerLayer fullBodyLayer;
        private AnimancerLayer upperBodyLayer;
        private AnimancerLayer additiveLayer;
        private AnimancerLayer facialLayer;
        private AnimancerLayer headLookLayer;
        private AnimancerLayer footstepLayer;

        private DriverArbiter fullBodyArbiter;
        private DriverArbiter upperBodyArbiter;
        private DriverArbiter additiveArbiter;
        private DriverArbiter facialArbiter;

        private LocomotionMotor motor;

        public NamedAnimancerComponent Animancer => animancer;
        public Animator Animator => animator;

        internal void SetMotor(LocomotionMotor motor)
        {
            this.motor = motor;
        }

        private void Awake()
        {
            if (animancer == null)
            {
                animancer = GetComponentInChildren<NamedAnimancerComponent>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            ConfigureRuntimeLayers();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            fullBodyArbiter?.Update(deltaTime);
            upperBodyArbiter?.Update(deltaTime);
            additiveArbiter?.Update(deltaTime);
            facialArbiter?.Update(deltaTime);
        }

        public AnimancerLayer GetLayer(int index)
        {
            return index switch
            {
                AnimationLayerIndex.FullBody  => fullBodyLayer,
                AnimationLayerIndex.UpperBody => upperBodyLayer,
                AnimationLayerIndex.Additive  => additiveLayer,
                AnimationLayerIndex.Facial    => facialLayer,
                AnimationLayerIndex.HeadLook  => headLookLayer,
                AnimationLayerIndex.Footstep  => footstepLayer,
                _ => null
            };
        }

        public AnimancerLayer GetFullBodyLayer()
            => fullBodyLayer;

        public AnimancerLayer GetUpperBodyLayer()
            => upperBodyLayer;

        public AnimancerLayer GetAdditiveLayer()
            => additiveLayer;

        public AnimancerLayer GetFacialLayer()
            => facialLayer;

        public AnimancerLayer GetHeadLookLayer()
            => headLookLayer;

        public AnimancerLayer GetFootLayer()
            => footstepLayer;

        public void RegisterDriver(ICharacterAnimationDriver driver)
        {
            if (driver == null)
            {
                return;
            }

            driver.Initialize(this);

            switch (driver.Channel)
            {
                case ECharacterAnimationChannel.FullBody:
                    fullBodyArbiter?.RegisterDriver(driver);
                    break;
                case ECharacterAnimationChannel.UpperBody:
                    upperBodyArbiter?.RegisterDriver(driver);
                    break;
                case ECharacterAnimationChannel.Additive:
                    additiveArbiter?.RegisterDriver(driver);
                    break;
                case ECharacterAnimationChannel.Facial:
                    facialArbiter?.RegisterDriver(driver);
                    break;
            }
        }

        private void ConfigureRuntimeLayers()
        {
            if (animancer == null)
            {
                return;
            }

            animancer.Layers.SetMinCount(TotalLayerCount);

            BindDriverLayer(AnimationLayerIndex.FullBody,  mask: null);
            fullBodyArbiter = new DriverArbiter(ECharacterAnimationChannel.FullBody, fullBodyLayer);

            BindDriverLayer(AnimationLayerIndex.UpperBody, upperBodyMask);
            upperBodyArbiter = new DriverArbiter(ECharacterAnimationChannel.UpperBody, upperBodyLayer);

            BindDriverLayer(AnimationLayerIndex.Additive,  additiveMask);
            additiveArbiter = new DriverArbiter(ECharacterAnimationChannel.Additive, additiveLayer);

            BindDriverLayer(AnimationLayerIndex.Facial,    facialMask);
            facialArbiter = new DriverArbiter(ECharacterAnimationChannel.Facial, facialLayer);

            BindFixedLayer(AnimationLayerIndex.HeadLook,   headMask, out headLookLayer);
            BindFixedLayer(AnimationLayerIndex.Footstep,   footMask, out footstepLayer);
        }

        private void BindDriverLayer(int index, AvatarMask mask)
        {
            AnimancerLayer layer = animancer.Layers[index];
            layer.Mask = mask;

            switch (index)
            {
                case AnimationLayerIndex.FullBody:
                    fullBodyLayer = layer;
                    break;
                case AnimationLayerIndex.UpperBody:
                    upperBodyLayer = layer;
                    break;
                case AnimationLayerIndex.Additive:
                    additiveLayer = layer;
                    break;
                case AnimationLayerIndex.Facial:
                    facialLayer = layer;
                    break;
            }
        }

        private void BindFixedLayer(int index, AvatarMask mask, out AnimancerLayer layer)
        {
            layer = animancer.Layers[index];
            layer.Mask = mask;
        }

        private void OnAnimatorMove()
        {
            if (!forwardRootMotion)
            {
                return;
            }

            if (animator == null || motor == null)
            {
                return;
            }

            Vector3 deltaPosition = animator.deltaPosition;
            Quaternion deltaRotation = animator.deltaRotation;

            motor.ApplyDeltaPosition(deltaPosition);
            motor.ApplyDeltaRotation(deltaRotation);
        }
    }
}