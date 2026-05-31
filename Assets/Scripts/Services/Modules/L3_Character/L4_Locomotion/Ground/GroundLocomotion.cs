using RedDust.Character;
using RedDust.Character.Director;

namespace RedDust.Character.Locomotion
{
    internal sealed class GroundLocomotion : ILocomotionSimulator
    {
        private readonly Motor motor = new();
        private readonly Stance stance = new();

        public void Simulate(ref CharacterFrameContext ctx, in SCharacterIntent intent, LocomotionProfile profile, float dt)
        {
            ctx.Motor = motor.Evaluate(in ctx.Kinematic, in intent, profile, dt);
            ctx.Discrete = stance.Evaluate(in ctx.Motor, in ctx.Kinematic, in intent, profile, ctx.LocomotionAnimationProfile, dt);
        }
    }
}
