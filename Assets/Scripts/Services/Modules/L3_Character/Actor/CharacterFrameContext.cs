using RedDust.Character;
using RedDust.Character.Kinematic;
using RedDust.Character.Locomotion;

namespace RedDust.Character
{
    public struct CharacterFrameContext
    {
        public SCharacterInputActions Input;
        public SCharacterKinematic Kinematic;
        public SCharacterMotor Motor;
        public SCharacterDiscrete Discrete;
    }
}
