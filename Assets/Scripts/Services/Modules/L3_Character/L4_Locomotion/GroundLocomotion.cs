using RedDust.Character;

namespace RedDust.Character.Locomotion
{
    internal sealed class GroundLocomotion : ILocomotionSimulator
    {
        private readonly Motor motor = new();
        private readonly Stance stance = new();

        public void Simulate(ref CharacterFrameContext ctx, LocomotionProfile profile, float dt)
        {
            ctx.Motor = motor.Evaluate(in ctx.Kinematic, in ctx.Input, profile, dt);
            ctx.Discrete = stance.Evaluate(in ctx.Motor, in ctx.Kinematic, in ctx.Input, profile, dt);
        }
    }
}
