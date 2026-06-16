using UnityEngine;
using Animancer;
using RedDust.Core;
using RedDust.Character.Animation.Drivers;
using RedDust.Character.Animation.Drivers.Locomotion;
using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    public sealed class AnimationBrain : ModuleBehaviour
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

        // All other animation config moved to CharacterActor.
        // AnimationBrain reads config from CharacterActor at runtime.

        // ── Animation Layers ──
        private AnimancerLayer fullBodyLayer;
        private AnimancerLayer headLookLayer;

        // ── Core State ──
        private DriverArbiter fullBodyArbiter;
        // Rig 通过 _actor.CharacterRig → ctx.Rig 实时读取，不缓存

        // ── Root Motion Speed Matching ──
        public float SpeedMultiplier { get; private set; } = 1f;
        private EMovementGait lastAppliedGait = (EMovementGait)(-1);
        private object lastAppliedState;

        // ── Head Look ──
        private Vector2MixerState headLookMixer;
        private bool headLookInitialized;
        private float headLookSmoothedYaw;
        private float headLookSmoothedPitch;

        // ── Events ──
        public event System.Action OnFootstep;

        // ── Public Accessors ──
        internal CharacterRig CharacterRig => _actor != null ? _actor.CharacterRig : null;
        public NamedAnimancerComponent Animancer => animancer;
        public AnimancerLayer FullBodyLayer => fullBodyLayer;
        public AnimancerLayer HeadLookLayer => headLookLayer;

        // ── Lifecycle ──

        private CharacterActor _actor;

        protected override void Awake()
        {
            _actor = GetComponentInParent<CharacterActor>();
            base.Awake();  // Registry + OnAssemble（图层初始化 + 添加 Drivers）+ OnAssembleAll
        }

        public override void OnAssemble()
        {
            if (_actor == null) return;

            // Rig 不在此处捕获——CharacterActor.OnAssemble 中通过 ctx.Rig 注入。
            // AnimationBrain 通过 _actor.CharacterRig 实时读取。

            if (animancer == null) animancer = GetComponentInChildren<NamedAnimancerComponent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animancer == null) return;

            animancer.Layers.SetMinCount(TotalLayerCount);

            fullBodyLayer = animancer.Layers[FullBody];
            fullBodyArbiter = new DriverArbiter(fullBodyLayer);

            BindLayer(UpperBody, _actor.UpperBodyMask);
            BindLayer(Additive, _actor.AdditiveMask);
            BindLayer(Facial, _actor.FacialMask);
            headLookLayer = BindLayer(HeadLook, _actor.HeadMask);
            BindLayer(Footstep, _actor.FootMask);

            var aliasProfile = _actor.AnimationAliasProfile;
            if (headLookLayer != null && aliasProfile != null && aliasProfile.lookMixer != null)
                headLookMixer = headLookLayer.TryPlay(aliasProfile.lookMixer) as Vector2MixerState;

            // Drivers 是 AnimationBrain 的子模块，通过代码挂载随 Model 生灭
            gameObject.AddComponent<LocomotionDriver>();
            gameObject.AddComponent<TraversalDriver>();
        }

        private void OnAnimatorMove()
        {
            var rig = CharacterRig;
            if (!_actor.ForwardRootMotion || animator == null || rig == null) return;

            if (rig.SuppressGroundLock)
                rig.ApplyPosition(animator.deltaPosition);
            else
                rig.ApplyPositionPlanar(animator.deltaPosition);

            if (_actor.ApplyRootMotionRotation)
                rig.ApplyRotation(animator.deltaRotation);
        }

        public override void OnWire()
        {
            base.OnWire();  // LocomotionDriver.OnWire 创建 BaseLayer
            // TODO: 桥接 BaseLayer.FootstepCallback → AnimationBrain.OnFootstep 事件
            // 当前是临时方案——未来 BaseLayer 应通过 EventHub 发布，去掉这层桥接。
            var locoDriver = GetComponent<LocomotionDriver>();
            if (locoDriver?.BaseLayer != null)
                locoDriver.BaseLayer.FootstepCallback = () => OnFootstep?.Invoke();
        }

        // ── Core API ──

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
            var animProfile = _actor.LocomotionAnimationProfile;
            float speed = animProfile != null ? animProfile.headLookSmoothingSpeed : 12f;
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
            if (!_actor.AutoMatchAnimationSpeed || fullBodyLayer?.CurrentState == null) return;

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
