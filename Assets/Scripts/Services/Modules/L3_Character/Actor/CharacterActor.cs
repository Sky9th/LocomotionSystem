using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;
using RedDust.Character.Animation;
using RedDust.Character.Animation.Drivers.Ability;
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
    [RequireComponent(typeof(AbilityReactor))]
    [RequireComponent(typeof(Identity))]
    /// <summary>继承 ModuleHub。pre-assemble 在 base.Awake 之前，post-wire 在 base.Start 之后。</summary>
    public partial class CharacterActor : ModuleHub
    {
        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Config")]
        [SerializeField] private GroundSystemConfigSO groundSystemConfig;

        [Header("Animation")]
        [SerializeField] private CharacterAnimationProfileSO characterAnimationProfile;

        [Header("Ability")]
        [SerializeField] private AbilityTreeSO[] innateTrees;

        [Header("Audio")]
        [SerializeField] private CharacterAudioConfigSO characterAudioConfig;

        [Header("Model")]
        [SerializeField] private GameObject modelPrefab;
        [Header("Animation")]
        [SerializeField] private bool forwardRootMotion = true;
        [SerializeField] private bool applyRootMotionRotation;
        [SerializeField] private bool autoMatchAnimationSpeed = true;

        [Header("Animation Masks")]
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField] private AvatarMask armMask;
        [SerializeField] private AvatarMask additiveMask;
        [SerializeField] private AvatarMask facialMask;
        [SerializeField] private AvatarMask headMask;
        [SerializeField] private AvatarMask footMask;


        [Header("Hierarchy")]
        [SerializeField] private Transform modelRoot;

        // ── Identity ──
        public bool IsPlayer => isPlayer;

        // ── Config SO ──
        internal CharacterAnimationProfileSO CharacterAnimationProfile => characterAnimationProfile;
        internal CharacterAudioConfigSO CharacterAudioConfig => characterAudioConfig;

        // ── Animation ──
        internal bool ForwardRootMotion => forwardRootMotion;
        internal bool ApplyRootMotionRotation => applyRootMotionRotation;
        internal bool AutoMatchAnimationSpeed => autoMatchAnimationSpeed;

        // ── Masks ──
        internal AvatarMask UpperBodyMask => upperBodyMask;
        internal AvatarMask ArmMask => armMask;
        internal AvatarMask AdditiveMask => additiveMask;
        internal AvatarMask FacialMask => facialMask;
        internal AvatarMask HeadMask => headMask;
        internal AvatarMask FootMask => footMask;

        // ── Runtime State ──
        internal CharacterBuildContext BuildContext => buildCtx;
        internal CharacterRig CharacterRig => characterRig;
        internal SCharacterKinematic LastKinematic { get; private set; }
        internal SCharacterMotor LastMotor { get; private set; }
        internal SCharacterDiscrete LastDiscrete { get; private set; }

        // ── Module Access（Command/Query 直接调用）──
        internal PathfindingAgent Pathfinding => pathfindingAgent;
        internal AbilityExecutor Ability => ability;

        // ── Input ──
        internal SCharacterInputState InputState { get; set; } = SCharacterInputState.Default;

        // ── Private Fields ──
        private CharacterRig characterRig;
        private CharacterBuildContext buildCtx;
        private PathfindingAgent pathfindingAgent;
        private CharacterKinematic characterKinematic;
        private ILocomotionSimulator locomotionSimulator;
        private AnimationBrain characterAnimation;
        private AbilityExecutor ability;
        private AbilityReactor reactor;
        private CharacterCombat combat;
        private CharacterEquipment equipment;
        private EventHub eventHub;
        private Identity identity;
        private AbilityForest abilityForest;

        protected override void Awake()
        {
            SetupModel();
            ResolveComponents();

            // Identity 组件在 Awake 时已在 GO 上（Properties 由 EntityService 在 Instantiate 后 push）。
            // ── pre-assemble: 创建 C# 子模块（构造自注册到 Registry）──
            characterRig = new CharacterRig(transform, modelRoot);

            abilityForest = new AbilityForest(innateTrees);

            buildCtx = new CharacterBuildContext(
                root: transform, eventHub: eventHub,
                identity: identity,
                ability: ability, reactor: reactor, pathfinding: pathfindingAgent,
                modelRoot: modelRoot, rig: characterRig,
                animationProfile: characterAnimationProfile,
                groundSystemConfig: groundSystemConfig,
                physique: default,
                audioConfig: characterAudioConfig,
                upperBodyMask: upperBodyMask, armMask: armMask, additiveMask: additiveMask,
                facialMask: facialMask, headMask: headMask, footMask: footMask,
                forwardRootMotion: forwardRootMotion,
                applyRootMotionRotation: applyRootMotionRotation,
                autoMatchAnimationSpeed: autoMatchAnimationSpeed,
                abilityForest: abilityForest
            );

            characterKinematic = new CharacterKinematic(buildCtx, Registry);
            locomotionSimulator = new GroundLocomotion(Registry);
            combat = new CharacterCombat(buildCtx, Registry);
            equipment = new CharacterEquipment(buildCtx, Registry);

            // ModuleHub.Awake: 扫描 ModuleChildMono → Register → OnAssembleAll
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();  // Registry.OnWireAll() — 子模块完成初始化
            buildCtx.Physique = CharacterPhysique.From(buildCtx.Properties);
            buildCtx.Container = identity.Entity?.NestedContainer;
            // TODO: 饥饿/体力是测试代码。Actor 不应内联属性变化逻辑。
            buildCtx.Properties.AddModifier(new FloatModifier { Owner = this, TargetPath = CharacterConst.PropertyPath.Vitals.Hunger, Frequency = ModifierFrequency.PerSecond, Delta = -0.01f });
            buildCtx.Properties.AddModifier(new FloatModifier { Owner = this, TargetPath = CharacterConst.PropertyPath.Vitals.Stamina, Frequency = ModifierFrequency.PerSecond, Delta = 25f });
        }

        private void SetupModel()
        {
            if (modelPrefab == null)
            {
                if (modelRoot == null) modelRoot = transform;
                return;
            }

            var model = Instantiate(modelPrefab, transform);
            model.name = "Model";
            var animancer = model.GetComponent<NamedAnimancerComponent>();
            if (animancer == null)
                model.AddComponent<NamedAnimancerComponent>();
            if (model.GetComponent<AnimationBrain>() == null)
            {
                model.AddComponent<AnimationBrain>();
            }
            modelRoot = model.transform;
        }

        private void ResolveComponents()
        {
            identity = GetComponent<Identity>();
            characterAnimation = GetComponentInChildren<AnimationBrain>();
            eventHub = GetComponent<EventHub>();
            pathfindingAgent = GetComponent<PathfindingAgent>();
            ability = GetComponent<AbilityExecutor>();
            reactor = GetComponent<AbilityReactor>();
        }

        private void OnEnable() { }

        /// <summary>软暂停——仅重置运动学状态。硬销毁走 OnDestroy。</summary>
        private void OnDisable()
        {
            characterKinematic?.Reset();
        }

        private void OnDestroy()
        {
            combat?.UnsubscribeEvents();
            characterKinematic?.Reset();
        }

        public void ReplaceModel(GameObject newModelPrefab)
        {
            // TODO Phase 3: 完整实现（装备系统依赖）
            //   1. Unregister 所有 AnimationDriver from AnimationBrain
            //   2. DestroyImmediate 旧 Model 子节点
            //   3. Instantiate(newModelPrefab) → AnimationBrain.Awake
            //   4. Ensure NamedAnimancerComponent + AnimationBrain on new model
            //   5. 重建 CharacterRig: characterRig = new CharacterRig(transform, newModel.transform)
            //   6. ctx.ModelRoot = newModel.transform; ctx.Rig = characterRig;
            //   7. 所有模块通过 ctx 自动读到新 ModelRoot/Rig
            //   8. 重新调用 OnWire() 递归连线
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= Mathf.Epsilon) return;

            var frameCtx = new SCharacterFrameContext();

            try
            {
                frameCtx.Kinematic = characterKinematic.Evaluate(InputState, deltaTime);

                // 装备同步：以 Container 为数据源，diff 管理 GO + GripTag
                equipment.SyncEquipment();

                var ownedTags = buildCtx.OwnedGripTags;
                buildCtx.BodyForm = EBodyForm.Relax; // TODO: 装备系统决定 BodyForm
                buildCtx.ResolvedLocoAnimSet = buildCtx.GripTable?.Resolve(ownedTags, buildCtx.BodyForm)
                    ?? buildCtx.DefaultLocomotionSet;

                // 装备变化 → 技能重解析（武器树 × 兼容标签）
                buildCtx.AbilityForest?.SetWeaponTags(ownedTags);

                locomotionSimulator.Simulate(ref frameCtx, InputState, buildCtx, deltaTime);

                LastKinematic = frameCtx.Kinematic;
                LastMotor = frameCtx.Motor;
                LastDiscrete = frameCtx.Discrete;

                characterAnimation?.Apply(in frameCtx);
                pathfindingAgent?.SyncLocomotion(in frameCtx.Discrete);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterActor] {name} — 配置缺失或运行时异常，跳过帧:\n{e}", this);
                enabled = false;
            }

            buildCtx.Properties.Tick(deltaTime);
        }

    }
}
