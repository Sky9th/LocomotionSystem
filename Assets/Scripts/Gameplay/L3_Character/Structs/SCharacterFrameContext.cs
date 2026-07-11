using RedDust.Character.Kinematic;
using RedDust.Character.Locomotion;

namespace RedDust.Character
{
    public struct SCharacterFrameContext
    {
        public SCharacterKinematic Kinematic;
        public SCharacterMotor Motor;
        public SCharacterDiscrete Discrete;
    }
}
