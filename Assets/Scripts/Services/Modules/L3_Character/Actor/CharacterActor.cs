using System.Collections.Generic;
using RedDust.Core;
using UnityEngine;
using RedDust.Character.Animation;
using RedDust.Character;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;
using RedDust.Character.Pathfinding;
using RedDust.Character.Locomotion;
using RedDust.Character.Stats;
using RedDust.Stats;

namespace RedDust.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventChannels))]
    public partial class CharacterActor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private bool isPlayer;

        [Header("Config")]
        [SerializeField] private CharacterProfile characterProfile;

        [Header("Locomotion")]
        [SerializeField] private LocomotionProfile locomotionProfile;
        [SerializeField] private LocomotionAnimationProfile locomotionAnimationProfile;

        [Header("Stats")]
        [SerializeField] private StatsTreeSO statsTree;

        [Header("Hierarchy")]
        [SerializeField] private Transform modelRoot;

        public bool IsPlayer => isPlayer;
        public Dictionary<string, (float current, float max)> LastStats { get; private set; }
        internal SCharacterKinematic LastKinematic { get; private set; }
        internal SCharacterMotor LastMotor { get; private set; }
        internal SCharacterDiscrete LastDiscrete { get; private set; }
        internal LocomotionProfile LocomotionProfile => locomotionProfile;

        private PlayerDirector director;
        private PathfindingAgent pathfindingAgent;
        private CharacterRig characterRig;
        private CharacterKinematic characterKinematic;
        private ILocomotionSimulator locomotionSimulator;
        private AnimationBrain characterAnimation;
        private CharacterStats stats;

        private void Awake()
        {
            characterAnimation = GetComponentInChildren<AnimationBrain>();
            if (modelRoot == null) modelRoot = transform;
            characterRig = new CharacterRig(transform, modelRoot);
            characterAnimation?.SetRig(characterRig);
            var eventChannels = GetComponent<EventChannels>();
            pathfindingAgent = GetComponent<PathfindingAgent>();

            director = new PlayerDirector(eventChannels, modelRoot, this);

            characterKinematic = new CharacterKinematic(transform, modelRoot, characterRig);
            locomotionSimulator = new GroundLocomotion();
            stats = new CharacterStats(statsTree);
        }

        private void Start() { }

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
            ctx.LocomotionProfile = locomotionProfile;
            ctx.LocomotionAnimationProfile = locomotionAnimationProfile;
            ctx.Kinematic = characterKinematic.Evaluate(characterProfile, intent.LocomotionHeading,
                intent.AimDirection, deltaTime);

            locomotionSimulator.Simulate(ref ctx, intent, locomotionProfile, deltaTime);

            LastKinematic = ctx.Kinematic;
            LastMotor = ctx.Motor;
            LastDiscrete = ctx.Discrete;
            stats?.Update(ctx, deltaTime);
            LastStats = stats?.LastStats;

            characterAnimation?.Apply(in ctx);
            pathfindingAgent?.SyncLocomotion(in ctx.Discrete);
        }
    }
}
