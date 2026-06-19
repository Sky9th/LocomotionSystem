using RedDust.Ability;
using RedDust.Core;
using RedDust.Character.Animation;
using RedDust.Character.Audio;
using RedDust.Character.Kinematic;
using RedDust.Character.Pathfinding;
using RedDust.Properties;
using UnityEngine;

namespace RedDust.Character
{
    /// <summary>
    /// Character 子模块统一依赖上下文。
    /// class 而非 struct —— ModelRoot/Rig 会在 Model 替换时原地更新，
    /// 所有持有引用的模块自动读到最新值。
    /// </summary>
    internal class CharacterBuildContext
    {
        // ── 同 GameObject 组件（静态，构造期确定，永不变） ──
        public Transform Root { get; }
        public EventHub EventHub { get; }
        public PropertyAgent Agent { get; }
        public AbilityExecutor Ability { get; }
        public AbilityReactor Reactor { get; }
        public PathfindingAgent Pathfinding { get; }

        // ── 动态引用（Model 替换时原地更新） ──
        public Transform ModelRoot { get; internal set; }
        public CharacterRig Rig { get; internal set; }

        // ── Animation / Audio config（构造期确定） ──
        public CharacterAnimationProfileSO AnimationProfile { get; }
        public LocomotionAnimationConfigSO LocomotionAnimConfig => AnimationProfile?.locomotionConfig;
        public AnimationModeConfigSO[] ModeProfiles => AnimationProfile?.modeProfiles;
        public LocomotionAnimationSetSO DefaultLocomotionSet => AnimationProfile?.defaultLocomotionSet;
        public GripAnimationTableSO GripTable => AnimationProfile?.gripTable;
        public TraversalAnimationSetSO TraversalSet => AnimationProfile?.traversalSet;

        // ── 系统级物理配置（世界定义，所有角色共享） ──
        public GroundSystemConfigSO GroundSystemConfig { get; }

        // TODO: Properties 接入更多属性后（负重、移速修正等）在此追加字段。
        // 角色物理属性缓存——从 PropertyAgent 读取一次，hot path 零开销 struct 字段访问。
        public CharacterPhysique Physique { get; }
        public CharacterAudioConfigSO AudioConfig { get; }
        public AvatarMask UpperBodyMask { get; }
        public AvatarMask AdditiveMask { get; }
        public AvatarMask FacialMask { get; }
        public AvatarMask HeadMask { get; }
        public AvatarMask FootMask { get; }
        public bool ForwardRootMotion { get; }
        public bool ApplyRootMotionRotation { get; }
        public bool AutoMatchAnimationSpeed { get; }

        // ── 临时配置（TODO: 技能树/装备系统完成后由 AbilitySlotManager 替代） ──
        public AbilityDefSO SkillSlot1 { get; }
        public AbilityDefSO SkillSlot2 { get; }

        internal CharacterBuildContext(
            Transform root, EventHub eventHub, PropertyAgent agent,
            AbilityExecutor ability, AbilityReactor reactor, PathfindingAgent pathfinding,
            Transform modelRoot, CharacterRig rig,
            CharacterAnimationProfileSO animationProfile,
            GroundSystemConfigSO groundSystemConfig,
            CharacterPhysique physique,
            CharacterAudioConfigSO audioConfig,
            AvatarMask upperBodyMask, AvatarMask additiveMask,
            AvatarMask facialMask, AvatarMask headMask, AvatarMask footMask,
            bool forwardRootMotion, bool applyRootMotionRotation, bool autoMatchAnimationSpeed,
            AbilityDefSO skillSlot1, AbilityDefSO skillSlot2)
        {
            Root = root;
            EventHub = eventHub;
            Agent = agent;
            Ability = ability;
            Reactor = reactor;
            Pathfinding = pathfinding;
            ModelRoot = modelRoot;
            Rig = rig;
            AnimationProfile = animationProfile;
            GroundSystemConfig = groundSystemConfig;
            Physique = physique;
            AudioConfig = audioConfig;
            UpperBodyMask = upperBodyMask;
            AdditiveMask = additiveMask;
            FacialMask = facialMask;
            HeadMask = headMask;
            FootMask = footMask;
            ForwardRootMotion = forwardRootMotion;
            ApplyRootMotionRotation = applyRootMotionRotation;
            AutoMatchAnimationSpeed = autoMatchAnimationSpeed;
            SkillSlot1 = skillSlot1;
            SkillSlot2 = skillSlot2;
        }
    }
}
