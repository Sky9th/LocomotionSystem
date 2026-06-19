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

        public void Simulate(ref CharacterFrameContext frameCtx, in SCharacterIntent intent,
            CharacterBuildContext buildCtx, float dt)
        {
            var profile = buildCtx.LocomotionProfile;
            var kProfile = buildCtx.KinematicProfile;
            var animProfile = buildCtx.LocomotionAnimConfig;
            frameCtx.Motor = motor.Evaluate(in frameCtx.Kinematic, in intent, profile, dt);
            frameCtx.Discrete = stance.Evaluate(in frameCtx.Motor, in frameCtx.Kinematic, in intent, profile, kProfile, animProfile, dt);
        }
    }
}
