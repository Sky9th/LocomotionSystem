using RedDust.Core.Modules;
using UnityEngine;
using Animancer;
using RedDust.Core.Events;
using RedDust.Gameplay.Character.Animation.Drivers;
using RedDust.Gameplay.Character.Animation.Drivers.Locomotion;
using RedDust.Gameplay.Character.Animation.Drivers.Traversal;
using RedDust.Gameplay.Character.Animation.Drivers.Ability;
using RedDust.Gameplay.Character.Animation.Drivers.HitReaction;
using RedDust.Gameplay.Character.Animation;
using RedDust.Gameplay.Character;
using RedDust.Gameplay.Character.Kinematic;

namespace RedDust.Gameplay.Character.Animation
{
    [DisallowMultipleComponent]
    public sealed class AnimationBrain : ModuleHub, IModuleChild
    {
        // ── Constants ──
        public const int TotalLayerCount = 6;
        public const int FullBody = 0;
        public const int UpperBody = 1;
        public const int Arm = 2;
        public const int Additive = 3;
        public const int Facial = 4;
        // HeadLook = 5 — 已移除。Head Look IK 延后（俯视角游戏优先级低），将来用 Animation Rigging MultiAimConstraint 实现。
        public const int Footstep = 5;

        // ── Serialized ──
        [Header("Dependencies")]
        [SerializeField] private NamedAnimancerComponent animancer;
        [SerializeField] private Animator animator;

        // All other animation config moved to CharacterActor.
        // AnimationBrain reads config from CharacterActor at runtime.

        // ── Animation Layers ──
        private AnimancerLayer fullBodyLayer;
        private AnimancerLayer upperBodyLayer;
        private AnimancerLayer armLayer;

        // ── Core State ──
        private CharacterBuildContext buildCtx;
        private DriverArbiter fullBodyArbiter;

        // ── Root Motion Speed Matching ──
        public float SpeedMultiplier { get; private set; } = 1f;
        private EMovementGait lastAppliedGait = (EMovementGait)(-1);
        private object lastAppliedState;

        // ── Events ──
        public event System.Action OnFootstep;

        // ── Public Accessors ──
        internal CharacterRig CharacterRig => buildCtx?.Rig;
        internal CharacterBuildContext BuildContext => buildCtx;
        public NamedAnimancerComponent Animancer => animancer;
        public AnimancerLayer FullBodyLayer => fullBodyLayer;
        public AnimancerLayer UpperBodyLayer => upperBodyLayer;
        public AnimancerLayer ArmLayer => armLayer;

        // HeadLookLayer — 已移除。将来用 Animation Rigging MultiAimConstraint 实现 IK。

        protected override void Awake()
        {
            // 自注册到父 Hub (CharacterActor)，使 OnAssemble/OnWire 被纳入生命周期。
            GetComponentInParent<ModuleHub>()?.Registry?.Register(this);

            // 装配 Drivers。AddComponent 立即触发其 Awake，之后 base.Awake() 扫描发现。
            gameObject.AddComponent<LocomotionDriver>();
            gameObject.AddComponent<TraversalDriver>();
            gameObject.AddComponent<AbilityDriver>();
            gameObject.AddComponent<HitReactionDriver>();

            base.Awake();  // 扫描 ModuleChildMono → Register → OnAssembleAll
        }

        public void OnAssemble() { }

        public void OnWire()
        {
            // 由父 Hub (CharacterActor) 的 OnWireAll 驱动。
            // 解析父 Context、设置动画图层，子 Driver 的 OnWire 依赖这些资源就位。
            buildCtx = GetComponentInParent<CharacterActor>()?.BuildContext;

            if (animancer == null) animancer = GetComponentInChildren<NamedAnimancerComponent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animancer != null)
            {
                animancer.Layers.SetMinCount(TotalLayerCount);

                fullBodyLayer = animancer.Layers[FullBody];
                fullBodyArbiter = new DriverArbiter(fullBodyLayer);

                upperBodyLayer = BindLayer(UpperBody, buildCtx.UpperBodyMask);
                armLayer = BindLayer(Arm, buildCtx.ArmMask);
                BindLayer(Additive, buildCtx.AdditiveMask);
                BindLayer(Facial, buildCtx.FacialMask);
                // HeadLook layer 已移除。将来用 Animation Rigging MultiAimConstraint 实现 IK。
                BindLayer(Footstep, buildCtx.FootMask);
            }
        }

        protected override void Start()
        {
            base.Start();  // Registry.OnWireAll() → LocomotionDriver.OnWire 创建 BaseLayer

            // Footstep 事件桥接延后（俯视角游戏脚步声优先级低，除非机器人等特殊角色）。
            // 保留 OnFootstep 事件签名 + BaseLayer.FootstepCallback 基础设施，将来需要时重新接线。
            // var locoDriver = GetComponent<LocomotionDriver>();
            // if (locoDriver?.BaseLayer != null)
            //     locoDriver.BaseLayer.FootstepCallback = () => OnFootstep?.Invoke();
        }

        private void OnAnimatorMove()
        {
            if (buildCtx == null) return;
            var rig = buildCtx.Rig;
            if (!buildCtx.ForwardRootMotion || animator == null || rig == null) return;

            if (rig.SuppressGroundLock)
                rig.ApplyPosition(animator.deltaPosition);
            else
                rig.ApplyPositionPlanar(animator.deltaPosition);

            if (buildCtx.ApplyRootMotionRotation)
                rig.ApplyRotation(animator.deltaRotation);
        }

        // ── Core API ──

        internal void Apply(in SCharacterFrameContext ctx)
        {
            fullBodyArbiter.Resolve(ctx, Time.deltaTime);
            // UpdateHeadLook(ctx); — Head Look IK 延后（俯视角游戏优先级低）
            ApplySpeedMultiplier(ctx);
        }

        // Head Look IK 延后（俯视角游戏优先级低）。
        // 将来用 Unity Animation Rigging 包：MultiAimConstraint + RigBuilder 驱动头骨 IK。
        // 旧 Vector2MixerState 方案已移除（UpdateHeadLook / FreezeHeadLookChildren）。

        // ── Root Motion Speed Matching ──

        private void ApplySpeedMultiplier(in SCharacterFrameContext ctx)
        {
            if (!buildCtx.AutoMatchAnimationSpeed || fullBodyLayer?.CurrentState == null) return;

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

        /// <summary>提交 AnimationRequest。Brain 根据 DriverType 内部解析对应 Driver，调用方无需持有 Driver 引用。</summary>
        internal void SubmitRequest(AnimationRequest request)
        {
            if (request == null) return;
            var driver = request.DriverType switch
            {
                EDriverType.Ability => (ICharacterAnimationDriver)GetComponent<AbilityDriver>(),
                EDriverType.Traversal => GetComponent<TraversalDriver>(),
                EDriverType.HitReaction => (ICharacterAnimationDriver)GetComponent<HitReactionDriver>(),
                _ => null
            };
            if (driver != null) fullBodyArbiter?.SubmitRequest(driver, request);
        }

        internal void Release()
        {
            fullBodyArbiter?.ReleaseActive();
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
