using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Pathfinding;
using RedDust.Core;

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
            var physique = buildCtx.Physique;
            var animSet = buildCtx.ResolvedLocoAnimSet;
            var pf = buildCtx.Pathfinding;
            bool hasActivePath = pf != null && pf.HasActivePath;

            // Gait: 从 InputState + pathfinding 推导
            var gait = hasActivePath
                ? (input.WantsSprint ? EMovementGait.Sprint : EMovementGait.Run)
                : EMovementGait.Idle;

            var desiredSpeed = gait != EMovementGait.Idle
                ? animSet?.GetNativeSpeed(gait) ?? 0f : 0f;
            var acceleration = physique.Acceleration;

            frameCtx.Motor = motor.Evaluate(in frameCtx.Kinematic, pf, desiredSpeed, acceleration, dt);
            frameCtx.Discrete = stance.Evaluate(in frameCtx.Motor, in frameCtx.Kinematic,
                in input, gait, animSet, dt);
        }
    }
}
