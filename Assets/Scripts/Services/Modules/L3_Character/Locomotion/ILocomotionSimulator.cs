using RedDust.Character;
using RedDust.Character.Director;

namespace RedDust.Character.Locomotion
{
    internal interface ILocomotionSimulator
    {
        void Simulate(ref CharacterFrameContext ctx, in SCharacterIntent intent, LocomotionProfileSO profile, float dt);
    }
}
