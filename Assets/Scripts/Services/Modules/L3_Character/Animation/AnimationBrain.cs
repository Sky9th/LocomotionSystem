using UnityEngine;
using Animancer;
using RedDust.Character.Animation.Drivers;
using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    public sealed class AnimationBrain : MonoBehaviour
    {
        // ── Constants ──
        public const int TotalLayerCount = 6;
        public const int FullBody = 0;
        public const int UpperBody = 1;
        public const int Additive = 2;
        public const int Facial = 3;
        public const int HeadLook = 4;
        public const int Footstep = 5;

        // ── Serialized ──
        [Header("Dependencies")]
        [SerializeField] private NamedAnimancerComponent animancer;
        [SerializeField] private Animator animator;

        [Header("Animation Config")]
        [SerializeField] private AnimationClipSetSO aliasProfile;
        [SerializeField] private LocomotionAnimationConfigSO animationProfile;

        [Header("Locomotion")]
        [SerializeField] private Locomotion.LocomotionProfileSO locomotionProfile;

        [Header("Root Motion")]
        [SerializeField] private bool forwardRootMotion = true;
        [SerializeField] private bool applyRootMotionRotation = false;
        [SerializeField] private bool autoMatchAnimationSpeed = true;

        [Header("Masks")]
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField] private AvatarMask additiveMask;
        [SerializeField] private AvatarMask facialMask;
        [SerializeField] private AvatarMask headMask;
        [SerializeField] private AvatarMask footMask;

        // ── Animation Layers ──
        private AnimancerLayer fullBodyLayer;
        private AnimancerLayer headLookLayer;
        private AnimancerLayer footstepLayer;

        // ── Core State ──
        private DriverArbiter fullBodyArbiter;
        private CharacterRig characterRig;

        // ── Root Motion Speed Matching ──
        public float SpeedMultiplier { get; private set; } = 1f;
        private EMovementGait lastAppliedGait = (EMovementGait)(-1);
        private object lastAppliedState;

        // ── Head Look ──
        private Vector2MixerState headLookMixer;
        private bool headLookInitialized;
        private float headLookSmoothedYaw;
        private float headLookSmoothedPitch;

        // ── Public Accessors ──
        internal CharacterRig CharacterRig => characterRig;
        public NamedAnimancerComponent Animancer => animancer;
        public AnimancerLayer FullBodyLayer => fullBodyLayer;
        public AnimancerLayer HeadLookLayer => headLookLayer;

        // ── Lifecycle ──

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

            if (locomotionProfile == null)
            {
                var actor = GetComponentInParent<CharacterActor>();
                if (actor != null) locomotionProfile = actor.LocomotionProfile;
            }

            if (headLookLayer != null && aliasProfile != null && aliasProfile.lookMixer != null)
                headLookMixer = headLookLayer.TryPlay(aliasProfile.lookMixer) as Vector2MixerState;
        }

        private void OnAnimatorMove()
        {
            if (!forwardRootMotion || animator == null || characterRig == null) return;

            if (characterRig.SuppressGroundLock)
                characterRig.ApplyPosition(animator.deltaPosition);
            else
                characterRig.ApplyPositionPlanar(animator.deltaPosition);

            if (applyRootMotionRotation)
                characterRig.ApplyRotation(animator.deltaRotation);
        }

        // ── Core API ──

        internal void SetRig(CharacterRig rig)
        {
            characterRig = rig;
        }

        internal void Apply(in CharacterFrameContext ctx)
        {
            fullBodyArbiter.Resolve(ctx, Time.deltaTime);
            UpdateHeadLook(ctx);
            ApplySpeedMultiplier(ctx);
        }

        // ── Head Look ──

        private void UpdateHeadLook(in CharacterFrameContext ctx)
        {
            if (headLookMixer == null) return;

            if (!headLookInitialized)
            {
                FreezeHeadLookChildren();
                headLookInitialized = true;
            }

            Vector2 target = ctx.Kinematic.LookDirection;
            float speed = animationProfile != null ? animationProfile.headLookSmoothingSpeed : 12f;
            float step = speed * Time.deltaTime;

            headLookSmoothedYaw = Mathf.MoveTowards(headLookSmoothedYaw, target.x, step);
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

        // ── Root Motion Speed Matching ──

        private void ApplySpeedMultiplier(in CharacterFrameContext ctx)
        {
            if (!autoMatchAnimationSpeed || fullBodyLayer?.CurrentState == null) return;

            var gait = ctx.Discrete.Gait;
            var state = (object)fullBodyLayer.CurrentState;

            if (gait == lastAppliedGait && state == lastAppliedState) return;
            lastAppliedGait = gait;
            lastAppliedState = state;

            SpeedMultiplier = ctx.Discrete.MotionSpeedScale;
            fullBodyLayer.CurrentState.Speed = SpeedMultiplier;
        }

        // ── Driver Management ──

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

        // ── Helpers ──

        private AnimancerLayer BindLayer(int index, AvatarMask mask)
        {
            var layer = animancer.Layers[index];
            if (mask != null) layer.Mask = mask;
            return layer;
        }
    }
}
