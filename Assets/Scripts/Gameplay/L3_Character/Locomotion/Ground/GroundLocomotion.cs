using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Kinematic;
using RedDust.Character.Pathfinding;
using RedDust.Container;
using RedDust.Core;
using UnityEngine;

namespace RedDust.Character.Locomotion
{
    internal sealed class GroundLocomotion : ModuleChild, ILocomotionSimulator
    {
        private readonly Motor motor = new();
        private readonly Stance stance = new();

        internal GroundLocomotion(ModuleRegistry registry) : base(registry) { }

        public void Simulate(ref SCharacterFrameContext frameCtx, in SCharacterInputState input,
            CharacterBuildContext buildCtx, float dt)
        {
            var props = buildCtx.Properties;
            var animSet = buildCtx.ResolvedLocoAnimSet;
            var pf = buildCtx.Pathfinding;
            bool hasActivePath = pf != null && pf.HasActivePath;

            var gait = input.DesiredPosture == EPosture.Crouching
                ? (hasActivePath ? EMovementGait.Crawl : EMovementGait.Idle)
                : hasActivePath
                    ? (input.WantsSprint ? EMovementGait.Sprint : EMovementGait.Run)
                    : EMovementGait.Idle;

            // ── Properties 速度系数 ──
            float agility          = props.GetFloat(CharacterConst.PropertyPath.Attributes.Agility);
            float carryWeight      = props.GetFloat(CharacterConst.PropertyPath.Movement.CarryWeight);
            float motionSpeedScale = ComputeMotionSpeedScale(agility, carryWeight,
                buildCtx.Container, buildCtx.GroundSystemConfig);

            float rawNativeSpeed = gait != EMovementGait.Idle
                ? animSet?.GetNativeSpeed(gait) ?? 0f : 0f;
            float desiredSpeed = rawNativeSpeed * motionSpeedScale;
            float acceleration = props.GetFloat(CharacterConst.PropertyPath.Movement.Acceleration);

            frameCtx.Motor = motor.Evaluate(in frameCtx.Kinematic, pf, desiredSpeed, acceleration, dt);
            frameCtx.Discrete = stance.Evaluate(in frameCtx.Motor, in frameCtx.Kinematic,
                in input, gait, animSet, motionSpeedScale, dt);
        }

        private static float ComputeMotionSpeedScale(float agility, float carryWeight,
            Container.RdContainer container, GroundSystemConfigSO config)
        {
            float agilityBonus = agility * config.agilitySpeedBonus;
            float currentWeight = container?.CurrentTotalWeight ?? 0f;
            float weightRatio = Mathf.Clamp(currentWeight / carryWeight, 0f, 1f);
            float weightPenalty = weightRatio * config.weightPenaltyRatio;
            return 1f + agilityBonus - weightPenalty;
        }
    }
}
