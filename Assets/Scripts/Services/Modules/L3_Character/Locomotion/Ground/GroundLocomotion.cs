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
            var animSet = buildCtx.DefaultLocomotionSet;
            var physique = buildCtx.Physique;
            // TODO: posture-aware speed — gait 速度 × posture 系数 (源自 Properties.Body/xxx 或 posture-specific animSet)
            var desiredSpeed = intent.HasMovement ? (animSet != null ? animSet.GetNativeSpeed(intent.DesiredGait) : 0f) : 0f;
            var acceleration = physique.Acceleration;

            frameCtx.Motor = motor.Evaluate(in frameCtx.Kinematic, in intent, desiredSpeed, acceleration, dt);
            frameCtx.Discrete = stance.Evaluate(in frameCtx.Motor, in frameCtx.Kinematic, in intent, animSet, dt);
        }
    }
}
