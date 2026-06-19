using RedDust.Character;
using RedDust.Character.Director;

namespace RedDust.Character.Locomotion
{
    internal interface ILocomotionSimulator
    {
        void Simulate(ref CharacterFrameContext frameCtx, in SCharacterIntent intent,
            CharacterBuildContext buildCtx, float dt);
    }
}
