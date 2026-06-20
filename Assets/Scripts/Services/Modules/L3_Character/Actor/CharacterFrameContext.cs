using RedDust.Character;
using RedDust.Character.Animation;
using RedDust.Character.Director;
using RedDust.Character.Kinematic;
using RedDust.Character.Locomotion;

namespace RedDust.Character
{
    public struct CharacterFrameContext
    {
        public SCharacterIntent Intent;
        public SCharacterKinematic Kinematic;
        public SCharacterMotor Motor;
        public SCharacterDiscrete Discrete;
    }
}
