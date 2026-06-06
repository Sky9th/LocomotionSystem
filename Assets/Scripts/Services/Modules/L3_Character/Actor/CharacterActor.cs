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
using RedDust.Character.Combat;
using RedDust.Character.Stats;
using RedDust.Stats;

namespace RedDust.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventHub))]
    public partial class CharacterActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Config")]
        [SerializeField] private CharacterProfileSO characterProfile;

        [Header("Locomotion")]
        [SerializeField] private LocomotionAnimationConfigSO locomotionAnimationProfile;

        [Header("Stats")]
        [SerializeField] private StatsTreeSO statsTree;

        // TODO: 临时方案 — 技能树/装备系统完成后替换为技能槽位子系统
        [Header("Ability Slots (Temp)")]
        [SerializeField] private AbilityDefSO skillSlot1;
        [SerializeField] private AbilityDefSO skillSlot2;

        [Header("Hierarchy")]
        [SerializeField] private Transform modelRoot;

        public bool IsPlayer => isPlayer;
        internal AbilityDefSO SkillSlot1 => skillSlot1;
        internal AbilityDefSO SkillSlot2 => skillSlot2;
        public Dictionary<string, (float current, float max)> LastStats { get; private set; }
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
        private CharacterStats stats;
        private AbilityExecutor ability;
        private AbilityReactor reactor;
        private CharacterCombat combat;
        private EventHub eventHub;
        internal AbilityExecutor AbilityExecutor => ability;

        private void Awake()
        {
            characterAnimation = GetComponentInChildren<AnimationBrain>();
            if (modelRoot == null) modelRoot = transform;
            characterRig = new CharacterRig(transform, modelRoot);
            characterAnimation?.SetRig(characterRig);
            eventHub = GetComponent<EventHub>();
            pathfindingAgent = GetComponent<PathfindingAgent>();

            director = new PlayerDirector(eventHub, modelRoot, this);

            characterKinematic = new CharacterKinematic(transform, modelRoot, characterRig);
            locomotionSimulator = new GroundLocomotion();
            stats = new CharacterStats(statsTree);
            ability = GetComponent<AbilityExecutor>();
            reactor = GetComponent<AbilityReactor>();
            combat = new CharacterCombat(ability, reactor, stats, eventHub);
        }

        private void Start()
        {
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
            stats?.Update(deltaTime);
            LastStats = stats?.LastStats;
            
            characterAnimation?.Apply(in ctx);
            pathfindingAgent?.SyncLocomotion(in ctx.Discrete);
        }
    }
}
