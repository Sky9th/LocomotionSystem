using System.Collections.Generic;
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

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        public bool IsPlayer => isPlayer;
        public Dictionary<string, (float current, float max)> LastStats { get; private set; }
        internal SCharacterKinematic LastKinematic { get; private set; }
        internal SCharacterMotor LastMotor { get; private set; }
        internal SCharacterDiscrete LastDiscrete { get; private set; }
        internal LocomotionProfile LocomotionProfile => locomotionProfile;

        private ICharacterDirector director;
        private PathfindingAgent pathfindingAgent;
        private CharacterRig characterRig;
        private CharacterKinematic characterKinematic;
        private ILocomotionSimulator locomotionSimulator;
        private AnimationBrain characterAnimation;
        private CharacterStats stats;

        private void Awake()
        {
            characterAnimation = GetComponentInChildren<AnimationBrain>();
            characterRig = new CharacterRig(transform, characterAnimation?.transform ?? transform);
            characterAnimation?.SetRig(characterRig);
            pathfindingAgent = GetComponent<PathfindingAgent>();
            director = new PlayerDirector(this);
            characterKinematic = new CharacterKinematic(transform, transform, characterRig);
            locomotionSimulator = new GroundLocomotion();

            stats = new CharacterStats(statsTree);
        }

        private void Start() { }

        private void OnEnable()
        {
            if (autoSubscribeInput && director is PlayerDirector pd) pd.Subscribe();
        }

        private void OnDisable()
        {
            if (director is PlayerDirector pd) { pd.Unsubscribe(); pd.Reset(); }
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
