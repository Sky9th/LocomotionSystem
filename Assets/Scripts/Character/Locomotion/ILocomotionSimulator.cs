using Game.Character.Components;

namespace Game.Character.Locomotion
{
    internal interface ILocomotionSimulator
    {
        void Simulate(ref CharacterFrameContext ctx, LocomotionProfile profile, float dt);
    }
}
