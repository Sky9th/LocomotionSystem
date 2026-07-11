using RedDust.Gameplay.Character.Kinematic;
using RedDust.Gameplay.Character.Locomotion;

namespace RedDust.Gameplay.Character
{
    public struct SCharacterFrameContext
    {
        public SCharacterKinematic Kinematic;
        public SCharacterMotor Motor;
        public SCharacterDiscrete Discrete;
    }
}
