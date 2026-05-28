using System.Collections.Generic;
using UnityEngine;
using RedDust.Character.Animation;
using RedDust.Character;
using RedDust.Character.Kinematic;
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

        [Header("Stats")]
        [SerializeField] private StatsTreeSO statsTree;

        [Header("Input")]
        [SerializeField] private bool autoSubscribeInput = true;

        public bool IsPlayer => isPlayer;
        public Dictionary<string, (float current, float max)> LastStats { get; private set; }
        internal float PlanarSpeed { get; private set; }
        internal SCharacterKinematic LastKinematic { get; private set; }
        internal SCharacterMotor LastMotor { get; private set; }
        internal SCharacterDiscrete LastDiscrete { get; private set; }

        private CharacterEventReceiver inputModule;
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
            inputModule = new CharacterEventReceiver(this);
            characterKinematic = new CharacterKinematic(transform, transform, characterRig);
            locomotionSimulator = new GroundLocomotion();

            stats = new CharacterStats(statsTree);

            DumpStatsTree();
        }

        private void DumpStatsTree()
        {
            if (statsTree == null) { Debug.Log("[StatsTree] null"); return; }
            var resolved = statsTree.Resolve();
            var sb = new System.Text.StringBuilder($"[StatsTree] {statsTree.name} — {resolved.Count} stats\n");
            foreach (var s in resolved)
                sb.AppendLine($"  ✅ {s.Path}  ({s.Current})");
        }

        private void Start() { }

        private void OnEnable()
        {
            if (autoSubscribeInput) inputModule?.Subscribe();
        }

        private void OnDisable()
        {
            inputModule?.Unsubscribe();
            inputModule?.Reset();
            characterKinematic?.Reset();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= Mathf.Epsilon) return;

            var ctx = new CharacterFrameContext();

            inputModule.ReadActions(out ctx.Input);

            Vector3 heading;
            if (inputModule.ReadMouseGroundPosition(out Vector3 mouseWorldPos))
            {
                var dir = mouseWorldPos - transform.position;
                dir.y = 0f;
                heading = dir.sqrMagnitude > Mathf.Epsilon ? dir.normalized : transform.forward;
            }
            else
            {
                heading = transform.forward;
            }

            ctx.Kinematic = characterKinematic.Evaluate(characterProfile, heading, deltaTime);

            locomotionSimulator.Simulate(ref ctx, locomotionProfile, deltaTime);

            PlanarSpeed = ctx.Motor.ActualPlanarVelocity.magnitude;
            LastKinematic = ctx.Kinematic;
            LastMotor = ctx.Motor;
            LastDiscrete = ctx.Discrete;
            stats?.Update(ctx, deltaTime);

            if (stats != null)
            {
                var dict = new Dictionary<string, (float current, float max)>();
                foreach (var kv in stats.All)
                    dict[kv.Key] = (kv.Value.Current, kv.Value.Def.Max);
                LastStats = dict;
            }

            characterAnimation?.Apply(in ctx);
        }
    }
}
