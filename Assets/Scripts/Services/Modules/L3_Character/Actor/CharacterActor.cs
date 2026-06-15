using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;
using RedDust.Character.Animation;
using RedDust.Character;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;
using RedDust.Character.Pathfinding;
using RedDust.Character.Locomotion;
using RedDust.Ability;
using RedDust.Character.Audio;
using RedDust.Character.Combat;
using RedDust.Properties;
using Animancer;
using Animancer.TransitionLibraries;

namespace RedDust.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventHub))]
    [RequireComponent(typeof(PropertyAgent))]
    /// <summary>
    /// TODO: Character 模块生命周期管理
    /// 当前所有初始化堆在 Awake 里，依赖 Unity 隐式调用顺序（不可靠，已导致 EventHub 时序 bug）。
    /// 需要显式分阶段：
    ///   Phase 0 — 预配置加载（modelPrefab 实例化、序列化字段就绪）
    ///   Phase 1 — 核心服务就绪后（EventHub、PropertyAgent 完成初始化）
    ///   Phase 2 — 子模块注入（Director、Combat、Kinematic 等依赖 Phase 1 服务的东西）
    /// 方案候选：手动 Init() 链 / [DefaultExecutionOrder] / 自检重试 / 服务定位器。
    /// </summary>
    public partial class CharacterActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Config")]
        [SerializeField] private CharacterProfileSO characterProfile;

        [Header("Locomotion")]
        [SerializeField] private LocomotionAnimationConfigSO locomotionAnimationProfile;

        // TODO: 临时方案 — 技能树/装备系统完成后替换为技能槽位子系统
        [Header("Ability Slots (Temp)")]
        [SerializeField] private AbilityDefSO skillSlot1;
        [SerializeField] private AbilityDefSO skillSlot2;

        [Header("Audio")]
        [SerializeField] private CharacterAudioConfigSO characterAudioConfig;

        [Header("Model")]
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private TransitionLibraryAsset animancerTransitions;

        [Header("Animation")]
        [SerializeField] private AnimationClipSetSO animationAliasProfile;
        [SerializeField] private bool forwardRootMotion = true;
        [SerializeField] private bool applyRootMotionRotation;
        [SerializeField] private bool autoMatchAnimationSpeed = true;

        [Header("Animation Masks")]
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField] private AvatarMask additiveMask;
        [SerializeField] private AvatarMask facialMask;
        [SerializeField] private AvatarMask headMask;
        [SerializeField] private AvatarMask footMask;

        [Header("Hierarchy")]
        [SerializeField] private Transform modelRoot;

        public bool IsPlayer => isPlayer;
        internal AbilityDefSO SkillSlot1 => skillSlot1;
        internal AbilityDefSO SkillSlot2 => skillSlot2;
        internal LocomotionAnimationConfigSO LocomotionAnimationProfile => locomotionAnimationProfile;

        // Animation config — consumed by AnimationBrain
        internal AnimationClipSetSO AnimationAliasProfile => animationAliasProfile;
        internal bool ForwardRootMotion => forwardRootMotion;
        internal bool ApplyRootMotionRotation => applyRootMotionRotation;
        internal bool AutoMatchAnimationSpeed => autoMatchAnimationSpeed;
        internal AvatarMask UpperBodyMask => upperBodyMask;
        internal AvatarMask AdditiveMask => additiveMask;
        internal AvatarMask FacialMask => facialMask;
        internal AvatarMask HeadMask => headMask;
        internal AvatarMask FootMask => footMask;
        internal CharacterAudioConfigSO CharacterAudioConfig => characterAudioConfig;
        public IPropertyReader Props { get; private set; }
        internal SCharacterKinematic LastKinematic { get; private set; }
        internal SCharacterMotor LastMotor { get; private set; }
        internal SCharacterDiscrete LastDiscrete { get; private set; }
        internal LocomotionProfileSO LocomotionProfile => characterProfile?.locomotion;

        private PlayerDirector director;
        private PathfindingAgent pathfindingAgent;
        private CharacterRig characterRig;
        private CharacterKinematic characterKinematic;
        private ILocomotionSimulator locomotionSimulator;
        private AnimationBrain characterAnimation;
        private PropertyAgent agent;
        private AbilityExecutor ability;
        private AbilityReactor reactor;
        private CharacterCombat combat;
        private EventHub eventHub;
        internal AbilityExecutor AbilityExecutor => ability;

        private void Awake()
        {
            SetupModel();
            ResolveComponents();
            SetupAnimation();
            SetupModules();
        }

        private void SetupModel()
        {
            if (modelPrefab == null)
            {
                if (modelRoot == null) modelRoot = transform;
                return;
            }

            // 立即删除旧的硬编码 Model 子节点（Destroy 延迟执行会导致新旧 AnimationBrain 并存）
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "Model" || child.GetComponent<Animator>() != null)
                    DestroyImmediate(child.gameObject);
            }

            var model = Instantiate(modelPrefab, transform);
            model.name = "Model";

            var animancer = model.GetComponent<NamedAnimancerComponent>();
            if (animancer == null)
                animancer = model.AddComponent<NamedAnimancerComponent>();
            if (animancerTransitions != null)
                animancer.Transitions = animancerTransitions;

            if (model.GetComponent<AnimationBrain>() == null)
                model.AddComponent<AnimationBrain>();

            modelRoot = model.transform;
        }

        private void ResolveComponents()
        {
            characterAnimation = GetComponentInChildren<AnimationBrain>();
            eventHub = GetComponent<EventHub>();
            pathfindingAgent = GetComponent<PathfindingAgent>();
            Props = agent = GetComponent<PropertyAgent>();
            ability = GetComponent<AbilityExecutor>();
            reactor = GetComponent<AbilityReactor>();
        }

        private void SetupAnimation()
        {
            characterRig = new CharacterRig(transform, modelRoot);
            characterAnimation?.SetRig(characterRig);
        }

        private void SetupModules()
        {
            director = new PlayerDirector(eventHub, modelRoot, this);
            characterKinematic = new CharacterKinematic(transform, modelRoot, characterRig);
            locomotionSimulator = new GroundLocomotion();
            combat = new CharacterCombat(ability, reactor, agent, eventHub);
        }

        private void Start()
        {
            // TODO: 修改器应统一管理模块注入，不应散落在 Actor 中
            agent.AddModifier(new FloatModifier { Owner = this, TargetPath = "Vitals/Hunger", Frequency = ModifierFrequency.PerSecond, Delta = -0.01f });
            combat?.SubscribeEvents();
        }

        private void OnEnable() { }

        private void OnDisable()
        {
            characterKinematic?.Reset();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= Mathf.Epsilon) return;

            var ctx = new CharacterFrameContext();

            var intent = director.Evaluate();
            ctx.Intent = intent;
            ctx.LocomotionProfile = characterProfile?.locomotion;
            ctx.LocomotionAnimationProfile = locomotionAnimationProfile;
            ctx.KinematicProfile = characterProfile?.kinematic;
            ctx.Kinematic = characterKinematic.Evaluate(characterProfile?.kinematic, intent.LocomotionHeading,
                intent.AimDirection, deltaTime);

            locomotionSimulator.Simulate(ref ctx, intent, characterProfile?.locomotion, deltaTime);

            LastKinematic = ctx.Kinematic;
            LastMotor = ctx.Motor;
            LastDiscrete = ctx.Discrete;
            
            characterAnimation?.Apply(in ctx);
            pathfindingAgent?.SyncLocomotion(in ctx.Discrete);
        }
    }
}
