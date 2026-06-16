using RedDust.Character;
using RedDust.Character.Director;
using RedDust.Core;

namespace RedDust.Character.Locomotion
{
    internal sealed class GroundLocomotion : Module, ILocomotionSimulator
    {
        private readonly Motor motor = new();
        private readonly Stance stance = new();

        internal GroundLocomotion(ModuleRegistry registry) : base(registry) { }

        public void Simulate(ref CharacterFrameContext ctx, in SCharacterIntent intent, LocomotionProfileSO profile, float dt)
        {
            ctx.Motor = motor.Evaluate(in ctx.Kinematic, in intent, profile, dt);
            ctx.Discrete = stance.Evaluate(in ctx.Motor, in ctx.Kinematic, in intent, profile, ctx.KinematicProfile, ctx.LocomotionAnimationProfile, dt);
        }
    }
}
