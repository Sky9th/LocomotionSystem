using RedDust.Gameplay.Character;
using RedDust.Gameplay.Character.Animation;

namespace RedDust.Gameplay.Character.Locomotion
{
    internal interface ILocomotionSimulator
    {
        void Simulate(ref SCharacterFrameContext frameCtx, in SCharacterInputState input,
            CharacterBuildContext buildCtx, float dt);
    }
}
