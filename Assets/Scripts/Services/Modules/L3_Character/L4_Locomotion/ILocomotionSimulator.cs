using RedDust.Character;

namespace RedDust.Character.Locomotion
{
    internal interface ILocomotionSimulator
    {
        void Simulate(ref CharacterFrameContext ctx, LocomotionProfile profile, float dt);
    }
}
