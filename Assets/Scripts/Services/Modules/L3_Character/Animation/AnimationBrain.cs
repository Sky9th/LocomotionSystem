using UnityEngine;
using Animancer;
using RedDust.Core;
using RedDust.Character.Animation.Drivers;
using RedDust.Character.Animation.Drivers.Locomotion;
using RedDust.Character.Animation.Drivers.Traversal;
using RedDust.Character.Animation;
using RedDust.Character;

namespace RedDust.Character.Animation
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    public sealed class AnimationBrain : ModuleHub, IModuleChild
    {
        // ── Constants ──
        public const int TotalLayerCount = 7;
        public const int FullBody = 0;
        public const int UpperBody = 1;
        public const int Arm = 2;
        public const int Additive = 3;
        public const int Facial = 4;
        public const int HeadLook = 5;
        public const int Footstep = 6;

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
        private AnimancerLayer headLookLayer;

        // ── Core State ──
        private CharacterBuildContext buildCtx;
        private DriverArbiter fullBodyArbiter;

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
        internal CharacterRig CharacterRig => buildCtx?.Rig;
        internal CharacterBuildContext BuildContext => buildCtx;
        public NamedAnimancerComponent Animancer => animancer;
        public AnimancerLayer FullBodyLayer => fullBodyLayer;
        public AnimancerLayer UpperBodyLayer => upperBodyLayer;
        public AnimancerLayer ArmLayer => armLayer;
        public AnimancerLayer HeadLookLayer => headLookLayer;

        protected override void Awake()
        {
            // 自注册到父 Hub (CharacterActor)，使 OnAssemble/OnWire 被纳入生命周期。
            GetComponentInParent<ModuleHub>()?.Registry?.Register(this);

            // 装配 Drivers。AddComponent 立即触发其 Awake，之后 base.Awake() 扫描发现。
            gameObject.AddComponent<LocomotionDriver>();
            gameObject.AddComponent<TraversalDriver>();

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
                headLookLayer = BindLayer(HeadLook, buildCtx.HeadMask);
                BindLayer(Footstep, buildCtx.FootMask);
            }
        }

        protected override void Start()
        {
            base.Start();  // Registry.OnWireAll() → LocomotionDriver.OnWire 创建 BaseLayer

            // 桥接 footstep 回调（依赖 Driver.OnWire 创建的 BaseLayer）
            var locoDriver = GetComponent<LocomotionDriver>();
            if (locoDriver?.BaseLayer != null)
                locoDriver.BaseLayer.FootstepCallback = () => OnFootstep?.Invoke();
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
            float speed = buildCtx.LocomotionAnimConfig != null ? buildCtx.LocomotionAnimConfig.headLookSmoothingSpeed : 12f;
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
