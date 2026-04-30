using Game.Character.Input;

namespace Game.Character.Components
{
    internal struct CharacterFrameContext
    {
        public SCharacterInputActions Input;
        public SCharacterKinematic Kinematic;
        public SLocomotionMotor Motor;
        public SLocomotionDiscrete Discrete;
    }
}
