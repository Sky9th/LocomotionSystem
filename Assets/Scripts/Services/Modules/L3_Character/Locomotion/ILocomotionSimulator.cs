using RedDust.Character;
using RedDust.Character.Animation;

namespace RedDust.Character.Locomotion
{
    internal interface ILocomotionSimulator
    {
        void Simulate(ref SCharacterFrameContext frameCtx, in SCharacterInputState input,
            CharacterBuildContext buildCtx, float dt);
    }
}
