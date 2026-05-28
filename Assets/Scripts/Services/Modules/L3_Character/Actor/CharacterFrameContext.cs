using Game.Character.Input;

namespace Game.Character.Components
{
    public struct CharacterFrameContext
    {
        public SCharacterInputActions Input;
        public SCharacterKinematic Kinematic;
        public SCharacterMotor Motor;
        public SCharacterDiscrete Discrete;
    }
}
