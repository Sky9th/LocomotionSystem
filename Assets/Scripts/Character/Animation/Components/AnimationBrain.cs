using UnityEngine;
using Animancer;
using Game.Character.Animation.Drivers;
using Game.Character.Animation.Requests;
using Game.Character.Components;
using Game.Locomotion.Animation.Config;

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

        [Header("Animation Config")]
        [SerializeField] private AnimationAliasProfile aliasProfile;
        [SerializeField] private LocomotionAnimationProfile animationProfile;

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
        private Vector2MixerState headLookMixer;
        private bool headLookInitialized;
        private float headLookSmoothedYaw;
        private float headLookSmoothedPitch;
        internal CharacterRig CharacterRig => characterRig;

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

            if (headLookLayer != null && aliasProfile != null && aliasProfile.lookMixer != null)
            {
                headLookMixer = headLookLayer.TryPlay(aliasProfile.lookMixer) as Vector2MixerState;
            }
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
            if (headLookMixer == null) return;

            if (!headLookInitialized)
            {
                FreezeHeadLookChildren();
                headLookInitialized = true;
            }

            Vector2 target = snapshot.Kinematic.LookDirection;
            float speed = animationProfile != null ? animationProfile.headLookSmoothingSpeed : 12f;
            float step = speed * Time.deltaTime;

            headLookSmoothedYaw   = Mathf.MoveTowards(headLookSmoothedYaw,   target.x, step);
            headLookSmoothedPitch = Mathf.MoveTowards(headLookSmoothedPitch, target.y, step);

            headLookMixer.Parameter = new Vector2(headLookSmoothedYaw, headLookSmoothedPitch);
        }

        private void FreezeHeadLookChildren()
        {
            if (headLookMixer == null) return;
            for (int i = 0; i < headLookMixer.ChildCount; i++)
            {
                var child = headLookMixer.GetChild(i);
                child.Speed = 0f;
                child.Weight = 1f;
                child.NormalizedTime = 1f;
            }
        }

        private void OnAnimatorMove()
        {
            if (!forwardRootMotion || animator == null || characterRig == null) return;

            if (characterRig.SuppressGroundLock)
                characterRig.ApplyPosition(animator.deltaPosition);
            else
                characterRig.ApplyPositionPlanar(animator.deltaPosition);

            characterRig.ApplyRotation(animator.deltaRotation);
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
