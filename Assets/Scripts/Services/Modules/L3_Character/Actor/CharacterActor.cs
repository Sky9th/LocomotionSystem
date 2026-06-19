using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;
using RedDust.Character.Animation;
using RedDust.Character.Animation.Drivers.Locomotion;
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
    /// <summary>继承 ModuleBehaviour。OnAssemble 在 Awake 末尾，OnWire 在 Start。</summary>
    public partial class CharacterActor : ModuleBehaviour
    {
        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Config")]
        [SerializeField] private Kinematic.GroundSystemConfigSO groundSystemConfig;

        [Header("Animation")]
        [SerializeField] private CharacterAnimationProfileSO characterAnimationProfile;

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

        // ── Identity ──
        public bool IsPlayer => isPlayer;

        // ── Config SO ──
        internal CharacterAnimationProfileSO CharacterAnimationProfile => characterAnimationProfile;
        internal CharacterPhysique Physique { get; private set; }
        internal CharacterAudioConfigSO CharacterAudioConfig => characterAudioConfig;

        // ── Animation ──
        internal bool ForwardRootMotion => forwardRootMotion;
        internal bool ApplyRootMotionRotation => applyRootMotionRotation;
        internal bool AutoMatchAnimationSpeed => autoMatchAnimationSpeed;

        // ── Masks ──
        internal AvatarMask UpperBodyMask => upperBodyMask;
        internal AvatarMask AdditiveMask => additiveMask;
        internal AvatarMask FacialMask => facialMask;
        internal AvatarMask HeadMask => headMask;
        internal AvatarMask FootMask => footMask;

        // ── Runtime ──
        internal CharacterBuildContext BuildContext => buildCtx;
        internal CharacterRig CharacterRig => characterRig;
        public IPropertyReader Props { get; private set; }
        internal SCharacterKinematic LastKinematic { get; private set; }
        internal SCharacterMotor LastMotor { get; private set; }
        internal SCharacterDiscrete LastDiscrete { get; private set; }

        private CharacterBuildContext buildCtx;
        private ICharacterDirector director;
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

        protected override void Awake()
        {
            SetupModel();
            ResolveComponents();
            base.Awake();  // Registry + OnAssemble
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
                animancer = model.AddComponent<NamedAnimancerComponent>();
            if (animancerTransitions != null)
                animancer.Transitions = animancerTransitions;

            if (model.GetComponent<AnimationBrain>() == null)
            {
                Debug.LogWarning($"[CharacterActor] modelPrefab '{modelPrefab.name}' missing AnimationBrain. Adding at runtime.", this);
                model.AddComponent<AnimationBrain>();
            }

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

        public override void OnAssemble()
        {
            characterRig = new CharacterRig(transform, modelRoot);

            Physique = CharacterPhysique.FromAgent(agent);

            buildCtx = new CharacterBuildContext(
                root: transform, eventHub: eventHub, agent: agent,
                ability: ability, reactor: reactor, pathfinding: pathfindingAgent,
                modelRoot: modelRoot, rig: characterRig,
                animationProfile: characterAnimationProfile,
                groundSystemConfig: groundSystemConfig,
                physique: Physique,
                audioConfig: characterAudioConfig,
                upperBodyMask: upperBodyMask, additiveMask: additiveMask,
                facialMask: facialMask, headMask: headMask, footMask: footMask,
                forwardRootMotion: forwardRootMotion,
                applyRootMotionRotation: applyRootMotionRotation,
                autoMatchAnimationSpeed: autoMatchAnimationSpeed,
                skillSlot1: skillSlot1, skillSlot2: skillSlot2
            );

            if (isPlayer) director = new PlayerDirector(buildCtx, Registry);
            else          director = new NpcDirector(buildCtx, Registry);
            characterKinematic = new CharacterKinematic(buildCtx, Registry);
            locomotionSimulator = new GroundLocomotion(Registry);
            combat = new CharacterCombat(buildCtx, Registry);
        }

        public override void OnWire()
        {
            base.OnWire();
            agent.AddModifier(new FloatModifier { Owner = this, TargetPath = "Vitals/Hunger", Frequency = ModifierFrequency.PerSecond, Delta = -0.01f });
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

            var frameCtx = new CharacterFrameContext();

            try
            {
                var intent = director.Evaluate();
                frameCtx.Intent = intent;
                frameCtx.Kinematic = characterKinematic.Evaluate(intent.LocomotionHeading,
                    intent.AimDirection, deltaTime);

                locomotionSimulator.Simulate(ref frameCtx, intent, buildCtx, deltaTime);

                LastKinematic = frameCtx.Kinematic;
                LastMotor = frameCtx.Motor;
                LastDiscrete = frameCtx.Discrete;

                characterAnimation?.Apply(in frameCtx);
                pathfindingAgent?.SyncLocomotion(in frameCtx.Discrete);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CharacterActor] {name} — 配置缺失或运行时异常，跳过帧:\n{e.GetType().Name}: {e.Message}", this);
                enabled = false;
            }
        }
    }
}
