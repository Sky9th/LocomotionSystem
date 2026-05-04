using UnityEngine;
using Animancer;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;
using Game.Character.Components;

namespace Game.Character.Animation.Components
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    public sealed class AnimationBrain : MonoBehaviour
    {
        public const int TotalLayerCount = 6;
        public const int FullBody  = 0;
        public const int UpperBody = 1;
        public const int Additive  = 2;
        public const int Facial    = 3;
        public const int HeadLook  = 4;
        public const int Footstep  = 5;

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
        private AnimancerLayer headLookLayer;
        private AnimancerLayer footstepLayer;
        private DriverArbiter fullBodyArbiter;
        private CharacterRig characterRig;

        public NamedAnimancerComponent Animancer => animancer;
        public AnimancerLayer FullBodyLayer => fullBodyLayer;
        public AnimancerLayer HeadLookLayer => headLookLayer;

        private void Awake()
        {
            if (animancer == null) animancer = GetComponentInChildren<NamedAnimancerComponent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (animancer == null) return;

            animancer.Layers.SetMinCount(TotalLayerCount);

            fullBodyLayer = animancer.Layers[FullBody];
            fullBodyArbiter = new DriverArbiter(fullBodyLayer);

            BindLayer(UpperBody, upperBodyMask);
            BindLayer(Additive, additiveMask);
            BindLayer(Facial, facialMask);
            headLookLayer = BindLayer(HeadLook, headMask);
            footstepLayer = BindLayer(Footstep, footMask);
        }

        internal void SetRig(CharacterRig rig)
        {
            characterRig = rig;
        }

        internal void Apply(in SCharacterSnapshot snapshot)
        {
            fullBodyArbiter.Resolve(snapshot, Time.deltaTime);
            UpdateHeadLook(snapshot);
        }

        private void UpdateHeadLook(in SCharacterSnapshot snapshot)
        {
            // TODO: 需 LocomotionAliasProfile.lookMixer — LocomotionDriver 初始化时设置 Vector2Mixer
        }

        private void OnAnimatorMove()
        {
            if (!forwardRootMotion || animator == null || characterRig == null) return;

            var delta = animator.deltaPosition;
            if (applyRootMotionPlanarPositionOnly)
                characterRig.ApplyModelPositionPlanar(delta);
            else
                characterRig.ApplyModelPosition(delta);

            characterRig.ApplyModelRotation(animator.deltaRotation);
        }

        internal void RegisterDriver(ICharacterAnimationDriver driver)
        {
            fullBodyArbiter?.RegisterDriver(driver);
        }

        internal void UnregisterDriver(ICharacterAnimationDriver driver)
        {
            fullBodyArbiter?.UnregisterDriver(driver);
        }

        internal void SubmitRequest(ICharacterAnimationDriver driver, AnimationRequest request)
        {
            fullBodyArbiter?.SubmitRequest(driver, request);
        }

        internal void Release(ICharacterAnimationDriver driver)
        {
            fullBodyArbiter?.Release(driver);
        }

        private AnimancerLayer BindLayer(int index, AvatarMask mask)
        {
            var layer = animancer.Layers[index];
            if (mask != null) layer.Mask = mask;
            return layer;
        }
    }
}
